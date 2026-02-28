
using Core.DTO.Dish;
using Core.Extensions;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.I18n;
using Core.Mappers;
using Microsoft.AspNetCore.Http;

namespace Core.Service;

/// <summary>
/// Implementation of IDishService handling dish business logic
/// Service implementation for dish business logic operations.
/// </summary>
public class DishService : IDishService
{
    private readonly IDishRepository _dishRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly ILogger<DishService> _logger;
    private readonly IDishI18nService _dishI18nService;
    private readonly IMediaRepository _mediaRepo;
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _fileStorage;

    private static readonly string[] SupportedLangs = { "en", "vi", "fr" };

    public DishService(IDishRepository dishRepository, ILookupResolver lookupResolver, ILogger<DishService> logger, IDishI18nService dishI18NService, IMediaRepository mediaRepository, IUnitOfWork unitOfWork, IFileStorage fileStorage)
    {
        _dishRepository = dishRepository;
        _lookupResolver = lookupResolver;
        _logger = logger;
        _dishI18nService = dishI18NService;
        _mediaRepo = mediaRepository;
        _uow = unitOfWork;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc/>
    public async Task<DishDetailDto?> GetDishByIdAsync(long id, string? langCode = null, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(id, cancellationToken);

        // Default to English if not specified. Supported: en (English), fr (French), vi (Vietnamese)
        var language = langCode ?? "en";

        if (dish == null)
        {
            return null;
        }

        return new DishDetailDto
        {
            DishId = dish.DishId,
            DishName = dish.DishNameText.GetTranslation(language),
            Price = dish.Price,
            CategoryName = dish.Category.CategoryName,
            Description = dish.DescriptionText?.GetTranslation(language),
            ShortDescription = dish.ShortDescriptionText?.GetTranslation(language),
            Slogan = dish.SloganText?.GetTranslation(language),
            Calories = dish.Calories,
            PrepTimeMinutes = dish.PrepTimeMinutes,
            CookTimeMinutes = dish.CookTimeMinutes,
            ImageUrls = dish.DishMedia
                .Where(dm => dm.Media != null)
                .Select(dm => dm.Media!.Url ?? string.Empty)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList(),
            Composition = dish.Recipes
                .Select(r => new RecipeItemDto
                {
                    IngredientId = r.IngredientId,
                    IngredientName = r.Ingredient?.IngredientNameText?.GetTranslation(language) ?? r.Ingredient?.IngredientName ?? string.Empty,
                    Quantity = r.Quantity,
                    Unit = r.Unit,
                    Note = r.Note
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<DishStatusDto> UpdateDishStatusAsync(
        long dishId,
        DishStatusCode newStatus,
        CancellationToken ct = default)
    {
        try
        {
            // Check if dish exists
            var dish = await _dishRepository.FindByIdAsync(dishId, ct);
            if (dish == null)
            {
                _logger.LogWarning("Dish with ID {DishId} not found", dishId);
                throw new KeyNotFoundException($"Dish with ID {dishId} not found.");
            }

            // Resolve status code to lookup value ID using extension method
            var statusId = await newStatus.ToDishStatusIdAsync(_lookupResolver, ct);

            // Update the dish status
            await _dishRepository.UpdateStatusAsync(dishId, statusId, ct);

            _logger.LogInformation(
                "Updated dish {DishId} ({DishName}) status to {Status}",
                dishId,
                dish.DishName,
                newStatus);

            return new DishStatusDto
            {
                DishId = dish.DishId,
                DishName = dish.DishName,
                StatusCode = newStatus,
                StatusId = statusId,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (KeyNotFoundException)
        {
            // Re-throw KeyNotFoundException to be handled by controller
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dish status for dish ID {DishId}", dishId);
            throw new InvalidOperationException($"Failed to update dish status: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lấy danh sách cho Admin (Map sang DishManagementDto)
    /// </summary>
    public async Task<(List<DishManagementDto> Items, int TotalCount)> GetDishesForAdminAsync(
        GetDishesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Gọi Repository để lấy dữ liệu
            var (entities, totalCount) = await _dishRepository.GetDishesAsync(request, cancellationToken);

            // 2. Map sang DTO của bạn
            var dtos = entities.Select(d => new DishManagementDto
            {
                DishId = d.DishId,
                DishName = d.DishName,

                // Xử lý null cho Category
                CategoryName = d.Category?.CategoryName ?? "Uncategorized",

                Price = d.Price,

                // Map Status (Tên hiển thị)
                Status = d.DishStatusLv?.ValueName ?? "Unknown",

                // Map StatusId (Quan trọng để Frontend tô màu Badge)
                StatusId = d.DishStatusLvId,

                IsOnline = d.IsOnline ?? false,
                CreatedAt = d.CreatedAt
            }).ToList();

            return (dtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dishes for admin");
            throw;
        }
    }

    /// <summary>
    /// Hàm hỗ trợ: Trích xuất 3 ngôn ngữ từ Entity i18nText sang DTO
    /// </summary>
    private I18nTextDto MapTranslations(I18nText? textEntity, string fallbackStr)
    {
        if (textEntity == null || textEntity.I18nTranslations == null || !textEntity.I18nTranslations.Any())
        {
            return new I18nTextDto { Vi = fallbackStr, En = fallbackStr, Fr = fallbackStr };
        }

        return new I18nTextDto
        {
            Vi = textEntity.I18nTranslations.FirstOrDefault(t => t.LangCode == "vi")?.TranslatedText ?? fallbackStr,
            En = textEntity.I18nTranslations.FirstOrDefault(t => t.LangCode == "en")?.TranslatedText ?? fallbackStr,
            Fr = textEntity.I18nTranslations.FirstOrDefault(t => t.LangCode == "fr")?.TranslatedText ?? fallbackStr
        };
    }

    /// <summary>
    /// Lấy danh sách cho Customer (Đã hỗ trợ Đa ngôn ngữ trọn gói)
    /// </summary>
    public async Task<(List<DishDisplayDto> Items, int TotalCount)> GetDishesForCustomerAsync(
        GetDishesRequest request,
        CancellationToken cancellationToken = default)
    {
        request.IsCustomerView = true;
        var (entities, totalCount) = await _dishRepository.GetDishesAsync(request, cancellationToken);

        var dtos = entities.Select(d => new DishDisplayDto
        {
            DishId = d.DishId,

            // Map Đa ngôn ngữ Tên món
            DishName = MapTranslations(d.DishNameText, d.DishName),

            Price = d.Price,

            // Map Đa ngôn ngữ Tên Category
            CategoryName = d.Category != null
                ? MapTranslations(d.Category.CategoryNameText, d.Category.CategoryName)
                : new I18nTextDto(),

            // Map Đa ngôn ngữ Mô tả
            Description = MapTranslations(d.DescriptionText, string.Empty),

            // Lấy ảnh Primary, nếu không có thì lấy ảnh đầu tiên
            ImageUrl = d.DishMedia.FirstOrDefault(dm => dm.IsPrimary == true)?.Media?.Url
                    ?? d.DishMedia.FirstOrDefault()?.Media?.Url,

            IsChefRecommended = d.ChefRecommended ?? false
        }).ToList();

        return (dtos, totalCount);
    }

    // --- CÁC HÀM HỖ TRỢ DROPDOWN (Giữ nguyên) ---

    public async Task<List<string>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dishRepository.GetAllCategoriesAsync(cancellationToken);
    }

    public async Task<List<DishStatusDto>> GetDishStatusesAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _dishRepository.GetDishStatusesAsync(cancellationToken);

        return statuses.Select(s => new DishStatusDto
        {
            StatusId = s.ValueId,
            StatusName = s.ValueName
        }).ToList();
    }

    public async Task<long> CreateDishAsync(
            CreateDishRequest request,
            IReadOnlyList<IFormFile> staticImages,
            IReadOnlyList<IFormFile> images360,
            CancellationToken ct)
    {
        var uploadedFiles = new List<string>();

        await _uow.BeginTransactionAsync(ct); // Start transaction

        try
        {
            // Create i18n text records for the dish
            var textIds = await _dishI18nService.CreateDishI18nTextsAsync(request.I18n, ct);

            // Create new Dish entity
            var dish = new Dish
            {
                CategoryId = request.CategoryId,
                DishName = request.I18n["en"].DishName,
                Price = request.Price,
                IsOnline = request.IsOnline,
                ChefRecommended = request.ChefRecommended,
                Calories = request.Calories,
                PrepTimeMinutes = request.PrepTimeMinutes,
                CookTimeMinutes = request.CookTimeMinutes,
                DishStatusLvId = request.DishStatusLvId,
                DishNameTextId = textIds.DishNameTextId,
                DescriptionTextId = textIds.DescriptionTextId,
                SloganTextId = textIds.SloganTextId,
                NoteTextId = textIds.NoteTextId,
                ShortDescriptionTextId = textIds.ShortDescriptionTextId,
                CreatedAt = DateTime.UtcNow
            };

            await _dishRepository.AddAsync(dish, ct); // Save dish to DB

            // Add tag to dish
            foreach (var tagId in request.TagIds.Distinct())
            {
                await _dishRepository.AddDishTagAsync(new DishTag
                {
                    DishId = dish.DishId,
                    TagId = tagId
                }, ct);
            }

            // Save static images and link to dish
            foreach (var file in staticImages)
            {
                var path = await _fileStorage.SaveAsync(file.OpenReadStream(), file.FileName, "dishes", ct); // Save file
                uploadedFiles.Add(path);

                var mediaTypeid = _lookupResolver.GetIdAsync((ushort)Enum.LookupType.MediaType, MediaTypeCode.IMAGE, ct).Result; // Get media type id

                var media = await _mediaRepo.AddMediaAsync(new MediaAsset
                {
                    Url = "/uploads/" + path,
                    MimeType = file.ContentType,
                    MediaTypeLvId = mediaTypeid,
                    CreatedAt = DateTime.UtcNow
                }, ct);

                await _mediaRepo.AddDishMediaAsync(new DishMedium
                {
                    DishId = dish.DishId,
                    MediaId = media.MediaId,
                    IsPrimary = true
                }, ct);
            }

            await _uow.CommitAsync(ct); // Commit transaction
            return dish.DishId;
        }
        catch
        {
            await _uow.RollbackAsync(ct); // Rollback transaction on error

            // Delete uploaded files if error occurs
            foreach (var f in uploadedFiles)
                await _fileStorage.DeleteAsync(f);

            throw;
        }
    }

    public async Task<List<ActiveDishStatusDto>> GetActiveDishStatusesAsync()
    {
        var entities = await _dishRepository.GetActiveDishStatusEntitiesAsync(); // Get active dish statuses

        return entities.Select(lv => new ActiveDishStatusDto
        {
            DishStatusLvId = lv.ValueId,
            ValueName = lv.ValueName,
            ValueCode = lv.ValueCode
        }).ToList();
    }

    public async Task<List<DishCategorySimpleDto>> GetAllDishCategoriesAsync()
    {
        var categories = await _dishRepository.GetAllDishCategoriesAsync(); // Get all dish categories

        return categories.Select(c =>
        {
            var translations = c.CategoryNameText?.I18nTranslations ?? new List<I18nTranslation>();

            return new DishCategorySimpleDto
            {
                CategoryId = c.CategoryId,
                NameVi = translations.FirstOrDefault(t => t.LangCode == "vi")?.TranslatedText ?? c.CategoryName,
                NameEn = translations.FirstOrDefault(t => t.LangCode == "en")?.TranslatedText ?? c.CategoryName,
                NameFr = translations.FirstOrDefault(t => t.LangCode == "fr")?.TranslatedText ?? c.CategoryName
            };
        }).ToList();
    }

    public async Task<List<DishTagDto>> GetAllActiveTagsAsync()
    {
        var tags = await _dishRepository.GetAllActiveTagsAsync(); // Get all active tags

        return tags.Select(t => new DishTagDto
        {
            TagId = t.ValueId,
            Code = t.ValueCode,
            Name = t.ValueName
        }).ToList();
    }

    public async Task<DishDetailForActionsDto> GetDishByIdAsync(long dishId, CancellationToken cancellationToken)
    {
        var dish = await _dishRepository.FindByIdForActionAsync(dishId, cancellationToken); // Find dish by id
        var dishTags = await _dishRepository.FindTagByDishIdAsync(dishId, cancellationToken); // Find tag for dish

        if (dish == null)
            throw new KeyNotFoundException($"Dish with ID {dishId} not found!");

        return DishMapper.ToDetailDto(dish, dishTags); // Map to DTO
    }

    public async Task UpdateDishAsync(
        UpdateDishRequest request,
        IReadOnlyList<IFormFile> staticImages,
        IReadOnlyList<IFormFile> images360,
        IReadOnlyList<long> removedMediaIds,
        CancellationToken ct)
    {
        var uploadedFiles = new List<string>();
        var filesToDeleteAfterCommit = new List<string>();

        await _uow.BeginTransactionAsync(ct); // Start transaction

        try
        {
            // 1. Load Dish
            var dish = await _dishRepository.FindByIdForActionAsync(request.DishId, ct)
                ?? throw new KeyNotFoundException($"Dish with ID {request.DishId} not found!");

            // 2. Update core fields
            dish.CategoryId = request.CategoryId;
            dish.Price = request.Price;
            dish.IsOnline = request.IsOnline;
            dish.ChefRecommended = request.ChefRecommended;
            dish.Calories = request.Calories;
            dish.PrepTimeMinutes = request.PrepTimeMinutes;
            dish.CookTimeMinutes = request.CookTimeMinutes;
            dish.DishStatusLvId = request.DishStatusLvId;
            dish.DishName = request.I18n["en"].DishName;

            // 2.1 Update tags (many-to-many)
            var existingTagIds = await _dishRepository
                .GetTagIdsByDishIdAsync(dish.DishId, ct);

            var newTagIds = request.TagIds
                .Distinct()
                .ToList();

            // Tags to remove
            var tagsToRemove = existingTagIds
                .Where(id => !newTagIds.Contains(id))
                .ToList();

            // Tags to add
            var tagsToAdd = newTagIds
                .Where(id => !existingTagIds.Contains(id))
                .ToList();

            // Remove old tags
            if (tagsToRemove.Any())
            {
                await _dishRepository.RemoveDishTagsAsync(
                    dish.DishId,
                    tagsToRemove,
                    ct);
            }

            // Add new tags
            foreach (var tagId in tagsToAdd)
            {
                await _dishRepository.AddDishTagAsync(new DishTag
                {
                    DishId = dish.DishId,
                    TagId = tagId
                }, ct);
            }

            // 3. Update I18n
            var newTextIds =
            await _dishI18nService.CreateOrUpdateDishI18nTextsAsync(
                new DishI18nTextIds
                {
                    DishNameTextId = dish.DishNameTextId,
                    DescriptionTextId = dish.DescriptionTextId,
                    ShortDescriptionTextId = dish.ShortDescriptionTextId,
                    SloganTextId = dish.SloganTextId,
                    NoteTextId = dish.NoteTextId
                },
                request.I18n,
                ct
            );
            // Assign new text ids to dish
            dish.DishNameTextId = newTextIds.DishNameTextId;
            dish.DescriptionTextId = newTextIds.DescriptionTextId;
            dish.ShortDescriptionTextId = newTextIds.ShortDescriptionTextId;
            dish.SloganTextId = newTextIds.SloganTextId;
            dish.NoteTextId = newTextIds.NoteTextId;

            // 4. Remove old media (DB only)
            if (removedMediaIds.Any())
            {
                var medias = await _mediaRepo.GetDishMediaByIdsAsync(
                    dish.DishId,
                    removedMediaIds,
                    ct
                );

                foreach (var m in medias)
                {
                    filesToDeleteAfterCommit.Add(m.Media.Url.Replace("/uploads/", "")); // Mark file for deletion after commit

                    await _mediaRepo.RemoveDishMediaAsync(m, ct); // Remove dish-media link
                    await _mediaRepo.RemoveMediaAsync(m.Media, ct); // Remove media record
                }
            }

            // 5. Add new static images
            var imageTypeId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.MediaType,
            MediaTypeCode.IMAGE,
                ct
            );

            foreach (var file in staticImages)
            {
                var path = await _fileStorage.SaveAsync(
                    file.OpenReadStream(),
                    file.FileName,
                    "dishes",
                    ct
                );

                uploadedFiles.Add(path);

                var media = await _mediaRepo.AddMediaAsync(new MediaAsset
                {
                    Url = "/uploads/" + path,
                    MimeType = file.ContentType,
                    MediaTypeLvId = imageTypeId,
                    CreatedAt = DateTime.UtcNow
                }, ct);

                await _mediaRepo.AddDishMediaAsync(new DishMedium
                {
                    DishId = dish.DishId,
                    MediaId = media.MediaId,
                    IsPrimary = true
                }, ct);
            }

            await _uow.SaveChangesAsync(ct); // Save changes to DB

            await _uow.CommitAsync(ct); // Commit transaction
        }
        catch
        {
            await _uow.RollbackAsync(ct); // Rollback transaction on error

            // Delete uploaded files if error occurs
            foreach (var f in uploadedFiles)
                await _fileStorage.DeleteAsync(f);

            throw;
        }
        // 7. Delete old files AFTER commit
        foreach (var f in filesToDeleteAfterCommit)
        {
            try
            {
                await _fileStorage.DeleteAsync(f); // Delete file from storage
            }
            catch (Exception ex)
            {
                // Ignore file deletion errors
            }
        }
    }

    public async Task<List<DishPosResponseDto>> GetPosDishesAsync(bool active)
    {
        var dishes = active
            ? await _dishRepository.GetActiveDishesAsync()
            : new List<Dish>();

        return dishes.Select(MapToDto).ToList();
    }

    private DishPosResponseDto MapToDto(Dish dish)
    {
        var dto = new DishPosResponseDto
        {
            DishId = dish.DishId,
            CategoryId = dish.CategoryId,
            Price = dish.Price,
            ChefRecommended = dish.ChefRecommended,
            DisplayOrder = dish.DisplayOrder
        };

        foreach (var lang in SupportedLangs)
        {
            dto.I18n[lang] = new DishI18nDto
            {
                DishName = GetTranslation(dish.DishNameText, lang),
                Description = GetTranslation(dish.DescriptionText, lang),
                Slogan = GetTranslation(dish.SloganText, lang),
                Note = GetTranslation(dish.NoteText, lang),
                ShortDescription = GetTranslation(dish.ShortDescriptionText, lang)
            };
        }

        return dto;
    }

    private string? GetTranslation(I18nText? text, string lang)
    {
        if (text == null) return null;

        if (text.SourceLangCode == lang)
            return text.SourceText;

        return text.I18nTranslations
            .FirstOrDefault(t => t.LangCode == lang)
            ?.TranslatedText
            ?? text.SourceText;
    }
}

