using Core.DTO.Dish;
using Core.DTO.DishCategory;
using Core.DTO.General;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.I18n;
using Core.Service;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace Tests.Services;

/// <summary>
/// Unit Test — DishService + DishCategoryService
/// Code Module : Core/Service/DishService.cs, Core/Service/DishCategoryService.cs
/// Methods     : GetDishByIdAsync (detail), UpdateDishStatusAsync, GetDishesForAdminAsync,
///               GetDishesForCustomerAsync, GetAllCategoriesAsync (dish), GetDishStatusesAsync,
///               CreateDishAsync, GetActiveDishStatusesAsync, GetAllDishCategoriesAsync,
///               GetAllActiveTagsAsync, GetDishByIdAsync (actions), UpdateDishAsync, GetPosDishesAsync,
///               GetAllCategoriesAsync (category), GetCategoryByIdAsync, CreateCategoryAsync,
///               UpdateCategoryAsync, ToggleCategoryStatusAsync
/// Created By  : AI Agent
/// Executed By : Tester
/// Test Req.   : Validate dish CRUD operations including detail retrieval, status update,
///               listing for admin/customer, dish creation/update with media and i18n,
///               category CRUD with i18n, and status toggling.
/// </summary>
public class DishAndDishCategoryServiceTests
{
    // ══════════════════════════════════════════════════════════════════════
    //  DishService — Mocks
    // ══════════════════════════════════════════════════════════════════════

    private readonly Mock<IDishRepository> _dishRepoMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<ILogger<DishService>> _dishLoggerMock = new();
    private readonly Mock<IDishI18nService> _dishI18nServiceMock = new();
    private readonly Mock<IMediaRepository> _mediaRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<ISystemSettingService> _systemSettingServiceMock = new();

    // ══════════════════════════════════════════════════════════════════════
    //  DishCategoryService — Mocks
    // ══════════════════════════════════════════════════════════════════════

    private readonly Mock<IDishCategoryRepository> _dishCategoryRepoMock = new();
    private readonly Mock<Ii18nService> _i18nServiceMock = new();
    private readonly Mock<ILogger<DishCategoryService>> _catLoggerMock = new();

    // ══════════════════════════════════════════════════════════════════════
    //  Factory Methods
    // ══════════════════════════════════════════════════════════════════════

    private DishService CreateDishService() => new(
        _dishRepoMock.Object,
        _lookupResolverMock.Object,
        _dishLoggerMock.Object,
        _dishI18nServiceMock.Object,
        _mediaRepoMock.Object,
        _uowMock.Object,
        _fileStorageMock.Object,
        _systemSettingServiceMock.Object);

    private DishCategoryService CreateCategoryService() => new(
        _dishCategoryRepoMock.Object,
        _i18nServiceMock.Object,
        _catLoggerMock.Object);

    // ══════════════════════════════════════════════════════════════════════
    //  Test Data Helpers
    // ══════════════════════════════════════════════════════════════════════

    private static I18nText MakeI18nText(long textId, string sourceText, string sourceLang = "en", Dictionary<string, string>? translations = null)
    {
        var text = new I18nText
        {
            TextId = textId,
            TextKey = $"test.key.{textId}",
            SourceLangCode = sourceLang,
            SourceText = sourceText,
            I18nTranslations = new List<I18nTranslation>()
        };

        if (translations != null)
        {
            foreach (var kv in translations)
            {
                text.I18nTranslations.Add(new I18nTranslation
                {
                    TextId = textId,
                    LangCode = kv.Key,
                    TranslatedText = kv.Value
                });
            }
        }
        else
        {
            text.I18nTranslations.Add(new I18nTranslation { TextId = textId, LangCode = "en", TranslatedText = sourceText });
            text.I18nTranslations.Add(new I18nTranslation { TextId = textId, LangCode = "vi", TranslatedText = $"{sourceText}_vi" });
            text.I18nTranslations.Add(new I18nTranslation { TextId = textId, LangCode = "fr", TranslatedText = $"{sourceText}_fr" });
        }

        return text;
    }

