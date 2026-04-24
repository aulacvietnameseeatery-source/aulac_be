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

        // Verify log message: LogInformation on successful status update
        _dishLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

        // Verify log message: LogWarning when dish not found
        _dishLoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("999")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

        // Verify log message: LogError when exception occurs during status update
        _dishLoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishesExist_MapsAllDtoFieldsCorrectly()
    {
        // Arrange
        var dish = MakeDish(1, "Pho Bo", 50000m);
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        totalCount.Should().Be(1);
        var dto = items[0];
        dto.DishId.Should().Be(1);
        dto.DishName.Should().Be("Pho Bo");
        dto.CategoryName.Should().Be("Soup");
        dto.Price.Should().Be(50000m);
        dto.Status.Should().Be("Available");
        dto.StatusId.Should().Be(10u);
        dto.IsOnline.Should().BeTrue();
        dto.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishesExist_MapsI18nFieldsCorrectly()
    {
        // Arrange
        var dish = MakeDish(1, "Pho Bo");
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        var dto = items[0];
        dto.NameI18n.En.Should().Be("Pho Bo");
        dto.NameI18n.Vi.Should().Be("Pho Bo_vi");
        dto.NameI18n.Fr.Should().Be("Pho Bo_fr");
        dto.DescriptionI18n.En.Should().Be("A delicious Vietnamese soup");
        dto.DescriptionI18n.Vi.Should().Be("A delicious Vietnamese soup_vi");
        dto.DescriptionI18n.Fr.Should().Be("A delicious Vietnamese soup_fr");
        dto.CategoryNameI18n.En.Should().Be("Soup");
        dto.CategoryNameI18n.Vi.Should().Be("Soup_vi");
        dto.CategoryNameI18n.Fr.Should().Be("Soup_fr");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishHasNullCategory_ReturnsCategoryUncategorized()
    {
        // Arrange
        var dish = MakeDish();
        dish.Category = null!;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        items[0].CategoryName.Should().Be("Uncategorized");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishHasNullStatus_ReturnsStatusUnknown()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishStatusLv = null;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        items[0].Status.Should().Be("Unknown");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishHasNullIsOnline_ReturnsFalse()
    {
        // Arrange
        var dish = MakeDish();
        dish.IsOnline = null;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        items[0].IsOnline.Should().BeFalse();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishHasNullI18nTexts_ReturnsEmptyStrings()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishNameText = null;
        dish.DescriptionText = null;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        items[0].NameI18n.Vi.Should().BeEmpty();
        items[0].NameI18n.En.Should().BeEmpty();
        items[0].NameI18n.Fr.Should().BeEmpty();
        items[0].DescriptionI18n.Vi.Should().BeEmpty();
        items[0].DescriptionI18n.En.Should().BeEmpty();
        items[0].DescriptionI18n.Fr.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenRepoThrows_LogsErrorBeforeRethrowing()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Timeout"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Timeout*");
        _dishLoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting dishes for admin")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenCategoryHasNullCategoryNameText_ReturnsEmptyI18nStrings()
    {
        // Arrange
        var dish = MakeDish();
        dish.Category.CategoryNameText = null;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        items[0].CategoryNameI18n.Vi.Should().BeEmpty();
        items[0].CategoryNameI18n.En.Should().BeEmpty();
        items[0].CategoryNameI18n.Fr.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenMultipleDishes_ReturnsTotalCountMatchingRepo()
    {
        // Arrange
        var dishes = Enumerable.Range(1, 5)
            .Select(i => MakeDish(i, $"Dish {i}", 10000m * i))
            .ToList();
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dishes, 25));

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        items.Should().HaveCount(5);
        totalCount.Should().Be(25);
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

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenNoPrimaryMedia_FallsBackToFirstMedia()
    {
        // Arrange
        var dish = MakeDish();
        // Set all media to non-primary
        foreach (var dm in dish.DishMedia) dm.IsPrimary = false;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].ImageUrl.Should().Be("/uploads/dishes/pho.jpg");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenChefRecommendedTrue_MapsCorrectly()
    {
        // Arrange
        var dish = MakeDish();
        dish.ChefRecommended = true;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].IsChefRecommended.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenNoMedia_ReturnsNullImageUrl()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishMedia = new List<DishMedium>();
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].ImageUrl.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenNullI18nTexts_FallsBackCorrectly()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishNameText = null!;
        dish.DescriptionText = null;
        dish.Category!.CategoryNameText = null;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        // MapTranslations(null, fallback) returns fallback for all three languages
        var dto = items[0];
        dto.DishName.En.Should().Be(dish.DishName);
        dto.DishName.Vi.Should().Be(dish.DishName);
        dto.DishName.Fr.Should().Be(dish.DishName);
        dto.Description.En.Should().BeEmpty();
        dto.Description.Vi.Should().BeEmpty();
        dto.Description.Fr.Should().BeEmpty();
        dto.CategoryName.En.Should().Be(dish.Category.CategoryName);
        dto.CategoryName.Vi.Should().Be(dish.Category.CategoryName);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenSettingServiceThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { MakeDish() }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Settings unavailable"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Settings unavailable*");
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

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllCategoriesAsync_Category")]
    public async Task GetAllCategoriesAsync_Category_WhenSearchProvided_PassesQueryToRepo()
    {
        // Arrange
        var pagedResult = new PagedResultDTO<DishCategoryDto>
        {
            PageData = new List<DishCategoryDto>
            {
                new() { CategoryId = 1, CategoryName = "Soup" }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 1
        };
        var query = new DishCategoryListQueryDTO { PageIndex = 1, PageSize = 10, Search = "Soup" };
        _dishCategoryRepoMock.Setup(r => r.GetAllCategoriesAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetAllCategoriesAsync(query);

        // Assert
        result.TotalCount.Should().Be(1);
        result.PageData.Should().HaveCount(1);
        _dishCategoryRepoMock.Verify(r => r.GetAllCategoriesAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllCategoriesAsync_Category")]
    public async Task GetAllCategoriesAsync_Category_WhenFilterByDisabled_PassesFilterToRepo()
    {
        // Arrange
        var pagedResult = new PagedResultDTO<DishCategoryDto>
        {
            PageData = new List<DishCategoryDto>
            {
                new() { CategoryId = 3, CategoryName = "Old Menu", IsDisabled = true }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 1
        };
        var query = new DishCategoryListQueryDTO { PageIndex = 1, PageSize = 10, IsDisabled = true };
        _dishCategoryRepoMock.Setup(r => r.GetAllCategoriesAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetAllCategoriesAsync(query);

        // Assert
        result.TotalCount.Should().Be(1);
        result.PageData.Should().OnlyContain(c => c.IsDisabled == true);
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

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetCategoryByIdAsync")]
    public async Task GetCategoryByIdAsync_WhenCategoryIsDisabled_ReturnsIsDisabledTrue()
    {
        // Arrange
        var category = MakeDishCategory(1, "Soup", isDisabled: true);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var service = CreateCategoryService();

        // Act
        var result = await service.GetCategoryByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.IsDisabled.Should().BeTrue();
        result.CategoryId.Should().Be(1);
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

        // Verify log message: LogInformation on successful category creation
        _catLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Appetizer")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

        // Verify log message: LogWarning when duplicate name
        _catLoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Soup")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateCategoryAsync")]
    public async Task CreateCategoryAsync_WhenIsDisabledTrue_CreatesDisabledCategory()
    {
        // Arrange
        var request = new CreateDishCategoryRequest
        {
            I18n = new Dictionary<string, CategoryI18nDto>
            {
                ["en"] = new() { Name = "Seasonal", Description = "Seasonal menu" }
            },
            IsDisabled = true
        };
        var createdCategory = MakeDishCategory(11, "Seasonal", isDisabled: true);

        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Seasonal", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Name", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(400L);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Description", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(401L);
        _dishCategoryRepoMock.Setup(r => r.GetMaxDisplayOrderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _dishCategoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCategory);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(createdCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.CreateCategoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        _dishCategoryRepoMock.Verify(r => r.CreateAsync(It.Is<DishCategory>(c =>
            c.IsDisabled == true &&
            c.DisPlayOrder == 6
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

        // Verify log message: LogWarning when category not found for update
        _catLoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("999")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

        // Verify log message: LogWarning when name conflict on update
        _catLoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Appetizer")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateCategoryAsync")]
    public async Task UpdateCategoryAsync_WhenNoDescription_SkipsDescriptionI18nCreation()
    {
        // Arrange
        var existingCategory = MakeDishCategory(1, "Soup");
        var request = new UpdateDishCategoryRequest
        {
            I18n = new Dictionary<string, CategoryI18nDto>
            {
                ["en"] = new() { Name = "Soup Updated", Description = null }
            },
            IsDisabled = false
        };
        var updatedCategory = MakeDishCategory(1, "Soup Updated");

        _dishCategoryRepoMock.SetupSequence(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory)
            .ReturnsAsync(updatedCategory);
        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Soup Updated", 1L, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.UpdateStringsAsync(200, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.UpdateCategoryAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        // Description i18n should NOT be created since no description provided
        _i18nServiceMock.Verify(s => s.CreateAsync(
            It.IsAny<string>(), "Dish Category Description", "en",
            It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
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

        // Verify log message: LogInformation on successful toggle
        _catLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Toggled")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

        // Verify log message: LogWarning when category not found for toggle
        _catLoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("999")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

    // ══════════════════════════════════════════════════════════════════════
    //  DishService.UpdateDishAsync
    // ══════════════════════════════════════════════════════════════════════

    #region UpdateDishAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenValidRequest_UpdatesDishSuccessfully()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 2,
            Price = 60000m,
            IsOnline = true,
            ChefRecommended = true,
            Calories = 400,
            PrepTimeMinutes = 20,
            CookTimeMinutes = 35,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 1, 3 },
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo Updated", Description = "Updated desc" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100, DescriptionTextId = 101 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint> { 1, 2 });
        _dishRepoMock.Setup(r => r.RemoveDishTagsAsync(1, It.IsAny<List<uint>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert
        existingDish.CategoryId.Should().Be(2);
        existingDish.Price.Should().Be(60000m);
        existingDish.DishName.Should().Be("Pho Bo Updated");
        _dishRepoMock.Verify(r => r.RemoveDishTagsAsync(1, It.Is<List<uint>>(t => t.Contains(2u)), It.IsAny<CancellationToken>()), Times.Once);
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.Is<DishTag>(dt => dt.TagId == 3), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenWithNewImages_SavesMediaAndLinks()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(1u);

        var uploadResult = new FileUploadResult { RelativePath = "dishes/new_img.jpg", PublicUrl = "/uploads/dishes/new_img.jpg", OriginalFileName = "new_img.jpg", SizeBytes = 2048 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);
        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 60, Url = "dishes/new_img.jpg", MediaTypeLvId = 1 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "new_img.jpg", ContentType = "image/jpeg" }
        };

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, images, new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepoMock.Verify(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenRemovingMedia_RemovesAndDeletesFiles()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var removedMediaIds = new List<long> { 1 };
        var dishMedia = new List<DishMedium>
        {
            new()
            {
                DishId = 1,
                MediaId = 1,
                IsPrimary = true,
                Media = new MediaAsset { MediaId = 1, Url = "dishes/old.jpg", MimeType = "image/jpeg", MediaTypeLvId = 1 }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _mediaRepoMock.Setup(r => r.GetDishMediaByIdsAsync(1, removedMediaIds, It.IsAny<CancellationToken>())).ReturnsAsync(dishMedia);
        _mediaRepoMock.Setup(r => r.RemoveDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mediaRepoMock.Setup(r => r.RemoveMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, removedMediaIds, default);

        // Assert
        _mediaRepoMock.Verify(r => r.RemoveDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepoMock.Verify(r => r.RemoveMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        // Old file should be deleted after commit
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.Is<IEnumerable<string>>(paths => paths.Any(p => p == "dishes/old.jpg"))), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenDishNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new UpdateDishRequest
        {
            DishId = 999,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Ghost" }
            }
        };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Dish?)null);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var act = () => service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*not found*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenExceptionOccurs_RollsBackAndCleansUpFiles()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
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
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("I18n update failed"));
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var act = () => service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*I18n update failed*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenNoTagChanges_SkipsTagOperations()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 1, 2 },
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint> { 1, 2 });
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert — no tag additions or removals needed
        _dishRepoMock.Verify(r => r.RemoveDishTagsAsync(It.IsAny<long>(), It.IsAny<List<uint>>(), It.IsAny<CancellationToken>()), Times.Never);
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenReplacingVideo_RemovesOldAndAddsNew()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };
        var existingVideoMedia = new List<DishMedium>
        {
            new()
            {
                DishId = 1,
                MediaId = 99,
                Media = new MediaAsset { MediaId = 99, Url = "dish-videos/old.mp4", MimeType = "video/mp4", MediaTypeLvId = 2 }
            }
        };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _mediaRepoMock.Setup(r => r.GetDishMediaByTypeAsync(1, MediaTypeCode.VIDEO, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVideoMedia);
        _mediaRepoMock.Setup(r => r.RemoveDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mediaRepoMock.Setup(r => r.RemoveMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var videoUploadResult = new FileUploadResult { RelativePath = "dish-videos/new.mp4", PublicUrl = "/uploads/dish-videos/new.mp4", OriginalFileName = "new.mp4", SizeBytes = 5000 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dish-videos", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(videoUploadResult);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(2u);
        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 100, Url = "dish-videos/new.mp4", MediaTypeLvId = 2 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var newVideo = new MediaFileInput { Stream = new MemoryStream(), FileName = "new.mp4", ContentType = "video/mp4" };
        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), newVideo, new List<long>(), default);

        // Assert — old video removed, new video added
        _mediaRepoMock.Verify(r => r.RemoveDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepoMock.Verify(r => r.RemoveMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.Is<MediaAsset>(m => m.Url == "dish-videos/new.mp4"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenNewVideoAndNoExistingVideo_AddsVideoWithoutRemoval()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _mediaRepoMock.Setup(r => r.GetDishMediaByTypeAsync(1, MediaTypeCode.VIDEO, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DishMedium>()); // No existing video

        var videoUploadResult = new FileUploadResult { RelativePath = "dish-videos/first.mp4", PublicUrl = "/uploads/dish-videos/first.mp4", OriginalFileName = "first.mp4", SizeBytes = 4000 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dish-videos", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(videoUploadResult);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(2u);
        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 101, Url = "dish-videos/first.mp4", MediaTypeLvId = 2 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var newVideo = new MediaFileInput { Stream = new MemoryStream(), FileName = "first.mp4", ContentType = "video/mp4" };
        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), newVideo, new List<long>(), default);

        // Assert — no old video removal, new video added
        _mediaRepoMock.Verify(r => r.RemoveDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediaRepoMock.Verify(r => r.RemoveMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.Is<MediaAsset>(m => m.Url == "dish-videos/first.mp4"), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenMinimalUpdateNoMediaNoTags_OnlyUpdatesFieldsAndI18n()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 55000m,
            IsOnline = false,
            ChefRecommended = false,
            Calories = null,
            PrepTimeMinutes = null,
            CookTimeMinutes = null,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo Light" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert — only field updates, no media/tag operations
        existingDish.Price.Should().Be(55000m);
        existingDish.IsOnline.Should().BeFalse();
        existingDish.ChefRecommended.Should().BeFalse();
        existingDish.Calories.Should().BeNull();
        existingDish.DishName.Should().Be("Pho Bo Light");
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediaRepoMock.Verify(r => r.GetDishMediaByIdsAsync(It.IsAny<long>(), It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenDuplicateTagIds_DeduplicatesAndUpdates()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 5, 5, 5 }, // duplicates
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishRepoMock.Setup(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert — tag 5 added only once (deduplicated by .Distinct())
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.Is<DishTag>(dt => dt.TagId == 5), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenI18nMissingEnKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["vi"] = new() { DishName = "Pho Bo Vi" } // missing "en" key
            }
        };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var act = () => service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert — accessing request.I18n["en"] throws KeyNotFoundException
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenAddingAndRemovingTagsSimultaneously_HandlesCorrectly()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 2, 3, 4 }, // existing: {1, 2} → remove 1, add 3 and 4
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint> { 1, 2 });
        _dishRepoMock.Setup(r => r.RemoveDishTagsAsync(1, It.IsAny<List<uint>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert — tag 1 removed, tags 3 and 4 added, tag 2 unchanged
        _dishRepoMock.Verify(r => r.RemoveDishTagsAsync(1, It.Is<List<uint>>(t => t.Count == 1 && t.Contains(1u)), It.IsAny<CancellationToken>()), Times.Once);
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.Is<DishTag>(dt => dt.TagId == 3), It.IsAny<CancellationToken>()), Times.Once);
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.Is<DishTag>(dt => dt.TagId == 4), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenFileUploadFails_RollsBackTransaction()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(1u);
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("File too large"));
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "big.jpg", ContentType = "image/jpeg" }
        };
        var service = CreateDishService();

        // Act
        var act = () => service.UpdateDishAsync(request, images, new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*File too large*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Additional Coverage — Missing Abnormal/Boundary Tests
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishesForCustomerAsync_Additional

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    #region GetDishByIdAsync_Actions_Additional

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishByIdAsync_Actions")]
    public async Task GetDishByIdAsync_Actions_WhenDishHasNoTags_ReturnsEmptyTagList()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishStatusLv.ValueNameText = MakeI18nText(500, "Available");
        foreach (var dm in dish.DishMedia)
        {
            dm.Media.MediaTypeLv = new LookupValue { ValueId = dm.Media.MediaTypeLvId, ValueCode = "IMAGE", ValueName = "Image", TypeId = 5, SortOrder = 1 };
        }

        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _dishRepoMock.Setup(r => r.FindTagByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<DishTag>());
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, default);

        // Assert
        result.Should().NotBeNull();
        result.DishId.Should().Be(1);
        result.Tags.Should().BeEmpty();
    }

    #endregion

    #region GetPosDishesAsync_Additional

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetPosDishesAsync")]
    public async Task GetPosDishesAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetActiveDishesAsync()).ThrowsAsync(new Exception("DB error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetPosDishesAsync(true);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    #region GetAllCategoriesAsync_Category_Additional

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAllCategoriesAsync_Category")]
    public async Task GetAllCategoriesAsync_Category_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        var query = new DishCategoryListQueryDTO { PageIndex = 1, PageSize = 10 };
        _dishCategoryRepoMock.Setup(r => r.GetAllCategoriesAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateCategoryService();

        // Act
        var act = () => service.GetAllCategoriesAsync(query);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  SUPPLEMENTARY TESTS — Missing Abnormal & Boundary Coverage
    // ══════════════════════════════════════════════════════════════════════

    // ── GetAllCategoriesAsync_Dish — Missing Abnormal ──

    #region GetAllCategoriesAsync_Dish_Supplementary

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAllCategoriesAsync_Dish")]
    public async Task GetAllCategoriesAsync_Dish_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetAllCategoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetAllCategoriesAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    // ── GetDishStatusesAsync — Missing Abnormal ──

    #region GetDishStatusesAsync_Supplementary

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishStatusesAsync")]
    public async Task GetDishStatusesAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishStatusesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishStatusesAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    // ── GetActiveDishStatusesAsync — Missing Abnormal ──

    #region GetActiveDishStatusesAsync_Supplementary

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetActiveDishStatusesAsync")]
    public async Task GetActiveDishStatusesAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetActiveDishStatusEntitiesAsync())
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetActiveDishStatusesAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    // ── GetAllDishCategoriesAsync — Missing Abnormal ──

    #region GetAllDishCategoriesAsync_Supplementary

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAllDishCategoriesAsync")]
    public async Task GetAllDishCategoriesAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetAllDishCategoriesAsync())
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetAllDishCategoriesAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    // ── GetAllActiveTagsAsync — Missing Abnormal ──

    #region GetAllActiveTagsAsync_Supplementary

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAllActiveTagsAsync")]
    public async Task GetAllActiveTagsAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetAllActiveTagsAsync())
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetAllActiveTagsAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    // ── GetDishByIdAsync_Detail — Additional Boundary/Normal ──

    #region GetDishByIdAsync_Detail_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenVietnameseLang_ReturnsVietnameseTranslation()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, "vi");

        // Assert
        result.Should().NotBeNull();
        result!.DishName.Should().Be("Pho Bo_vi");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenDishHasNoMedia_ReturnsEmptyLists()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishMedia = new List<DishMedium>();
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, "en");

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrls.Should().BeEmpty();
        result.VideoUrl.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenDishHasVideo_ReturnsVideoUrl()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishMedia.Add(new DishMedium
        {
            DishId = 1,
            MediaId = 2,
            IsPrimary = false,
            Media = new MediaAsset
            {
                MediaId = 2,
                Url = "dish-videos/intro.mp4",
                MimeType = "video/mp4",
                MediaTypeLvId = 2
            }
        });
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_video", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, "en");

        // Assert
        result.Should().NotBeNull();
        result!.VideoUrl.Should().Be("/uploads/dish-videos/intro.mp4");
        result.ImageUrls.Should().HaveCount(1); // only the image, not the video
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishByIdAsync_Detail")]
    public async Task GetDishByIdAsync_Detail_WhenDishHasNoRecipes_ReturnsEmptyComposition()
    {
        // Arrange
        var dish = MakeDish();
        dish.Recipes = new List<Recipe>();
        _dishRepoMock.Setup(r => r.GetDishByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, "en");

        // Assert
        result.Should().NotBeNull();
        result!.Composition.Should().BeEmpty();
    }

    #endregion

    // ── CreateDishAsync — Additional Coverage ──

    #region CreateDishAsync_Supplementary

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenWithVideo_SavesVideoMedia()
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
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 20)
            .Returns(Task.CompletedTask);

        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(2u);

        var videoUploadResult = new FileUploadResult { RelativePath = "dish-videos/vid.mp4", PublicUrl = "/uploads/dish-videos/vid.mp4", OriginalFileName = "vid.mp4", SizeBytes = 5000 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dish-videos", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(videoUploadResult);

        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 70, Url = "dish-videos/vid.mp4", MediaTypeLvId = 2 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var video = new MediaFileInput { Stream = new MemoryStream(), FileName = "vid.mp4", ContentType = "video/mp4" };
        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), video, default);

        // Assert
        result.Should().Be(20);
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.Is<MediaAsset>(m => m.Url == "dish-videos/vid.mp4"), It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepoMock.Verify(r => r.AddDishMediaAsync(It.Is<DishMedium>(dm => dm.IsPrimary == false), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenEmptyTagIds_SkipsTagCreation()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 30000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Spring Roll" }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 100 };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 30)
            .Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(30);
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    // ── CreateDishAsync — Extended Coverage ──

    #region CreateDishAsync_Extended

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenWithImagesAndVideo_CreatesAllMedia()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 75000m,
            IsOnline = true,
            ChefRecommended = true,
            Calories = 500,
            PrepTimeMinutes = 10,
            CookTimeMinutes = 25,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 3 },
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Bun Cha", Description = "Grilled pork noodle", Slogan = "Hanoi's finest", ShortDescription = "Noodle dish" }
            }
        };

        var textIds = new DishI18nTextIds
        {
            DishNameTextId = 200,
            DescriptionTextId = 201,
            SloganTextId = 202,
            ShortDescriptionTextId = 203
        };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 55)
            .Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1u);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(2u);

        var imgUpload = new FileUploadResult { RelativePath = "dishes/buncha.jpg", PublicUrl = "/uploads/dishes/buncha.jpg", OriginalFileName = "buncha.jpg", SizeBytes = 2048 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(imgUpload);

        var vidUpload = new FileUploadResult { RelativePath = "dish-videos/buncha.mp4", PublicUrl = "/uploads/dish-videos/buncha.mp4", OriginalFileName = "buncha.mp4", SizeBytes = 8000 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dish-videos", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vidUpload);

        _mediaRepoMock.SetupSequence(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 80, Url = "dishes/buncha.jpg", MediaTypeLvId = 1 })
            .ReturnsAsync(new MediaAsset { MediaId = 81, Url = "dish-videos/buncha.mp4", MediaTypeLvId = 2 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "buncha.jpg", ContentType = "image/jpeg" }
        };
        var video = new MediaFileInput { Stream = new MemoryStream(), FileName = "buncha.mp4", ContentType = "video/mp4" };

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, images, new List<MediaFileInput>(), video, default);

        // Assert
        result.Should().Be(55);
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mediaRepoMock.Verify(r => r.AddDishMediaAsync(It.Is<DishMedium>(dm => dm.IsPrimary == true), It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepoMock.Verify(r => r.AddDishMediaAsync(It.Is<DishMedium>(dm => dm.IsPrimary == false), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenMultipleImages_SavesAllWithPrimaryFlag()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 2,
            Price = 60000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Banh Mi" }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 300 };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 60)
            .Returns(Task.CompletedTask);

        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1u);

        var uploadResult = new FileUploadResult { RelativePath = "dishes/banhmi.jpg", PublicUrl = "/uploads/dishes/banhmi.jpg", OriginalFileName = "banhmi.jpg", SizeBytes = 1024 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 90, Url = "dishes/banhmi.jpg", MediaTypeLvId = 1 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "banhmi1.jpg", ContentType = "image/jpeg" },
            new() { Stream = new MemoryStream(), FileName = "banhmi2.jpg", ContentType = "image/jpeg" },
            new() { Stream = new MemoryStream(), FileName = "banhmi3.jpg", ContentType = "image/png" }
        };

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, images, new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(60);
        _fileStorageMock.Verify(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mediaRepoMock.Verify(r => r.AddDishMediaAsync(It.Is<DishMedium>(dm => dm.IsPrimary == true), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenPriceIsZero_CreatesDishSuccessfully()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 0m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Complimentary Water" }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 400 };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) =>
            {
                d.DishId = 70;
                d.Price.Should().Be(0m);
            })
            .Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(70);
        _dishRepoMock.Verify(r => r.AddAsync(It.Is<Dish>(d => d.Price == 0m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenAllOptionalFieldsNull_CreatesDish()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 40000m,
            IsOnline = false,
            ChefRecommended = false,
            Calories = null,
            PrepTimeMinutes = null,
            CookTimeMinutes = null,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Simple Dish", Description = null, Slogan = null, Note = null, ShortDescription = null }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 500, DescriptionTextId = null, SloganTextId = null, NoteTextId = null, ShortDescriptionTextId = null };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => d.DishId = 80)
            .Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(80);
        _dishRepoMock.Verify(r => r.AddAsync(
            It.Is<Dish>(d =>
                d.Calories == null &&
                d.PrepTimeMinutes == null &&
                d.CookTimeMinutes == null &&
                d.DescriptionTextId == null &&
                d.SloganTextId == null &&
                d.NoteTextId == null &&
                d.ShortDescriptionTextId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenRepositoryAddFails_RollsBackTransaction()
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
            .ThrowsAsync(new InvalidOperationException("Database constraint violation"));

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Database constraint violation*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenImageUploadFails_RollsBackAndCleansUp()
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

        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "img.jpg", ContentType = "image/jpeg" }
        };

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, images, new List<MediaFileInput>(), null, default);

        // Assert
        await act.Should().ThrowAsync<IOException>().WithMessage("*Disk full*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenCommitFails_RollsBackAndCleansUpSavedFiles()
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

        var uploadResult = new FileUploadResult { RelativePath = "dishes/pho.jpg", PublicUrl = "/uploads/dishes/pho.jpg", OriginalFileName = "pho.jpg", SizeBytes = 1024 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 50, Url = "dishes/pho.jpg", MediaTypeLvId = 1 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Commit failed"));
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "pho.jpg", ContentType = "image/jpeg" }
        };

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, images, new List<MediaFileInput>(), null, default);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*Commit failed*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenValidRequest_SetsDishPropertiesCorrectly()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 5,
            Price = 120000m,
            IsOnline = true,
            ChefRecommended = true,
            Calories = 800,
            PrepTimeMinutes = 20,
            CookTimeMinutes = 45,
            DishStatusLvId = 15u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Special Combo" }
            }
        };

        var textIds = new DishI18nTextIds
        {
            DishNameTextId = 600,
            DescriptionTextId = 601,
            SloganTextId = 602,
            NoteTextId = 603,
            ShortDescriptionTextId = 604
        };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        Dish? capturedDish = null;
        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) =>
            {
                capturedDish = d;
                d.DishId = 99;
            })
            .Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(99);
        capturedDish.Should().NotBeNull();
        capturedDish!.CategoryId.Should().Be(5);
        capturedDish.DishName.Should().Be("Special Combo");
        capturedDish.Price.Should().Be(120000m);
        capturedDish.IsOnline.Should().BeTrue();
        capturedDish.ChefRecommended.Should().BeTrue();
        capturedDish.Calories.Should().Be(800);
        capturedDish.PrepTimeMinutes.Should().Be(20);
        capturedDish.CookTimeMinutes.Should().Be(45);
        capturedDish.DishStatusLvId.Should().Be(15u);
        capturedDish.DishNameTextId.Should().Be(600);
        capturedDish.DescriptionTextId.Should().Be(601);
        capturedDish.SloganTextId.Should().Be(602);
        capturedDish.NoteTextId.Should().Be(603);
        capturedDish.ShortDescriptionTextId.Should().Be(604);
        capturedDish.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenVideoUploadFails_RollsBackAndCleansUpSavedImages()
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

        var imgUpload = new FileUploadResult { RelativePath = "dishes/img.jpg", PublicUrl = "/uploads/dishes/img.jpg", OriginalFileName = "img.jpg", SizeBytes = 1024 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(imgUpload);

        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 50, Url = "dishes/img.jpg", MediaTypeLvId = 1 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Video upload fails
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dish-videos", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Video storage quota exceeded"));

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "img.jpg", ContentType = "image/jpeg" }
        };
        var video = new MediaFileInput { Stream = new MemoryStream(), FileName = "vid.mp4", ContentType = "video/mp4" };

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, images, new List<MediaFileInput>(), video, default);

        // Assert
        await act.Should().ThrowAsync<IOException>().WithMessage("*Video storage quota exceeded*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenAddMediaAsyncFails_RollsBackTransaction()
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

        // AddMediaAsync fails after file was saved
        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Media table constraint violation"));

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "img.jpg", ContentType = "image/jpeg" }
        };

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, images, new List<MediaFileInput>(), null, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Media table constraint violation*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenAddDishTagFails_RollsBackTransaction()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 1, 2 },
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

        // First tag succeeds, second tag fails
        _dishRepoMock.SetupSequence(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new InvalidOperationException("Foreign key violation — invalid TagId"));

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Foreign key violation*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenLookupResolverFails_RollsBackTransaction()
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

        // LookupResolver for MediaType IMAGE fails (enum overload)
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Lookup value 'IMAGE' not found for type MediaType"));

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "img.jpg", ContentType = "image/jpeg" }
        };

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, images, new List<MediaFileInput>(), null, default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*IMAGE*not found*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    // ── GetDishesForAdminAsync — Additional Boundary ──

    #region GetDishesForAdminAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForAdminAsync")]
    public async Task GetDishesForAdminAsync_WhenDishHasNullCategoryAndStatus_ReturnsFallbackValues()
    {
        // Arrange
        var dish = MakeDish();
        dish.Category = null!;
        dish.DishStatusLv = null!;
        dish.DishNameText = null!;
        dish.DescriptionText = null;

        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForAdminAsync(MakeGetDishesRequest());

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].CategoryName.Should().Be("Uncategorized");
        items[0].Status.Should().Be("Unknown");
    }

    #endregion

    // ── GetDishesForCustomerAsync — Additional Boundary ──

    #region GetDishesForCustomerAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenDishHasNoPrimaryImage_FallsBackToFirstImage()
    {
        // Arrange
        var dish = MakeDish();
        foreach (var dm in dish.DishMedia) dm.IsPrimary = false;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].ImageUrl.Should().NotBeNull();
        items[0].ImageUrl.Should().Contain("/uploads/");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenDishHasNoMedia_ReturnsNullImageUrl()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishMedia = new List<DishMedium>();
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].ImageUrl.Should().BeNull();
    }

    #endregion

    // ── GetPosDishesAsync — Additional Boundary ──

    #region GetPosDishesAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPosDishesAsync")]
    public async Task GetPosDishesAsync_WhenDishHasNullOptionalI18nTexts_ReturnsNullTranslations()
    {
        // Arrange
        var dish = MakeDish();
        dish.SloganText = null;
        dish.NoteText = null;
        dish.ShortDescriptionText = null;

        _dishRepoMock.Setup(r => r.GetActiveDishesAsync()).ReturnsAsync(new List<Dish> { dish });
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetPosDishesAsync(true);

        // Assert
        result.Should().HaveCount(1);
        result[0].I18n.Should().ContainKey("en");
        result[0].I18n["en"].Slogan.Should().BeNull();
        result[0].I18n["en"].Note.Should().BeNull();
        result[0].I18n["en"].ShortDescription.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPosDishesAsync")]
    public async Task GetPosDishesAsync_WhenDishHasNoPrimaryImage_FallsBackToFirstMedia()
    {
        // Arrange
        var dish = MakeDish();
        foreach (var dm in dish.DishMedia) dm.IsPrimary = false;
        dish.SloganText = MakeI18nText(700, "Slogan");
        dish.NoteText = MakeI18nText(701, "Note");
        dish.ShortDescriptionText = MakeI18nText(702, "Short");

        _dishRepoMock.Setup(r => r.GetActiveDishesAsync()).ReturnsAsync(new List<Dish> { dish });
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetPosDishesAsync(true);

        // Assert
        result.Should().HaveCount(1);
        result[0].ImageUrl.Should().NotBeNull();
        result[0].ImageUrl.Should().Contain("/uploads/");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPosDishesAsync")]
    public async Task GetPosDishesAsync_WhenDishHasNoMedia_ReturnsNullImageUrl()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishMedia = new List<DishMedium>();
        dish.SloganText = MakeI18nText(700, "Slogan");
        dish.NoteText = MakeI18nText(701, "Note");
        dish.ShortDescriptionText = MakeI18nText(702, "Short");

        _dishRepoMock.Setup(r => r.GetActiveDishesAsync()).ReturnsAsync(new List<Dish> { dish });

        var service = CreateDishService();

        // Act
        var result = await service.GetPosDishesAsync(true);

        // Assert
        result.Should().HaveCount(1);
        result[0].ImageUrl.Should().BeNull();
    }

    #endregion

    // ── UpdateDishStatusAsync — Additional Boundary ──

    #region UpdateDishStatusAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishStatusAsync")]
    public async Task UpdateDishStatusAsync_WhenStatusIsSameAsAvailable_ReturnsAvailableStatus()
    {
        // Arrange
        var dish = MakeDish(); // already AVAILABLE
        _dishRepoMock.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(10u);
        _dishRepoMock.Setup(r => r.UpdateStatusAsync(1, 10u, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.UpdateDishStatusAsync(1, DishStatusCode.AVAILABLE);

        // Assert
        result.StatusCode.Should().Be(DishStatusCode.AVAILABLE);
        result.StatusId.Should().Be(10u);
    }

    #endregion

    // ── UpdateCategoryAsync — Additional Boundary ──

    #region UpdateCategoryAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateCategoryAsync")]
    public async Task UpdateCategoryAsync_WhenHasExistingDescriptionTextId_UpdatesDescriptionI18n()
    {
        // Arrange
        var existingCategory = MakeDishCategory(1, "Soup");
        var request = MakeUpdateCategoryRequest("Soup", "Updated soup description");
        var updatedCategory = MakeDishCategory(1, "Soup");

        _dishCategoryRepoMock.SetupSequence(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory)
            .ReturnsAsync(updatedCategory);
        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Soup", 1L, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.UpdateStringsAsync(200, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _i18nServiceMock.Setup(s => s.UpdateStringsAsync(201, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.UpdateCategoryAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        _i18nServiceMock.Verify(s => s.UpdateStringsAsync(201, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateCategoryAsync")]
    public async Task UpdateCategoryAsync_WhenNoExistingDescTextId_CreatesNewDescI18n()
    {
        // Arrange
        var existingCategory = MakeDishCategory(1, "Soup");
        existingCategory.DescriptionTextId = null;
        existingCategory.DescriptionText = null;
        var request = MakeUpdateCategoryRequest("Soup", "New description");
        var updatedCategory = MakeDishCategory(1, "Soup");

        _dishCategoryRepoMock.SetupSequence(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory)
            .ReturnsAsync(updatedCategory);
        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("Soup", 1L, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.UpdateStringsAsync(200, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Description", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(600L);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCategory);

        var service = CreateCategoryService();

        // Act
        var result = await service.UpdateCategoryAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        existingCategory.DescriptionTextId.Should().Be(600L);
        _i18nServiceMock.Verify(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Description", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ── ToggleCategoryStatusAsync — Additional Boundary ──

    #region ToggleCategoryStatusAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ToggleCategoryStatusAsync")]
    public async Task ToggleCategoryStatusAsync_WhenAlreadyEnabled_StaysEnabled()
    {
        // Arrange
        var category = MakeDishCategory(1, "Soup", isDisabled: false);
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _dishCategoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var service = CreateCategoryService();

        // Act
        var result = await service.ToggleCategoryStatusAsync(1, false);

        // Assert
        category.IsDisabled.Should().BeFalse();
        _dishCategoryRepoMock.Verify(r => r.UpdateAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ── GetCategoryByIdAsync — Additional Abnormal ──

    #region GetCategoryByIdAsync_Supplementary

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetCategoryByIdAsync")]
    public async Task GetCategoryByIdAsync_WhenRepoThrows_PropagatesException()
    {
        // Arrange
        _dishCategoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateCategoryService();

        // Act
        var act = () => service.GetCategoryByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB error*");
    }

    #endregion

    // ── CreateCategoryAsync — Additional Abnormal ──

    #region CreateCategoryAsync_Supplementary

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCategoryAsync")]
    public async Task CreateCategoryAsync_WhenRepoCreateThrows_PropagatesException()
    {
        // Arrange
        var request = MakeCreateCategoryRequest("BBQ", "Grilled items");

        _dishCategoryRepoMock.Setup(r => r.ExistsByNameAsync("BBQ", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Name", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(700L);
        _i18nServiceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), "Dish Category Description", "en", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(701L);
        _dishCategoryRepoMock.Setup(r => r.GetMaxDisplayOrderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _dishCategoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<DishCategory>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB write error"));

        var service = CreateCategoryService();

        // Act
        var act = () => service.CreateCategoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB write error*");
    }

    #endregion

    // ── UpdateDishAsync — Additional Boundary ──

    #region UpdateDishAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenNoMediaChanges_SkipsMediaOperations()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 55000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 1 },
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo Updated" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint> { 1 });
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert
        _mediaRepoMock.Verify(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediaRepoMock.Verify(r => r.RemoveDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>()), Times.Never);
        existingDish.Price.Should().Be(55000m);
        existingDish.DishName.Should().Be("Pho Bo Updated");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateDishAsync")]
    public async Task UpdateDishAsync_WhenDuplicateTagIds_DeduplicatesTags()
    {
        // Arrange
        var existingDish = MakeDish(1, "Pho Bo");
        var request = new UpdateDishRequest
        {
            DishId = 1,
            CategoryId = 1,
            Price = 50000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint> { 3, 3, 4, 4 },
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Pho Bo" }
            }
        };

        var newTextIds = new DishI18nTextIds { DishNameTextId = 100 };

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingDish);
        _dishRepoMock.Setup(r => r.GetTagIdsByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<uint>());
        _dishRepoMock.Setup(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dishI18nServiceMock.Setup(s => s.CreateOrUpdateDishI18nTextsAsync(It.IsAny<DishI18nTextIds>(), It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTextIds);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        await service.UpdateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, new List<long>(), default);

        // Assert — only 2 distinct tags should be added
        _dishRepoMock.Verify(r => r.AddDishTagAsync(It.IsAny<DishTag>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    // ── GetDishByIdAsync_Actions — Additional Boundary ──

    #region GetDishByIdAsync_Actions_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishByIdAsync_Actions")]
    public async Task GetDishByIdAsync_Actions_WhenDishHasNoMedia_ReturnsEmptyMediaList()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishMedia = new List<DishMedium>();
        dish.DishStatusLv.ValueNameText = MakeI18nText(500, "Available");

        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _dishRepoMock.Setup(r => r.FindTagByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<DishTag>());
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, default);

        // Assert
        result.Should().NotBeNull();
        result.DishId.Should().Be(1);
    }

    #endregion

    // ── GetAllDishCategoriesAsync — Additional Boundary ──

    #region GetAllDishCategoriesAsync_Supplementary

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllDishCategoriesAsync")]
    public async Task GetAllDishCategoriesAsync_WhenCategoryHasNoTranslations_ReturnsFallbackName()
    {
        // Arrange
        var category = new DishCategory
        {
            CategoryId = 1,
            CategoryName = "Soup",
            CategoryNameText = null,
            CategoryNameTextId = null
        };
        _dishRepoMock.Setup(r => r.GetAllDishCategoriesAsync()).ReturnsAsync(new List<DishCategory> { category });

        var service = CreateDishService();

        // Act
        var result = await service.GetAllDishCategoriesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].NameVi.Should().Be("Soup");
        result[0].NameEn.Should().Be("Soup");
        result[0].NameFr.Should().Be("Soup");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  EXTENDED COVERAGE — GetDishesForCustomerAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishesForCustomerAsync_Extended

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenCalled_SetsIsCustomerViewTrue()
    {
        // Arrange
        GetDishesRequest? capturedRequest = null;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GetDishesRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync((new List<Dish>(), 0));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateDishService();

        // Act
        await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.IsCustomerView.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenDishesExist_MapsI18nFieldsCorrectly()
    {
        // Arrange
        var dish = MakeDish(1, "Pho Bo");
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        var dto = items[0];
        dto.DishName.En.Should().Be("Pho Bo");
        dto.DishName.Vi.Should().Be("Pho Bo_vi");
        dto.DishName.Fr.Should().Be("Pho Bo_fr");
        dto.CategoryName.En.Should().Be("Soup");
        dto.CategoryName.Vi.Should().Be("Soup_vi");
        dto.CategoryName.Fr.Should().Be("Soup_fr");
        dto.Description.Should().NotBeNull();
        dto.Description!.En.Should().Be("A delicious Vietnamese soup");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenChefRecommendedIsNull_ReturnsFalse()
    {
        // Arrange
        var dish = MakeDish();
        dish.ChefRecommended = null;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].IsChefRecommended.Should().BeFalse();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenCategoryIsNull_ReturnsEmptyI18nTextDto()
    {
        // Arrange
        var dish = MakeDish();
        dish.Category = null!;
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].CategoryName.Should().NotBeNull();
        items[0].CategoryName.Vi.Should().BeEmpty();
        items[0].CategoryName.En.Should().BeEmpty();
        items[0].CategoryName.Fr.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenImagesEnabled_ReturnsPrimaryImageUrl()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Dish> { dish }, 1));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync("landing_page.show_dish_image", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, _) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        items[0].ImageUrl.Should().Be("/uploads/dishes/pho.jpg");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenRepoThrowsDbError_PropagatesException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB connection error"));

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*DB connection error*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishesForCustomerAsync")]
    public async Task GetDishesForCustomerAsync_WhenMultipleDishes_ReturnsAllMapped()
    {
        // Arrange
        var dishes = new List<Dish>
        {
            MakeDish(1, "Pho Bo", 50000m),
            MakeDish(2, "Bun Cha", 60000m),
            MakeDish(3, "Banh Mi", 25000m)
        };
        _dishRepoMock.Setup(r => r.GetDishesAsync(It.IsAny<GetDishesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dishes, 3));
        _systemSettingServiceMock.Setup(s => s.GetBoolAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var (items, totalCount) = await service.GetDishesForCustomerAsync(MakeGetDishesRequest());

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(3);
        items[0].DishId.Should().Be(1);
        items[0].Price.Should().Be(50000m);
        items[1].DishId.Should().Be(2);
        items[1].Price.Should().Be(60000m);
        items[2].DishId.Should().Be(3);
        items[2].Price.Should().Be(25000m);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  EXTENDED COVERAGE — GetDishByIdAsync_Actions
    // ══════════════════════════════════════════════════════════════════════

    #region GetDishByIdAsync_Actions_Extended

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishByIdAsync_Actions")]
    public async Task GetDishByIdAsync_Actions_WhenDishHasTags_MapsTagsCorrectly()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishStatusLv.ValueNameText = MakeI18nText(500, "Available");
        foreach (var dm in dish.DishMedia)
            dm.Media.MediaTypeLv = new LookupValue { ValueId = dm.Media.MediaTypeLvId, ValueCode = "IMAGE", ValueName = "Image", TypeId = 5, SortOrder = 1 };

        var dishTags = new List<DishTag>
        {
            new() { DishTagId = 1, DishId = 1, TagId = 1, Tag = new LookupValue { ValueId = 1, ValueCode = "SPICY", ValueName = "Spicy", TypeId = 14, SortOrder = 1, ValueNameText = MakeI18nText(600, "Spicy") } },
            new() { DishTagId = 2, DishId = 1, TagId = 2, Tag = new LookupValue { ValueId = 2, ValueCode = "VEGAN", ValueName = "Vegan", TypeId = 14, SortOrder = 2, ValueNameText = MakeI18nText(601, "Vegan") } }
        };

        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _dishRepoMock.Setup(r => r.FindTagByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dishTags);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, default);

        // Assert
        result.Should().NotBeNull();
        result.TagIds.Should().HaveCount(2);
        result.TagIds.Should().Contain(1u);
        result.TagIds.Should().Contain(2u);
        result.Tags.Should().HaveCount(2);
        result.Tags[0].TagId.Should().Be(1u);
        result.Tags[1].TagId.Should().Be(2u);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDishByIdAsync_Actions")]
    public async Task GetDishByIdAsync_Actions_WhenDishExists_MapsAllDtoFields()
    {
        // Arrange
        var dish = MakeDish();
        dish.DishStatusLv.ValueNameText = MakeI18nText(500, "Available");
        foreach (var dm in dish.DishMedia)
            dm.Media.MediaTypeLv = new LookupValue { ValueId = dm.Media.MediaTypeLvId, ValueCode = "IMAGE", ValueName = "Image", TypeId = 5, SortOrder = 1 };

        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _dishRepoMock.Setup(r => r.FindTagByDishIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<DishTag>());
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>())).Returns<string>(p => $"/uploads/{p}");

        var service = CreateDishService();

        // Act
        var result = await service.GetDishByIdAsync(1, default);

        // Assert
        result.Should().NotBeNull();
        result.DishId.Should().Be(1);
        result.CategoryId.Should().Be(1);
        result.Price.Should().Be(50000m);
        result.DishStatusLvId.Should().Be(10u);
        result.IsOnline.Should().BeTrue();
        result.ChefRecommended.Should().BeTrue();
        result.Calories.Should().Be(350);
        result.PrepTimeMinutes.Should().Be(15);
        result.CookTimeMinutes.Should().Be(30);
        result.Media.Should().HaveCount(1);
        result.Media[0].Url.Should().Contain("/uploads/");
        result.Media[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDishByIdAsync_Actions")]
    public async Task GetDishByIdAsync_Actions_WhenDishIdIsZero_ThrowsKeyNotFoundException()
    {
        // Arrange
        _dishRepoMock.Setup(r => r.FindByIdForActionAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync((Dish?)null);
        _dishRepoMock.Setup(r => r.FindTagByDishIdAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(new List<DishTag>());

        var service = CreateDishService();

        // Act
        var act = () => service.GetDishByIdAsync(0, default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*0*not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  EXTENDED COVERAGE — GetAllDishCategoriesAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetAllDishCategoriesAsync_Extended

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllDishCategoriesAsync")]
    public async Task GetAllDishCategoriesAsync_WhenCategoriesExist_MapsI18nCorrectly()
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
        result[0].NameEn.Should().Be("Soup");
        result[0].NameVi.Should().Be("Soup_vi");
        result[0].NameFr.Should().Be("Soup_fr");
        result[1].CategoryId.Should().Be(2);
        result[1].NameEn.Should().Be("Appetizer");
        result[1].NameVi.Should().Be("Appetizer_vi");
        result[1].NameFr.Should().Be("Appetizer_fr");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllDishCategoriesAsync")]
    public async Task GetAllDishCategoriesAsync_WhenCategoryHasEmptyTranslations_ReturnsFallbackName()
    {
        // Arrange
        var category = new DishCategory
        {
            CategoryId = 3,
            CategoryName = "Dessert",
            CategoryNameText = new I18nText
            {
                TextId = 300,
                TextKey = "test.key.300",
                SourceLangCode = "en",
                SourceText = "Dessert",
                I18nTranslations = new List<I18nTranslation>() // empty translations
            },
            CategoryNameTextId = 300
        };
        _dishRepoMock.Setup(r => r.GetAllDishCategoriesAsync()).ReturnsAsync(new List<DishCategory> { category });

        var service = CreateDishService();

        // Act
        var result = await service.GetAllDishCategoriesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].NameVi.Should().Be("Dessert"); // fallback
        result[0].NameEn.Should().Be("Dessert"); // fallback
        result[0].NameFr.Should().Be("Dessert"); // fallback
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  EXTENDED COVERAGE — UpdateDishStatusAsync
    // ══════════════════════════════════════════════════════════════════════

    #region UpdateDishStatusAsync_Extended

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateDishStatusAsync")]
    public async Task UpdateDishStatusAsync_WhenLookupResolverThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var dish = MakeDish();
        _dishRepoMock.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Status code not found in lookup"));

        var service = CreateDishService();

        // Act
        var act = () => service.UpdateDishStatusAsync(1, DishStatusCode.HIDDEN);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Status code not found*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateDishStatusAsync")]
    public async Task UpdateDishStatusAsync_WhenOutOfStock_ReturnsOutOfStockDto()
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
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  EXTENDED COVERAGE — CreateDishAsync
    // ══════════════════════════════════════════════════════════════════════

    #region CreateDishAsync_FullCoverage

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenMultipleLanguages_UsesEnglishNameForDishName()
    {
        // Arrange
        var request = new CreateDishRequest
        {
            CategoryId = 1,
            Price = 45000m,
            DishStatusLvId = 10u,
            TagIds = new List<uint>(),
            I18n = new Dictionary<string, DishI18nDto>
            {
                ["en"] = new() { DishName = "Spring Roll" },
                ["vi"] = new() { DishName = "Nem Ran" },
                ["fr"] = new() { DishName = "Rouleau de printemps" }
            }
        };

        var textIds = new DishI18nTextIds { DishNameTextId = 800 };
        _dishI18nServiceMock.Setup(s => s.CreateDishI18nTextsAsync(It.IsAny<Dictionary<string, DishI18nDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(textIds);

        Dish? capturedDish = null;
        _dishRepoMock.Setup(r => r.AddAsync(It.IsAny<Dish>(), It.IsAny<CancellationToken>()))
            .Callback<Dish, CancellationToken>((d, _) => { capturedDish = d; d.DishId = 100; })
            .Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateDishService();

        // Act
        var result = await service.CreateDishAsync(request, new List<MediaFileInput>(), new List<MediaFileInput>(), null, default);

        // Assert
        result.Should().Be(100);
        capturedDish.Should().NotBeNull();
        capturedDish!.DishName.Should().Be("Spring Roll"); // Uses en locale for DishName
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateDishAsync")]
    public async Task CreateDishAsync_WhenVideoUploadFails_RollsBackAndCleansUpPreviousUploads()
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
        _lookupResolverMock.Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>())).ReturnsAsync(2u);

        // Image upload succeeds
        var imgUpload = new FileUploadResult { RelativePath = "dishes/pho.jpg", PublicUrl = "/uploads/dishes/pho.jpg", OriginalFileName = "pho.jpg", SizeBytes = 1024 };
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dishes", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(imgUpload);

        _mediaRepoMock.Setup(r => r.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaAsset { MediaId = 50, Url = "dishes/pho.jpg", MediaTypeLvId = 1 });
        _mediaRepoMock.Setup(r => r.AddDishMediaAsync(It.IsAny<DishMedium>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Video upload fails
        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "dish-videos", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Video upload failed"));

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _fileStorageMock.Setup(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(), FileName = "pho.jpg", ContentType = "image/jpeg" }
        };
        var video = new MediaFileInput { Stream = new MemoryStream(), FileName = "vid.mp4", ContentType = "video/mp4" };

        var service = CreateDishService();

        // Act
        var act = () => service.CreateDishAsync(request, images, new List<MediaFileInput>(), video, default);

        // Assert
        await act.Should().ThrowAsync<IOException>().WithMessage("*Video upload failed*");
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteManyAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    #endregion
}
