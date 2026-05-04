using Core.DTO.General;
using Core.DTO.SystemSetting;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.Others;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test - SystemSettingService
/// Code Module : Core/Service/SystemSettingService.cs
/// Method      : Get/Set/Delete/GetGroup/BulkUpdate/CreateSetting/Upload methods
/// Created By  : Automation
/// Executed By : Test Runner
/// Test Req.   : Test cache behavior, value type conversion, grouping logic, store media normalization and upload routing.
/// </summary>
public class SystemSettingServiceTests
{
    private readonly Mock<ISystemSettingRepository> _repositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();

    private SystemSettingService CreateService() => new(
        _repositoryMock.Object,
        _cacheServiceMock.Object,
        _fileStorageMock.Object);

    private static SystemSetting MakeSetting(
        string key,
        string valueType,
        string? valueString = null,
        long? valueInt = null,
        decimal? valueDecimal = null,
        bool? valueBool = null,
        string? valueJson = null,
        bool isSensitive = false) => new()
    {
        SettingKey = key,
        ValueType = valueType,
        ValueString = valueString,
        ValueInt = valueInt,
        ValueDecimal = valueDecimal,
        ValueBool = valueBool,
        ValueJson = valueJson,
        IsSensitive = isSensitive,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetStringAsync")]
    public async Task GetStringAsync_WhenCacheHit_ReturnsStringWithoutQueryingRepository()
    {
        var cached = MakeSetting("store.email", "STRING", valueString: "a@b.com");

        _cacheServiceMock
            .Setup(c => c.GetAsync<SystemSetting>("system_setting:store.email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var service = CreateService();

        var result = await service.GetStringAsync("store.email", cancellationToken: CancellationToken.None);

        result.Should().Be("a@b.com");
        _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetIntAsync")]
    public async Task GetIntAsync_WhenTypeMismatch_ReturnsDefault()
    {
        var setting = MakeSetting("reservation.duration", "STRING", valueString: "abc");

        _cacheServiceMock
            .Setup(c => c.GetAsync<SystemSetting>("system_setting:reservation.duration", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemSetting?)null);

        _repositoryMock
            .Setup(r => r.GetByKeyAsync("reservation.duration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);

        var service = CreateService();

        var result = await service.GetIntAsync("reservation.duration", 120, CancellationToken.None);

        result.Should().Be(120);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetJsonAsync")]
    public async Task GetJsonAsync_WhenInvalidJson_ReturnsDefault()
    {
        var setting = MakeSetting("notification.rules", "JSON", valueJson: "{bad-json");

        _cacheServiceMock
            .Setup(c => c.GetAsync<SystemSetting>("system_setting:notification.rules", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemSetting?)null);

        _repositoryMock
            .Setup(r => r.GetByKeyAsync("notification.rules", It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);

        var service = CreateService();
        var fallback = new Dictionary<string, string> { ["k"] = "v" };

        var result = await service.GetJsonAsync("notification.rules", fallback, CancellationToken.None);

        result.Should().BeEquivalentTo(fallback);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "SetStringAsync")]
    public async Task SetStringAsync_WhenCalled_SavesAndClearsCache()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheServiceMock
            .Setup(c => c.RemoveAsync("system_setting:store.phone", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await service.SetStringAsync("store.phone", "0123", "desc", false, 10, CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<SystemSetting>(s =>
                s.SettingKey == "store.phone" &&
                s.ValueType == "STRING" &&
                s.ValueString == "0123" &&
                s.Description == "desc" &&
                s.UpdatedBy == 10),
            It.IsAny<CancellationToken>()), Times.Once);

        _cacheServiceMock.Verify(c => c.RemoveAsync("system_setting:store.phone", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteAsync")]
    public async Task DeleteAsync_WhenDeleted_ClearsCacheAndReturnsTrue()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync("store.logoUrl", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cacheServiceMock
            .Setup(c => c.RemoveAsync("system_setting:store.logoUrl", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var result = await service.DeleteAsync("store.logoUrl", CancellationToken.None);

        result.Should().BeTrue();
        _cacheServiceMock.Verify(c => c.RemoveAsync("system_setting:store.logoUrl", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "DeleteAsync")]
    public async Task DeleteAsync_WhenNotDeleted_DoesNotClearCache()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync("missing.key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        var result = await service.DeleteAsync("missing.key", CancellationToken.None);

        result.Should().BeFalse();
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllNonSensitiveAsync")]
    public async Task GetAllNonSensitiveAsync_WhenCalled_ReturnsTypedDictionary()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("s1", "STRING", valueString: "abc"),
            MakeSetting("s2", "INT", valueInt: 9),
            MakeSetting("s3", "DECIMAL", valueDecimal: 2.5m),
            MakeSetting("s4", "BOOL", valueBool: true),
            MakeSetting("s5", "JSON", valueJson: "{\"x\":1}")
        };

        _repositoryMock
            .Setup(r => r.GetAllNonSensitiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        var result = await service.GetAllNonSensitiveAsync(CancellationToken.None);

        result["s1"].Should().Be("abc");
        result["s2"].Should().Be(9);
        result["s3"].Should().Be(2.5m);
        result["s4"].Should().Be(true);
        result["s5"].Should().Be("{\"x\":1}");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllGroupedAsync")]
    public async Task GetAllGroupedAsync_WhenMixedKeys_GroupsByPrefixAndGeneral()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("store.email", "STRING", valueString: "a@b.com"),
            MakeSetting("reservation.default_duration_minutes", "INT", valueInt: 120),
            MakeSetting("timezone", "STRING", valueString: "UTC")
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        var result = await service.GetAllGroupedAsync(CancellationToken.None);

        result.Should().ContainKey("store");
        result.Should().ContainKey("reservation");
        result.Should().ContainKey("general");
        result["general"].Single().SettingKey.Should().Be("timezone");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGroupAsync")]
    public async Task GetGroupAsync_WhenStoreGroup_AttachesPublicUrlForMediaKeys()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("store.logoUrl", "STRING", valueString: "uploads/store-logo/logo.png"),
            MakeSetting("store.name", "STRING", valueString: "AuLac")
        };

        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("store", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _fileStorageMock
            .Setup(f => f.GetPublicUrl("store-logo/logo.png"))
            .Returns("/uploads/store-logo/logo.png");

        var service = CreateService();

        var result = await service.GetGroupAsync("store", CancellationToken.None);

        result.First(x => x.SettingKey == "store.logoUrl").PublicUrl.Should().Be("/uploads/store-logo/logo.png");
        result.First(x => x.SettingKey == "store.name").PublicUrl.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPublicGroupAsync")]
    public async Task GetPublicGroupAsync_WhenContainsSensitive_FiltersSensitiveOut()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("store.email", "STRING", valueString: "a@b.com", isSensitive: false),
            MakeSetting("store.smtp.password", "STRING", valueString: "secret", isSensitive: true)
        };

        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("store", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        var result = await service.GetPublicGroupAsync("store", CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().SettingKey.Should().Be("store.email");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "BulkUpdateGroupAsync")]
    public async Task BulkUpdateGroupAsync_WhenStoreMediaAbsoluteUrl_NormalizesToUploadsPath()
    {
        var existing = new List<SystemSetting>
        {
            MakeSetting("store.logoUrl", "STRING", valueString: "uploads/old.png", isSensitive: false)
        };

        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("store", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheServiceMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var items = new List<BulkUpdateSettingItemDto>
        {
            new()
            {
                Key = "store.logoUrl",
                Value = "https://cdn.example.com/uploads/store-logo/new-logo.png"
            }
        };

        await service.BulkUpdateGroupAsync("store", items, updatedBy: 20, cancellationToken: CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<SystemSetting>(s =>
                s.SettingKey == "store.logoUrl" &&
                s.ValueType == "STRING" &&
                s.ValueString == "uploads/store-logo/new-logo.png" &&
                s.UpdatedBy == 20),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "BulkUpdateGroupAsync")]
    public async Task BulkUpdateGroupAsync_WhenNotificationRecipients_ForcesJsonValueType()
    {
        var existing = new List<SystemSetting>
        {
            MakeSetting("notification.order.recipients", "STRING", valueString: "old", isSensitive: false)
        };

        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("notification", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheServiceMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var items = new List<BulkUpdateSettingItemDto>
        {
            new()
            {
                Key = "notification.order.recipients",
                Value = "[\"admin@example.com\"]"
            }
        };

        await service.BulkUpdateGroupAsync("notification", items, cancellationToken: CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<SystemSetting>(s =>
                s.SettingKey == "notification.order.recipients" &&
                s.ValueType == "JSON" &&
                s.ValueJson == "[\"admin@example.com\"]"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateSettingAsync")]
    public async Task CreateSettingAsync_WhenTypeIsInt_ParsesAndSavesIntValue()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheServiceMock
            .Setup(c => c.RemoveAsync("system_setting:reservation.max", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await service.CreateSettingAsync("reservation.max", "Max", "INT", "50", cancellationToken: CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<SystemSetting>(s =>
                s.SettingKey == "reservation.max" &&
                s.ValueType == "INT" &&
                s.ValueInt == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UploadStoreFileAsync")]
    public async Task UploadStoreFileAsync_WhenVideo_RoutesToStoreVideosFolder()
    {
        var uploadResult = new FileUploadResult
        {
            RelativePath = "store-videos/intro.mp4",
            PublicUrl = "/uploads/store-videos/intro.mp4",
            OriginalFileName = "intro.mp4",
            SizeBytes = 100
        };

        _fileStorageMock
            .Setup(f => f.SaveAsync(
                It.IsAny<FileUploadRequest>(),
                "store-videos",
                It.IsAny<FileValidationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.UploadStoreFileAsync(stream, "intro.mp4", "video/mp4", CancellationToken.None);

        result.RelativePath.Should().Be("store-videos/intro.mp4");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UploadStoreLogoAsync")]
    public async Task UploadStoreLogoAsync_WhenImage_RoutesToStoreLogoFolder()
    {
        var uploadResult = new FileUploadResult
        {
            RelativePath = "store-logo/logo.png",
            PublicUrl = "/uploads/store-logo/logo.png",
            OriginalFileName = "logo.png",
            SizeBytes = 100
        };

        _fileStorageMock
            .Setup(f => f.SaveAsync(
                It.IsAny<FileUploadRequest>(),
                "store-logo",
                It.IsAny<FileValidationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.UploadStoreLogoAsync(stream, "logo.png", "image/png", CancellationToken.None);

        result.RelativePath.Should().Be("store-logo/logo.png");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UploadStoreLogoAsync")]
    public async Task UploadStoreLogoAsync_WhenJpegImage_AlsoRoutesToStoreLogoFolder()
    {
        var uploadResult = new FileUploadResult
        {
            RelativePath = "store-logo/banner.jpg",
            PublicUrl = "/uploads/store-logo/banner.jpg",
            OriginalFileName = "banner.jpg",
            SizeBytes = 5000
        };

        _fileStorageMock
            .Setup(f => f.SaveAsync(
                It.IsAny<FileUploadRequest>(),
                "store-logo",
                It.IsAny<FileValidationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);

        var result = await service.UploadStoreLogoAsync(stream, "banner.jpg", "image/jpeg", CancellationToken.None);

        result.RelativePath.Should().Be("store-logo/banner.jpg");
        result.OriginalFileName.Should().Be("banner.jpg");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UploadStoreFileAsync — additional coverage
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UploadStoreFileAsync")]
    public async Task UploadStoreFileAsync_WhenImage_RoutesToStoreMediaFolder()
    {
        var uploadResult = new FileUploadResult
        {
            RelativePath = "store-media/hero.png",
            PublicUrl = "/uploads/store-media/hero.png",
            OriginalFileName = "hero.png",
            SizeBytes = 2000
        };

        _fileStorageMock
            .Setup(f => f.SaveAsync(
                It.IsAny<FileUploadRequest>(),
                "store-media",
                It.IsAny<FileValidationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.UploadStoreFileAsync(stream, "hero.png", "image/png", CancellationToken.None);

        result.RelativePath.Should().Be("store-media/hero.png");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UploadStoreFileAsync")]
    public async Task UploadStoreFileAsync_WhenMp4ExtensionButDifferentContentType_StillRoutesToStoreVideos()
    {
        var uploadResult = new FileUploadResult
        {
            RelativePath = "store-videos/clip.mp4",
            PublicUrl = "/uploads/store-videos/clip.mp4",
            OriginalFileName = "clip.mp4",
            SizeBytes = 50000
        };

        _fileStorageMock
            .Setup(f => f.SaveAsync(
                It.IsAny<FileUploadRequest>(),
                "store-videos",
                It.IsAny<FileValidationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var service = CreateService();
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.UploadStoreFileAsync(stream, "clip.mp4", "application/octet-stream", CancellationToken.None);

        result.RelativePath.Should().Be("store-videos/clip.mp4");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CreateSettingAsync — additional coverage
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateSettingAsync")]
    public async Task CreateSettingAsync_WhenTypeIsBool_ParsesAndSavesBoolValue()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheServiceMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await service.CreateSettingAsync("feature.enabled", "Feature Toggle", "BOOL", "true",
            description: "Enable feature", cancellationToken: CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<SystemSetting>(s =>
                s.SettingKey == "feature.enabled" &&
                s.ValueType == "BOOL" &&
                s.ValueBool == true &&
                s.Description == "Enable feature"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateSettingAsync")]
    public async Task CreateSettingAsync_WhenIntValueUnparseable_DefaultsToZero()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheServiceMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await service.CreateSettingAsync("reservation.limit", "Limit", "INT", "not-a-number",
            cancellationToken: CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<SystemSetting>(s =>
                s.SettingKey == "reservation.limit" &&
                s.ValueType == "INT" &&
                s.ValueInt == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateSettingAsync")]
    public async Task CreateSettingAsync_WhenUnknownValueType_DefaultsToString()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheServiceMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await service.CreateSettingAsync("custom.field", "Custom", "UNKNOWN_TYPE", "raw-value",
            cancellationToken: CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<SystemSetting>(s =>
                s.SettingKey == "custom.field" &&
                s.ValueType == "STRING" &&
                s.ValueString == "raw-value"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetAllGroupedAsync — additional coverage
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllGroupedAsync")]
    public async Task GetAllGroupedAsync_WhenNoSettings_ReturnsEmptyDictionary()
    {
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());

        var service = CreateService();

        var result = await service.GetAllGroupedAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllGroupedAsync")]
    public async Task GetAllGroupedAsync_WhenAllKeysWithoutDot_AllGoToGeneralGroup()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("timezone", "STRING", valueString: "UTC"),
            MakeSetting("currency", "STRING", valueString: "VND")
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        var result = await service.GetAllGroupedAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result.Should().ContainKey("general");
        result["general"].Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetGroupAsync — additional coverage
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGroupAsync")]
    public async Task GetGroupAsync_WhenNonStoreGroup_ReturnsSettingsWithoutPublicUrl()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("reservation.default_duration_minutes", "INT", valueInt: 120),
            MakeSetting("reservation.max_party_size", "INT", valueInt: 20)
        };

        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("reservation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        var result = await service.GetGroupAsync("reservation", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.PublicUrl.Should().BeNull());
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetGroupAsync")]
    public async Task GetGroupAsync_WhenNoSettingsInGroup_ReturnsEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemSetting>());

        var service = CreateService();

        var result = await service.GetGroupAsync("nonexistent", CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetPublicGroupAsync — additional coverage
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPublicGroupAsync")]
    public async Task GetPublicGroupAsync_WhenAllSettingsSensitive_ReturnsEmptyList()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("store.smtp.password", "STRING", valueString: "secret", isSensitive: true),
            MakeSetting("store.api.key", "STRING", valueString: "key123", isSensitive: true)
        };

        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("store", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();

        var result = await service.GetPublicGroupAsync("store", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPublicGroupAsync")]
    public async Task GetPublicGroupAsync_WhenStoreGroupWithMediaKey_AttachesPublicUrlForNonSensitive()
    {
        var settings = new List<SystemSetting>
        {
            MakeSetting("store.logoUrl", "STRING", valueString: "uploads/store-logo/logo.png", isSensitive: false),
            MakeSetting("store.smtp.password", "STRING", valueString: "secret", isSensitive: true)
        };

        _repositoryMock
            .Setup(r => r.GetByGroupPrefixAsync("store", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _fileStorageMock
            .Setup(f => f.GetPublicUrl("store-logo/logo.png"))
            .Returns("/uploads/store-logo/logo.png");

        var service = CreateService();

        var result = await service.GetPublicGroupAsync("store", CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().SettingKey.Should().Be("store.logoUrl");
        result.Single().PublicUrl.Should().Be("/uploads/store-logo/logo.png");
    }
}