    private static Dish MakeDish(long id = 1, string name = "Pho Bo", decimal price = 50000m, long categoryId = 1)
    {
        var nameText = MakeI18nText(100, name);
        var descText = MakeI18nText(101, "A delicious Vietnamese soup");
        var category = new DishCategory
        {
            CategoryId = categoryId,
            CategoryName = "Soup",
            CategoryNameText = MakeI18nText(200, "Soup")
        };

        return new Dish
        {
            DishId = id,
            DishName = name,
            DishNameText = nameText,
            DishNameTextId = 100,
            DescriptionText = descText,
            DescriptionTextId = 101,
            Price = price,
            CategoryId = categoryId,
            Category = category,
            DishStatusLvId = 10u,
            DishStatusLv = new LookupValue { ValueId = 10, ValueCode = "AVAILABLE", ValueName = "Available", TypeId = 12, SortOrder = 1 },
            Calories = 350,
            PrepTimeMinutes = 15,
            CookTimeMinutes = 30,
            IsOnline = true,
            ChefRecommended = true,
            CreatedAt = DateTime.UtcNow,
            DishMedia = new List<DishMedium>
            {
                new()
                {
                    DishId = id,
                    MediaId = 1,
                    IsPrimary = true,
                    Media = new MediaAsset
                    {
                        MediaId = 1,
                        Url = "dishes/pho.jpg",
                        MimeType = "image/jpeg",
                        MediaTypeLvId = 1
                    }
                }
            },
            Recipes = new List<Recipe>
            {
                new()
                {
                    DishId = id,
                    IngredientId = 1,
                    Quantity = 200,
                    Unit = "g",
                    Note = "Fresh noodles",
                    Ingredient = new Ingredient
                    {
                        IngredientId = 1,
                        IngredientName = "Rice Noodles",
                        IngredientNameText = MakeI18nText(300, "Rice Noodles")
                    }
                }
            }
        };
    }

    private static DishCategory MakeDishCategory(long id = 1, string name = "Soup", bool isDisabled = false)
    {
        return new DishCategory
        {
            CategoryId = id,
            CategoryName = name,
            Description = "Soup dishes",
            IsDisabled = isDisabled,
            DisPlayOrder = 1,
            CategoryNameText = MakeI18nText(200, name),
            CategoryNameTextId = 200,
            DescriptionText = MakeI18nText(201, "Soup dishes"),
            DescriptionTextId = 201
        };
    }

    private static CreateDishCategoryRequest MakeCreateCategoryRequest(string name = "Appetizer", string? desc = "Starter dishes")
    {
        return new CreateDishCategoryRequest
        {
            I18n = new Dictionary<string, CategoryI18nDto>
            {
                ["en"] = new() { Name = name, Description = desc },
                ["vi"] = new() { Name = $"{name}_vi", Description = $"{desc}_vi" }
            },
            IsDisabled = false
        };
    }

    private static UpdateDishCategoryRequest MakeUpdateCategoryRequest(string name = "Updated Category", string? desc = "Updated description")
    {
        return new UpdateDishCategoryRequest
        {
            I18n = new Dictionary<string, CategoryI18nDto>
            {
                ["en"] = new() { Name = name, Description = desc },
                ["vi"] = new() { Name = $"{name}_vi", Description = $"{desc}_vi" }
            },
            IsDisabled = false
        };
    }

    private static LookupValue MakeLookupValue(uint id, string code, string name, ushort typeId = 12)
    {
        return new LookupValue
        {
            ValueId = id,
            ValueCode = code,
            ValueName = name,
            TypeId = typeId,
            SortOrder = 1,
            IsActive = true
        };
    }

