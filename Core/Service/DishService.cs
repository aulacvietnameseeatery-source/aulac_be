using Core.DTO.Dish;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Dish;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.I18n;
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

            await _uow.BeginTransactionAsync(ct);

            try
            {
                // I18n
                var textIds = await _dishI18nService.CreateDishI18nTextsAsync(request.I18n, ct);

                // Dish
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

                await _dishRepo.AddAsync(dish, ct);

                // Media
                foreach (var file in staticImages)
                {
                    var path = await _fileStorage.SaveAsync(file.OpenReadStream(), "static", "dishes", ct);
                    uploadedFiles.Add(path);

                    var mediaTypeid = _lookupResolver.GetIdAsync((ushort)LookupType.MediaType, MediaTypeCode.IMAGE, ct).Result;


                    var media = await _mediaRepo.AddMediaAsync(new MediaAsset
                    {
                        Url = path,
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

                await _uow.CommitAsync(ct);
                return dish.DishId;
            }
            catch
            {
                await _uow.RollbackAsync(ct);

                foreach (var f in uploadedFiles)
                    await _fileStorage.DeleteAsync(f);

                throw;
            }
        }
    }

}
