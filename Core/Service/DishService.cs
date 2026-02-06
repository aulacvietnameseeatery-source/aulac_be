using Core.DTO.Dish;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Dish;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.I18n;
using Core.Mappers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LookupType = Core.Enum.LookupType;

namespace Core.Service
{
    public class DishService : IDishService
    {
        private readonly IDishI18nService _dishI18nService;
        private readonly ILookupResolver _lookupResolver;
        private readonly IDishRepository _dishRepo;
        private readonly IMediaRepository _mediaRepo;
        private readonly IUnitOfWork _uow;
        private readonly IFileStorage _fileStorage;

        public DishService(
            IDishRepository dishRepo,
            IMediaRepository mediaRepo,
            IUnitOfWork uow,
            IFileStorage fileStorage,
            IDishI18nService dishI18NService,
            ILookupResolver lookupResolver)
        {
            _dishRepo = dishRepo;
            _mediaRepo = mediaRepo;
            _uow = uow;
            _fileStorage = fileStorage;
            _dishI18nService = dishI18NService;
            _lookupResolver = lookupResolver;
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

                await _dishRepo.AddAsync(dish, ct); // Save dish to DB

                // Add tag to dish
                await _dishRepo.AddDishTagAsync(new DishTag
                {
                    DishId = dish.DishId,
                    TagId = request.TagId
                }, ct);

                // Save static images and link to dish
                foreach (var file in staticImages)
                {
                    var path = await _fileStorage.SaveAsync(file.OpenReadStream(), file.FileName, "dishes", ct); // Save file
                    uploadedFiles.Add(path);

                    var mediaTypeid = _lookupResolver.GetIdAsync((ushort)LookupType.MediaType, MediaTypeCode.IMAGE, ct).Result; // Get media type id

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
            var entities = await _dishRepo.GetActiveDishStatusEntitiesAsync(); // Get active dish statuses

            return entities.Select(lv => new ActiveDishStatusDto
            {
                DishStatusLvId = lv.ValueId,
                ValueName = lv.ValueName,
                ValueCode = lv.ValueCode
            }).ToList();
        }

        public async Task<List<DishCategorySimpleDto>> GetAllDishCategoriesAsync()
        {
            var categories = await _dishRepo.GetAllDishCategoriesAsync(); // Get all dish categories

            return categories.Select(c => new DishCategorySimpleDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            }).ToList();
        }

        public async Task<List<DishTagDto>> GetAllActiveTagsAsync()
        {
            var tags = await _dishRepo.GetAllActiveTagsAsync(); // Get all active tags

            return tags.Select(t => new DishTagDto
            {
                TagId = t.ValueId,
                Code = t.ValueCode,
                Name = t.ValueName
            }).ToList();
        }

        public async Task<DishDetailDto> GetDishByIdAsync(long dishId, CancellationToken cancellationToken)
        {
            var dish = await _dishRepo.FindByIdAsync(dishId, cancellationToken); // Find dish by id
            var dishTag = await _dishRepo.FindTagByDishIdAsync(dishId, cancellationToken); // Find tag for dish

            if (dish == null)
                throw new KeyNotFoundException($"Dish with ID {dishId} not found!");

            if (dishTag == null)
                throw new KeyNotFoundException($"Dish Tag with ID {dishId} not found!");

            return DishMapper.ToDetailDto(dish, dishTag); // Map to DTO
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
                var dish = await _dishRepo.FindByIdAsync(request.DishId, ct)
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

                var dishTag = await _dishRepo.FindTagByDishIdAsync(request.DishId, ct);
                if (dishTag != null) dishTag.TagId = request.TagId;

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
                    (ushort)LookupType.MediaType,
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
    }

}