    private static GetDishesRequest MakeGetDishesRequest(int page = 1, int size = 10)
    {
        return new GetDishesRequest { PageIndex = page, PageSize = size };
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetDishByIdAsync (Detail — with langCode)
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishByIdAsync_Detail

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenDishExists_ReturnsDishDetailDto()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_video", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, "en");

        // Assert
        result.Should().NotBeNull();
        result!.DishId.Should().Be(1);
        result.DishName.Should().Be("Pho Bo");
        result.Price.Should().Be(50000m);
        result.CategoryName.Should().Be("Soup");
        result.Calories.Should().Be(350);
        result.Composition.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenDishNotFound_ReturnsNull()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Dish?)null);

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(999, "en");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenLangCodeIsNull_DefaultsToEnglish()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, null);

        // Assert
        result.Should().NotBeNull();
        result!.DishName.Should().Be("Pho Bo");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenImagesDisabled_ReturnsEmptyImageUrls()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_video", true, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, "en");

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrls.Should().BeEmpty();
        result.VideoUrl.Should().BeNull();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.UpdateDishStatusAsync
    // ══════════════════════════════════════════════════════════════════════

    #region UpdateDishStatusAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateDishStatusAsync")]
    public async Task UpdateDishStatusAsync_WhenDishExistsAndValidStatus_ReturnsUpdatedStatus()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(11u);
        _dishRepoMock.Setup(r => r.UpdateStatusAsync(1, 11u, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.UpdateDishStatusAsync(1, DishStatusCode.OUT_OF_STOCK);

        // Assert
        result.Should().NotBeNull();
        result.DishId.Should().Be(1);
        result.DishName.Should().Be("Pho Bo");
        result.StatusCode.Should().Be(DishStatusCode.OUT_OF_STOCK);
        result.StatusId.Should().Be(11u);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateDishStatusAsync")]
    public async Task UpdateDishStatusAsync_WhenDishNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Dish?)null);

        var service = CreateDishService();

        // Act
        var act = () => service.UpdateDishStatusAsync(999, DishStatusCode.AVAILABLE);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateDishStatusAsync")]
    public async Task UpdateDishStatusAsync_WhenRepoThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(11u);
        _dishRepoMock.Setup(r => r.UpdateStatusAsync(1, 11u, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));

        var service = CreateDishService();

        // Act
        var act = () => service.UpdateDishStatusAsync(1, DishStatusCode.HIDDEN);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to update dish status*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishStatusAsync")]
    public async Task UpdateDishStatusAsync_WhenStatusIsHidden_ReturnsHiddenStatus()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(12u);
        _dishRepoMock.Setup(r => r.UpdateStatusAsync(1, 12u, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.UpdateDishStatusAsync(1, DishStatusCode.HIDDEN);

        // Assert
        result.StatusCode.Should().Be(DishStatusCode.HIDDEN);
        result.StatusId.Should().Be(12u);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetDishesForAdminAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishesForAdminAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishesExist_ReturnsMappedDtos()
    {
        // Arrange
        var dishes = new List<Dish> { MakeDish(1, "Pho Bo"), MakeDish(2, "Bun Cha", 60000m) };
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dishes, 2));

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items[0].DishName.Should().Be("Pho Bo");
        items[1].DishName.Should().Be("Bun Cha");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenNoDishes_ReturnsEmpty()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish>(), 0));

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB connection error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB connection error*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetDishesForCustomerAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishesForCustomerAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenDishesExist_ReturnsMappedDisplayDtos()
    {
        // Arrange
        var dishes = new List<Dish> { MakeDish(1, "Pho Bo") };
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dishes, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].DishId.Should().Be(1);
        items[0].Price.Should().Be(50000m);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenImagesDisabled_ReturnsNullImageUrl()
    {
        // Arrange
        var dishes = new List<Dish> { MakeDish() };
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dishes, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].ImageUrl.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenNoDishes_ReturnsEmpty()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish>(), 0));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetAllCategoriesAsync (Dish filter dropdown)
    // ══════════════════════════════════════════════════════════════════════

    #region GetAllCategoriesAsync_Dish

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllCategoriesAsync_Dish")]
    public async Task GetAllCategoriesAsync_Dish_WhenCategoriesExist_ReturnsList()
    {
        // Arrange
        var categories = new List<string> { "Soup", "Appetizer", "Main Course" };
        _dishRepoMock.Setup(r => r.GetAllCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);

        var service = CreateDishService();

        // Act
        var result = await service.GetAllCategoriesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Soup");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllCategoriesAsync_Dish")]
    public async Task GetAllCategoriesAsync_Dish_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetAllCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

        var service = CreateDishService();

        // Act
        var result = await service.GetAllCategoriesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetDishStatusesAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishStatusesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishStatusesAsync")]
    public async Task GetDishStatusesAsync_WhenStatusesExist_ReturnsMappedList()
    {
        // Arrange
        var statuses = new List<LookupValue>
        {
            MakeLookupValue(10, "AVAILABLE", "Available"),
            MakeLookupValue(11, "OUT_OF_STOCK", "Out of Stock"),
            MakeLookupValue(12, "HIDDEN", "Hidden")
        };
        _dishRepoMock.Setup(r => r.GetDishStatusesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(statuses);

        var service = CreateDishService();

        // Act
        var result = await service.GetDishStatusesAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].StatusId.Should().Be(10u);
        result[0].StatusName.Should().Be("Available");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishStatusesAsync")]
    public async Task GetDishStatusesAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishStatusesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LookupValue>());

        var service = CreateDishService();

        // Act
        var result = await service.GetDishStatusesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.CreateDishAsync
    // ══════════════════════════════════════════════════════════════════════

    #region CreateDishAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenValidRequest_ReturnsDishId()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 50000m,
            IsOnline = true,
            ChefRecommended = false,
            Calories = 350,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 1, 2 },
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo", Description = "Beef noodle soup" }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 100, DescriptionTextId = 101 };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 42)
            .Returns(Task.CompletedTask);

        _dishRepoMock.Setup(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1u);
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(42);
        _dishRepoMock.Verify(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()), Times.Once);
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenWithImages_SavesMediaAndLinks()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 100 };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 10)
            .Returns(Task.CompletedTask);

        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1u);

        var uploadResult = new FileUploadResult { RelativePath = "dishes/img.jpg", PublicUrl = "/uploads/dishes/img.jpg", OriginalFileName = "img.jpg", SizeBytes = 1024 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 50, Url = "dishes/img.jpg", MediaTypeLvId = 1 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "img.jpg", ContentType = "image/jpeg" }
        };

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, images, new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(10);
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepoMock.Verify(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenExceptionOccurs_RollsBackAndCleansUpFiles()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("I18n creation failed"));
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*I18n creation failed*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenDuplicateTagIds_DeduplicatesTags()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 1, 1, 2, 2 },
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 100 };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 1)
            .Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert — only 2 distinct tags should be added
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetActiveDishStatusesAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetActiveDishStatusesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetActiveDishStatusesAsync")]
    public async Task GetActiveDishStatusesAsync_WhenStatusesExist_ReturnsMappedList()
    {
        // Arrange
        var statuses = new List<LookupValue>
        {
            MakeLookupValue(10, "AVAILABLE", "Available"),
            MakeLookupValue(11, "OUT_OF_STOCK", "Out of Stock")
        };
        _dishRepoMock.Setup(r => r.GetActiveDishStatusEntitiesAsync()).ReturnsAsync(statuses);

        var service = CreateDishService();

        // Act
        var result = await service.GetActiveDishStatusesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].DishStatusLvId.Should().Be(10u);
        result[0].ValueCode.Should().Be("AVAILABLE");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetActiveDishStatusesAsync")]
    public async Task GetActiveDishStatusesAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetActiveDishStatusEntitiesAsync()).ReturnsAsync(new List<LookupValue>());

        var service = CreateDishService();

        // Act
        var result = await service.GetActiveDishStatusesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetAllDishCategoriesAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetAllDishCategoriesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllDishCategoriesAsync")]
    public async Task GetAllDishCategoriesAsync_WhenCategoriesExist_ReturnsMappedSimpleDtos()
    {
        // Arrange
        var categories = new List<DishCategory>
        {
            MakeDishCategory(1, "Soup"),
            MakeDishCategory(2, "Appetizer")
        };
        _dishRepoMock.Setup(r => r.GetAllDishCategoriesAsync()).ReturnsAsync(categories);

        var service = CreateDishService();

        // Act
        var result = await service.GetAllDishCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].CategoryId.Should().Be(1);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllDishCategoriesAsync")]
    public async Task GetAllDishCategoriesAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetAllDishCategoriesAsync()).ReturnsAsync(new List<DishCategory>());

        var service = CreateDishService();

        // Act
        var result = await service.GetAllDishCategoriesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetAllActiveTagsAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetAllActiveTagsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllActiveTagsAsync")]
    public async Task GetAllActiveTagsAsync_WhenTagsExist_ReturnsMappedDtos()
    {
        // Arrange
        var tags = new List<LookupValue>
        {
            new()
            {
                ValueId = 1,
                ValueCode = "SPICY",
                ValueName = "Spicy",
                TypeId = 14,
                SortOrder = 1,
                ValueNameText = MakeI18nText(400, "Spicy"),
                ValueDescText = MakeI18nText(401, "Hot and spicy dish")
            }
        };
        _dishRepoMock.Setup(r => r.GetAllActiveTagsAsync()).ReturnsAsync(tags);

        var service = CreateDishService();

        // Act
        var result = await service.GetAllActiveTagsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Code.Should().Be("SPICY");
        result[0].Name.Should().Be("Spicy");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllActiveTagsAsync")]
    public async Task GetAllActiveTagsAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetAllActiveTagsAsync()).ReturnsAsync(new List<LookupValue>());

        var service = CreateDishService();

        // Act
        var result = await service.GetAllActiveTagsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetDishByIdAsync (Actions — overload with single long param)
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishByIdAsync_Actions

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishByIdAsync_Actions")]
    public async Task GetDishByIdAsync_Actions_WhenDishExists_ReturnsDishDetailForActionsDto()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishStatusLv.ValueNameText = MakeI18nText(500, "Available");
        // Ensure DishMedia references have MediaTypeLv set for DishMapper
        foreach (var dm in dish.DishMedia)
        {
            dm.Media.MediaTypeLv = new LookupValue { ValueId = dm.Media.MediaTypeLvId, ValueCode = "IMAGE", ValueName = "Image", TypeId = 5, SortOrder = 1 };
        }
        var dishTags = new List<DishTag>
        {
            new() { DishTagId = 1, DishId = 1, TagId = 1, Tag = new LookupValue { ValueId = 1, ValueCode = "SPICY", ValueName = "Spicy", TypeId = 14, SortOrder = 1, ValueNameText = MakeI18nText(600, "Spicy") } }
        };

        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _dishRepoMock.Setup(r => r.FindTagByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dishTags);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, default);

        // Assert
        result.Should().NotBeNull();
        result.DishId.Should().Be(1);
        result.CategoryId.Should().Be(1);
        result.Price.Should().Be(50000m);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishByIdAsync_Actions")]
    public async Task GetDishByIdAsync_Actions_WhenDishNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Dish?)null);
        _dishRepoMock.Setup(r => r.FindTagByDishIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(new List<DishTag>());

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishByIdAsync(999, default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.GetPosDishesAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetPosDishesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPosDishesAsync")]
    public async Task GetPosDishesAsync_WhenActiveTrue_ReturnsActiveDishes()
    {
        // Arrange
        var dish = MakeDish();
        dish.SloganText = MakeI18nText(700, "Best Pho in town");
        dish.NoteText = MakeI18nText(701, "Served hot");
        dish.ShortDescriptionText = MakeI18nText(702, "Traditional pho");

        _dishRepoMock.Setup(r => r.GetActiveDishesAsync()).ReturnsAsync(new List<Dish> { dish });
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetPosDishesAsync(true);

        // Assert
        result.Should().HaveCount(1);
        result[0].DishId.Should().Be(1);
        result[0].Price.Should().Be(50000m);
        result[0].I18n.Should().ContainKey("en");
        result[0].I18n.Should().ContainKey("vi");
        result[0].I18n.Should().ContainKey("fr");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPosDishesAsync")]
    public async Task GetPosDishesAsync_WhenActiveFalse_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateDishService();

        // Act
        var result = await service.GetPosDishesAsync(false);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPosDishesAsync")]
    public async Task GetPosDishesAsync_WhenNoActiveDishes_ReturnsEmptyList()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetActiveDishesAsync()).ReturnsAsync(new List<Dish>());

        var service = CreateDishService();

        // Act
        var result = await service.GetPosDishesAsync(true);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishCategoryService.GetAllCategoriesAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetAllCategoriesAsync_Category

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllCategoriesAsync_Category")]
    public async Task GetAllCategoriesAsync_Category_WhenCategoriesExist_ReturnsPagedResult()
    {
        // Arrange
        var pagedResult = new PagedResultDTO<DishCategoryDto>
        {
            PageData = new List<DishCategoryDto>
            {
                new() { CategoryId = 1, CategoryName = "Soup" },
                new() { CategoryId = 2, CategoryName = "Appetizer" }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 2
        };
        var query = new DishCategoryListQueryDTO { PageIndex = 1, PageSize = 10 };
        _dishCategoryRepoMock.Setup(r => r.GetAllCategoriesAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetAllCategoriesAsync(query);

        // Assert
        result.TotalCount.Should().Be(2);
        result.PageData.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllCategoriesAsync_Category")]
    public async Task GetAllCategoriesAsync_Category_WhenEmpty_ReturnsEmptyPagedResult()
    {
        // Arrange
        var pagedResult = new PagedResultDTO<DishCategoryDto>
        {
            PageData = new List<DishCategoryDto>(),
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 0
        };
        var query = new DishCategoryListQueryDTO { PageIndex = 1, PageSize = 10 };
        _dishCategoryRepoMock.Setup(r => r.GetAllCategoriesAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetAllCategoriesAsync(query);

        // Assert
        result.TotalCount.Should().Be(0);
        result.PageData.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishCategoryService.GetCategoryByIdAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetCategoryByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCategoryByIdAsync")]
    public async Task GetCategoryByIdAsync_WhenCategoryExists_ReturnsMappedDto()
    {
        // Arrange
        var category = MakeDishCategory(1, "Soup");
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetCategoryByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(1);
        result.CategoryName.Should().Be("Soup");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetCategoryByIdAsync")]
    public async Task GetCategoryByIdAsync_WhenCategoryNotFound_ReturnsNull()
    {
        // Arrange
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((DishCategory?)null);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetCategoryByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetCategoryByIdAsync")]
    public async Task GetCategoryByIdAsync_WhenCategoryHasNoTranslations_ReturnsFallbackValues()
    {
        // Arrange
        var category = new DishCategory
        {
            CategoryId = 1,
            CategoryName = "Soup",
            Description = "Soup dishes",
            IsDisabled = false,
            CategoryNameText = null,
            DescriptionText = null
        };
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetCategoryByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.NameI18n.En.Should().Be("Soup");
        result.NameI18n.Vi.Should().Be("Soup");
        result.NameI18n.Fr.Should().Be("Soup");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishCategoryService.CreateCategoryAsync
    // ══════════════════════════════════════════════════════════════════════

    #region CreateCategoryAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateCategoryAsync")]
    public async Task CreateCategoryAsync_WhenValidRequest_ReturnsCreatedCategory()
    {
        // Arrange
        var request = MakeCreateCategoryRequest("Appetizer", "Starter dishes");
        var createdCategory = MakeDishCategory(5, "Appetizer");

        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Appetizer", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Name", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(200L);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Description", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(201L);
        _dishCategoryRepoMock.Setup(r => r.GetMaxDisplayOrderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _dishCategoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCategory);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(createdCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.CreateCategoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CategoryId.Should().Be(5);
        result.CategoryName.Should().Be("Appetizer");
        _dishCategoryRepoMock.Verify(r => r.CreateAsync(It.Is<DishCategory>(c =>
            c.CategoryName == "Appetizer" &&
            c.CategoryNameTextId == 200 &&
            c.DescriptionTextId == 201 &&
            c.DisPlayOrder == 4
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCategoryAsync")]
    public async Task CreateCategoryAsync_WhenNameAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var request = MakeCreateCategoryRequest("Soup");
        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Soup", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateCategoryService();

        // Act
        var act = () => service.CreateCategoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Soup*already exists*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateCategoryAsync")]
    public async Task CreateCategoryAsync_WhenNoDescription_CreatesWithoutDescriptionText()
    {
        // Arrange
        var request = new CreateDishCategoryRequest
        {
            I18n = new Dictionary<string, CategoryI18nDto>
            {
                ["en"] = new() { Name = "Simple", Description = null }
            },
            IsDisabled = false
        };
        var createdCategory = new DishCategory
        {
            CategoryId = 10,
            CategoryName = "Simple",
            CategoryNameText = MakeI18nText(300, "Simple"),
            CategoryNameTextId = 300
        };

        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Simple", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Name", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(300L);
        _dishCategoryRepoMock.Setup(r => r.GetMaxDisplayOrderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _dishCategoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCategory);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(createdCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.CreateCategoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        _dishCategoryRepoMock.Verify(r => r.CreateAsync(It.Is<DishCategory>(c =>
            c.DescriptionTextId == null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishCategoryService.UpdateCategoryAsync
    // ══════════════════════════════════════════════════════════════════════

    #region UpdateCategoryAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateCategoryAsync")]
    public async Task UpdateCategoryAsync_WhenValidRequest_ReturnsUpdatedCategory()
    {
        // Arrange
        var existingCategory = MakeDishCategory(1, "Soup");
        var updatedCategory = MakeDishCategory(1, "Updated Soup");
        var request = MakeUpdateCategoryRequest("Updated Soup", "Updated description");

        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);
        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Updated Soup", 1L, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.UpdateStringsAsync(200, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _i18nServiceMock.Setup(s => s.UpdateStringsAsync(201, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCategory);
        _dishCategoryRepoMock.SetupSequence(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory)
            .ReturnsAsync(updatedCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.UpdateCategoryAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        _dishCategoryRepoMock.Verify(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCategoryAsync")]
    public async Task UpdateCategoryAsync_WhenCategoryNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((DishCategory?)null);
        var request = MakeUpdateCategoryRequest();

        var service = CreateCategoryService();

        // Act
        var act = () => service.UpdateCategoryAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCategoryAsync")]
    public async Task UpdateCategoryAsync_WhenNameConflicts_ThrowsConflictException()
    {
        // Arrange
        var existingCategory = MakeDishCategory(1, "Soup");
        var request = MakeUpdateCategoryRequest("Appetizer");

        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCategory);
        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Appetizer", 1L, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateCategoryService();

        // Act
        var act = () => service.UpdateCategoryAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Appetizer*already exists*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateCategoryAsync")]
    public async Task UpdateCategoryAsync_WhenNoExistingNameTextId_CreatesNewI18nText()
    {
        // Arrange
        var existingCategory = new DishCategory
        {
            CategoryId = 1,
            CategoryName = "Soup",
            CategoryNameTextId = null,
            DescriptionTextId = null,
            CategoryNameText = null,
            DescriptionText = null
        };
        var request = MakeUpdateCategoryRequest("Soup Updated", "New desc");
        var updatedCategory = MakeDishCategory(1, "Soup Updated");

        _dishCategoryRepoMock.SetupSequence(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory)
            .ReturnsAsync(updatedCategory);
        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Soup Updated", 1L, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Name", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(500L);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Description", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(501L);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.UpdateCategoryAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        existingCategory.CategoryNameTextId.Should().Be(500L);
        existingCategory.DescriptionTextId.Should().Be(501L);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DishCategoryService.ToggleCategoryStatusAsync
    // ══════════════════════════════════════════════════════════════════════

    #region ToggleCategoryStatusAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ToggleCategoryStatusAsync")]
    public async Task ToggleCategoryStatusAsync_WhenEnabling_SetsIsDisabledFalse()
    {
        // Arrange
        var category = MakeDishCategory(1, "Soup", isDisabled: true);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var service = CreateCategoryService();

        // Act
        var result = await service.ToggleCategoryStatusAsync(1, false);

        // Assert
        result.Should().NotBeNull();
        category.IsDisabled.Should().BeFalse();
        _dishCategoryRepoMock.Verify(r => r.UpdateAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ToggleCategoryStatusAsync")]
    public async Task ToggleCategoryStatusAsync_WhenDisabling_SetsIsDisabledTrue()
    {
        // Arrange
        var category = MakeDishCategory(1, "Soup", isDisabled: false);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var service = CreateCategoryService();

        // Act
        var result = await service.ToggleCategoryStatusAsync(1, true);

        // Assert
        result.Should().NotBeNull();
        category.IsDisabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ToggleCategoryStatusAsync")]
    public async Task ToggleCategoryStatusAsync_WhenCategoryNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((DishCategory?)null);

        var service = CreateCategoryService();

        // Act
        var act = () => service.ToggleCategoryStatusAsync(999, true);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*not found*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ToggleCategoryStatusAsync")]
    public async Task ToggleCategoryStatusAsync_WhenAlreadyDisabled_StaysDisabled()
    {
        // Arrange
        var category = MakeDishCategory(1, "Soup", isDisabled: true);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var service = CreateCategoryService();

        // Act
        var result = await service.ToggleCategoryStatusAsync(1, true);

        // Assert
        category.IsDisabled.Should().BeTrue();
    }

    #endregion
}
