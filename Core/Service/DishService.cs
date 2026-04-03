using Core.DTO.Dish;
using Core.DTO.General;
using Core.Extensions;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.I18n;
using Core.Mappers;

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
    private readonly ISystemSettingService _systemSettingService;

    private static readonly string[] SupportedLangs = { "en", "vi", "fr" };

    public DishService(
        IDishRepository dishRepository,
        ILookupResolver lookupResolver,
        ILogger<DishService> logger,
        IDishI18nService dishI18NService,
        IMediaRepository mediaRepository,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        ISystemSettingService systemSettingService)
    {
        _dishRepository = dishRepository;
        _lookupResolver = lookupResolver;
        _logger = logger;
        _dishI18nService = dishI18NService;
        _mediaRepo = mediaRepository;
        _uow = unitOfWork;
        _fileStorage = fileStorage;
        _systemSettingService = systemSettingService;
    }

    /// <inheritdoc/>
    public async Task<DishDetailDto?> GetDishByIdAsync(long id, string? langCode = null, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(id, cancellationToken);
        var language = langCode ?? "en";

        if (dish == null) return null;

        return new DishDetailDto
        {
            DishId = dish.DishId,
            DishName = dish.DishNameText.GetTranslation(language),
            Price = dish.Price,
            CategoryName = dish.Category.CategoryNameText?.GetTranslation(language) ?? dish.Category.CategoryName,
            Description = dish.DescriptionText?.GetTranslation(language),
            ShortDescription = dish.ShortDescriptionText?.GetTranslation(language),
            Slogan = dish.SloganText?.GetTranslation(language),
            Calories = dish.Calories,
            PrepTimeMinutes = dish.PrepTimeMinutes,
            CookTimeMinutes = dish.CookTimeMinutes,
            // Url stores RelativePath — resolve to public URL at read time
            ImageUrls = await ShouldShowMedia("landing_page.show_dish_image", cancellationToken)
                ? dish.DishMedia
                    .Where(dm => dm.Media != null && !string.Equals(dm.Media!.MimeType, "video/mp4", StringComparison.OrdinalIgnoreCase) && !(dm.Media!.MimeType ?? "").StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                    .Select(dm => _fileStorage.GetPublicUrl(dm.Media!.Url))
                    .ToList()
                : new List<string>(),
            VideoUrl = await ShouldShowMedia("landing_page.show_dish_video", cancellationToken)
                ? dish.DishMedia
                    .Where(dm => dm.Media != null && (dm.Media!.MimeType ?? "").StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                    .Select(dm => _fileStorage.GetPublicUrl(dm.Media!.Url))
                    .FirstOrDefault()
                : null,
            Composition = dish.Recipes
            .Select(r => new RecipeItemDto
            {
                IngredientId = r.IngredientId,
                IngredientName = r.Ingredient?.IngredientNameText?.GetTranslation(language) ?? r.Ingredient?.IngredientName ?? string.Empty,
                Quantity = r.Quantity,
                Unit = r.Unit,
                Note = r.Note
            }).ToList()
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
            var (entities, totalCount) = await _dishRepository.GetDishesAsync(request, cancellationToken);

            var dtos = entities.Select(d => new DishManagementDto
            {
                DishId = d.DishId,
                DishName = d.DishName,
                CategoryName = d.Category?.CategoryName ?? "Uncategorized",
                Price = d.Price,
                Status = d.DishStatusLv?.ValueName ?? "Unknown",
                StatusId = d.DishStatusLvId,
                IsOnline = d.IsOnline ?? false,
                CreatedAt = d.CreatedAt,


                NameI18n = new I18nTextDto
                {
                    Vi = d.DishNameText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "vi")?.TranslatedText ?? string.Empty,
                    En = d.DishNameText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "en")?.TranslatedText ?? string.Empty,
                    Fr = d.DishNameText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "fr")?.TranslatedText ?? string.Empty
                },

                DescriptionI18n = new I18nTextDto
                {
                    Vi = d.DescriptionText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "vi")?.TranslatedText ?? string.Empty,
                    En = d.DescriptionText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "en")?.TranslatedText ?? string.Empty,
                    Fr = d.DescriptionText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "fr")?.TranslatedText ?? string.Empty
                },

                CategoryNameI18n = new I18nTextDto
                {
                    Vi = d.Category?.CategoryNameText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "vi")?.TranslatedText ?? string.Empty,
                    En = d.Category?.CategoryNameText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "en")?.TranslatedText ?? string.Empty,
                    Fr = d.Category?.CategoryNameText?.I18nTranslations?.FirstOrDefault(t => t.LangCode == "fr")?.TranslatedText ?? string.Empty
                }
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
        
        var showImages = await ShouldShowMedia("landing_page.show_dish_image", cancellationToken);

        var dtos = entities.Select(d => new DishDisplayDto
        {
            DishId = d.DishId,
            DishName = MapTranslations(d.DishNameText, d.DishName),
            Price = d.Price,
            CategoryName = d.Category != null
            ? MapTranslations(d.Category.CategoryNameText, d.Category.CategoryName) : new I18nTextDto(),
            Description = MapTranslations(d.DescriptionText, string.Empty),
            // Url stores RelativePath — resolve to public URL at read time
            ImageUrl = showImages 
                ? (d.DishMedia.FirstOrDefault(dm => dm.IsPrimary == true)?.Media?.Url is { } primaryUrl
                    ? _fileStorage.GetPublicUrl(primaryUrl)
                    : d.DishMedia.FirstOrDefault()?.Media?.Url is { } firstUrl ? _fileStorage.GetPublicUrl(firstUrl) : null)
                : null,
            IsChefRecommended = d.ChefRecommended ?? false
        }).ToList();

        return (dtos, totalCount);
    }


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
        IReadOnlyList<MediaFileInput> staticImages,
        IReadOnlyList<MediaFileInput> images360,
        MediaFileInput? video,
        CancellationToken ct)
    {
        var savedFilePaths = new List<string>();

        await _uow.BeginTransactionAsync(ct);

        try
        {
            var textIds = await _dishI18nService.CreateDishI18nTextsAsync(request.I18n, ct);

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

            await _dishRepository.AddAsync(dish, ct);

            foreach (var tagId in request.TagIds.Distinct())
            {
                await _dishRepository.AddDishTagAsync(new DishTag { DishId = dish.DishId, TagId = tagId }, ct);
            }

            var imageTypeId = await _lookupResolver.GetIdAsync(
            (ushort)Enum.LookupType.MediaType, MediaTypeCode.IMAGE, ct);

            foreach (var file in staticImages)
            {
                var uploadResult = await _fileStorage.SaveAsync(
               new FileUploadRequest
               {
                   Stream = file.Stream,
                   FileName = file.FileName,
                   ContentType = file.ContentType
               },"dishes",FileValidationOptions.ImageUpload,ct);

                savedFilePaths.Add(uploadResult.RelativePath);

                var media = await _mediaRepo.AddMediaAsync(new MediaAsset
                {
                    // Store RelativePath — resolve to PublicUrl at read time via _fileStorage.GetPublicUrl()
                    Url = uploadResult.RelativePath,
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

            if (video is not null)
            {
                var uploadResult = await _fileStorage.SaveAsync(
                    new FileUploadRequest
                    {
                        Stream = video.Stream,
                        FileName = video.FileName,
                        ContentType = video.ContentType
                    },
                    "dish-videos",
                    FileValidationOptions.DishVideo,
                    ct);

                savedFilePaths.Add(uploadResult.RelativePath);

                var videoTypeId = await _lookupResolver.GetIdAsync(
                    (ushort)Enum.LookupType.MediaType,
                    MediaTypeCode.VIDEO,
                    ct);

                var media = await _mediaRepo.AddMediaAsync(new MediaAsset
                {
                    Url = uploadResult.RelativePath,
                    MimeType = video.ContentType,
                    MediaTypeLvId = videoTypeId,
                    CreatedAt = DateTime.UtcNow
                }, ct);

                await _mediaRepo.AddDishMediaAsync(new DishMedium
                {
                    DishId = dish.DishId,
                    MediaId = media.MediaId,
                    IsPrimary = false
                }, ct);
            }

            await _uow.CommitAsync(ct);
            return dish.DishId;
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            await _fileStorage.DeleteManyAsync(savedFilePaths);
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
        var tags = await _dishRepository.GetAllActiveTagsAsync();

        return tags.Select(t => new DishTagDto
        {
            TagId = t.ValueId,
            Code = t.ValueCode,
            Name = t.ValueName,
            NameI18n = MapTranslations(t.ValueNameText, t.ValueName),
            DescriptionI18n = MapTranslations(t.ValueDescText, string.Empty)
        }).ToList();
    }

    public async Task<DishDetailForActionsDto> GetDishByIdAsync(long dishId, CancellationToken cancellationToken)
    {
        var dish = await _dishRepository.FindByIdForActionAsync(dishId, cancellationToken); // Find dish by id
        var dishTags = await _dishRepository.FindTagByDishIdAsync(dishId, cancellationToken); // Find tag for dish

        if (dish == null)
            throw new KeyNotFoundException($"Dish with ID {dishId} not found!");

        return DishMapper.ToDetailDto(dish, dishTags, _fileStorage); // Map to DTO
    }

    public async Task UpdateDishAsync(
        UpdateDishRequest request,
        IReadOnlyList<MediaFileInput> staticImages,
        IReadOnlyList<MediaFileInput> images360,
        MediaFileInput? video,
        IReadOnlyList<long> removedMediaIds,
        CancellationToken ct)
    {
        var savedFilePaths = new List<string>();
        var filesToDeleteAfterCommit = new List<string>();

        await _uow.BeginTransactionAsync(ct);

        try
        {
            var dish = await _dishRepository.FindByIdForActionAsync(request.DishId, ct)
                        ?? throw new KeyNotFoundException($"Dish with ID {request.DishId} not found!");

            dish.CategoryId = request.CategoryId;
            dish.Price = request.Price;
            dish.IsOnline = request.IsOnline;
            dish.ChefRecommended = request.ChefRecommended;
            dish.Calories = request.Calories;
            dish.PrepTimeMinutes = request.PrepTimeMinutes;
            dish.CookTimeMinutes = request.CookTimeMinutes;
            dish.DishStatusLvId = request.DishStatusLvId;
            dish.DishName = request.I18n["en"].DishName;

            var existingTagIds = await _dishRepository.GetTagIdsByDishIdAsync(dish.DishId, ct);
            var newTagIds = request.TagIds.Distinct().ToList();
            var tagsToRemove = existingTagIds.Where(id => !newTagIds.Contains(id)).ToList();
            var tagsToAdd = newTagIds.Where(id => !existingTagIds.Contains(id)).ToList();

            if (tagsToRemove.Any())
                await _dishRepository.RemoveDishTagsAsync(dish.DishId, tagsToRemove, ct);

            foreach (var tagId in tagsToAdd)
                await _dishRepository.AddDishTagAsync(new DishTag { DishId = dish.DishId, TagId = tagId }, ct);

            var newTextIds = await _dishI18nService.CreateOrUpdateDishI18nTextsAsync(
              new DishI18nTextIds
              {
                  DishNameTextId = dish.DishNameTextId,
                  DescriptionTextId = dish.DescriptionTextId,
                  ShortDescriptionTextId = dish.ShortDescriptionTextId,
                  SloganTextId = dish.SloganTextId,
                  NoteTextId = dish.NoteTextId
              }, request.I18n, ct);

            dish.DishNameTextId = newTextIds.DishNameTextId;
            dish.DescriptionTextId = newTextIds.DescriptionTextId;
            dish.ShortDescriptionTextId = newTextIds.ShortDescriptionTextId;
            dish.SloganTextId = newTextIds.SloganTextId;
            dish.NoteTextId = newTextIds.NoteTextId;

            if (removedMediaIds.Any())
            {
                var medias = await _mediaRepo.GetDishMediaByIdsAsync(dish.DishId, removedMediaIds, ct);
                foreach (var m in medias)
                {
                    // Url is RelativePath — pass directly to DeleteAsync, no .Replace() needed
                    filesToDeleteAfterCommit.Add(m.Media.Url);
                    await _mediaRepo.RemoveDishMediaAsync(m, ct);
                    await _mediaRepo.RemoveMediaAsync(m.Media, ct);
                }
            }

            var imageTypeId = await _lookupResolver.GetIdAsync((ushort)Enum.LookupType.MediaType, MediaTypeCode.IMAGE, ct);

            foreach (var file in staticImages)
            {
                var uploadResult = await _fileStorage.SaveAsync(
                    new FileUploadRequest
                    {
                        Stream = file.Stream,
                        FileName = file.FileName,
                        ContentType = file.ContentType
                    },
                 "dishes", FileValidationOptions.ImageUpload, ct);

                savedFilePaths.Add(uploadResult.RelativePath);

                var media = await _mediaRepo.AddMediaAsync(new MediaAsset
                {
                    // Store RelativePath — resolve to PublicUrl at read time via _fileStorage.GetPublicUrl()
                    Url = uploadResult.RelativePath,
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

            if (video is not null)
            {
                // get old video
                var existingVideos = await _mediaRepo.GetDishMediaByTypeAsync(
                    dish.DishId,
                    MediaTypeCode.VIDEO,
                    ct);

                foreach (var v in existingVideos)
                {
                    filesToDeleteAfterCommit.Add(v.Media.Url);

                    await _mediaRepo.RemoveDishMediaAsync(v, ct);
                    await _mediaRepo.RemoveMediaAsync(v.Media, ct);
                }

                var uploadResult = await _fileStorage.SaveAsync(
                    new FileUploadRequest
                    {
                        Stream = video.Stream,
                        FileName = video.FileName,
                        ContentType = video.ContentType
                    },
                    "dish-videos",
                    FileValidationOptions.DishVideo,
                    ct);

                savedFilePaths.Add(uploadResult.RelativePath);

                var videoTypeId = await _lookupResolver.GetIdAsync(
                    (ushort)Enum.LookupType.MediaType,
                    MediaTypeCode.VIDEO,
                    ct);

                var media = await _mediaRepo.AddMediaAsync(new MediaAsset
                {
                    Url = uploadResult.RelativePath,
                    MimeType = video.ContentType,
                    MediaTypeLvId = videoTypeId,
                    CreatedAt = DateTime.UtcNow
                }, ct);

                await _mediaRepo.AddDishMediaAsync(new DishMedium
                {
                    DishId = dish.DishId,
                    MediaId = media.MediaId
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            await _fileStorage.DeleteManyAsync(savedFilePaths);
            throw;
        }

        // Delete old files AFTER successful commit — best-effort, logged by DeleteManyAsync
        await _fileStorage.DeleteManyAsync(filesToDeleteAfterCommit);
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
        var imageUrl = dish.DishMedia
            .FirstOrDefault(m => m.IsPrimary == true)?.Media?.Url
            ?? dish.DishMedia
                .FirstOrDefault()?.Media?.Url;

        var dto = new DishPosResponseDto
        {
            DishId = dish.DishId,
            CategoryId = dish.CategoryId,
            Price = dish.Price,
            ChefRecommended = dish.ChefRecommended,
            DisplayOrder = dish.DisplayOrder,
            ImageUrl = imageUrl != null ? _fileStorage.GetPublicUrl(imageUrl) : null
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

    private async Task<bool> ShouldShowMedia(string settingKey, CancellationToken ct)
    {
        var show = await _systemSettingService.GetBoolAsync(settingKey, true, ct);
        return show ?? true;
    }
}

