using Core.Data;
using Core.DTO.General;
using Core.DTO.Table;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.LookUp;
using Core.Interface.Service.Notification;
using Core.Service;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Tests.Services;

/// <summary>
/// Unit Test — TableService
/// Code Module : Core/Service/TableService.cs
/// Method      : GetTableByIdAsync, CreateTableAsync, UpdateTableAsync,
///               DeleteTableAsync, UpdateStatusAsync, RegenerateQrCodeAsync
/// Created By  : AI Agent
/// Executed By : AI Agent
/// Test Req.   : Verify table management business logic including table retrieval,
///               creation with QR code generation, update with image management,
///               soft-delete with conflict detection, status transitions with
///               notification publishing, and QR code regeneration.
/// </summary>
public class TableServiceTests
{
    // ── Mocks ──
    private readonly Mock<ITableRepository> _tableRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<ILookupService> _lookupServiceMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<IMediaRepository> _mediaRepoMock = new();
    private readonly Mock<IQrCodeGenerator> _qrCodeGenMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();

    // ── IOptions ──
    private readonly IOptions<BaseUrlOptions> _baseUrlOpt =
        Options.Create(new BaseUrlOptions { Client = "https://example.com", Api = "https://api.example.com" });

    // ── Lookup IDs ──
    private const uint AvailableStatusId = 100;
    private const uint OccupiedStatusId = 101;
    private const uint ReservedStatusId = 102;
    private const uint LockedStatusId = 103;
    private const uint RegularTypeId = 200;
    private const uint IndoorZoneId = 300;
    private const uint ImageMediaTypeId = 400;

    // ── Factory ──
    private TableService CreateService() => new(
        _tableRepoMock.Object,
        _uowMock.Object,
        _lookupResolverMock.Object,
        _lookupServiceMock.Object,
        _fileStorageMock.Object,
        _mediaRepoMock.Object,
        _qrCodeGenMock.Object,
        _baseUrlOpt,
        _notificationMock.Object,
        _orderRepoMock.Object);

    // ── Test Data Helpers ──
    private static LookupValue MakeStatusLv(string code = "AVAILABLE", uint id = AvailableStatusId) => new()
    {
        ValueId = id,
        TypeId = (ushort)LookupTypeEnum.TableStatus,
        ValueCode = code,
        ValueName = code,
        IsActive = true
    };

    private static LookupValue MakeTypeLv(string code = "REGULAR", uint id = RegularTypeId) => new()
    {
        ValueId = id,
        TypeId = (ushort)LookupTypeEnum.TableType,
        ValueCode = code,
        ValueName = code,
        IsActive = true
    };

    private static LookupValue MakeZoneLv(string code = "INDOOR", uint id = IndoorZoneId) => new()
    {
        ValueId = id,
        TypeId = (ushort)LookupTypeEnum.TableZone,
        ValueCode = code,
        ValueName = code,
        IsActive = true
    };

    private static RestaurantTable MakeValidTable(long id = 1, string code = "TB-001") => new()
    {
        TableId = id,
        TableCode = code,
        Capacity = 4,
        IsOnline = true,
        TableStatusLvId = AvailableStatusId,
        TableTypeLvId = RegularTypeId,
        ZoneLvId = IndoorZoneId,
        QrToken = "abc123token",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDeleted = false,
        TableStatusLv = MakeStatusLv(),
        TableTypeLv = MakeTypeLv(),
        ZoneLv = MakeZoneLv(),
        TableQrImgNavigation = null,
        Orders = new List<Order>(),
        Reservations = new List<Reservation>(),
        ServiceErrors = new List<ServiceError>(),
        TableMedia = new List<TableMedium>()
    };

    private static RestaurantTable MakeTableWithQrImage(long id = 1) => new()
    {
        TableId = id,
        TableCode = "TB-001",
        Capacity = 4,
        IsOnline = true,
        TableStatusLvId = AvailableStatusId,
        TableTypeLvId = RegularTypeId,
        ZoneLvId = IndoorZoneId,
        QrToken = "oldtoken123",
        TableQrImg = 10L,
        TableQrImgNavigation = new MediaAsset
        {
            MediaId = 10,
            Url = "table-qr/old-qr.png",
            MimeType = "image/png",
            MediaTypeLvId = ImageMediaTypeId
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDeleted = false,
        TableStatusLv = MakeStatusLv(),
        TableTypeLv = MakeTypeLv(),
        ZoneLv = MakeZoneLv(),
        Orders = new List<Order>(),
        Reservations = new List<Reservation>(),
        ServiceErrors = new List<ServiceError>(),
        TableMedia = new List<TableMedium>()
    };

    private void SetupValidLookups()
    {
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(AvailableStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(OccupiedStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(ReservedStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(LockedStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(RegularTypeId, (ushort)LookupTypeEnum.TableType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(IndoorZoneId, (ushort)LookupTypeEnum.TableZone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupQrGeneration()
    {
        _qrCodeGenMock.Setup(q => q.GeneratePng(It.IsAny<string>()))
            .Returns(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "table-qr", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileUploadResult
            {
                RelativePath = "table-qr/qr-1.png",
                PublicUrl = "/uploads/table-qr/qr-1.png",
                OriginalFileName = "qr-1.png"
            });

        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.MediaType, nameof(MediaTypeCode.IMAGE), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageMediaTypeId);

        _mediaRepoMock.Setup(m => m.AddMediaAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaAsset a, CancellationToken _) =>
            {
                a.MediaId = 10;
                return a;
            });
    }

    // ══════════════════════════════════════════════════════════════════
    // ██  GetTableByIdAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetTableByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTableByIdAsync")]
    public async Task GetTableByIdAsync_WhenTableExists_ReturnsTableDetailDto()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.GetTableByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TableId.Should().Be(1);
        result.TableCode.Should().Be("TB-001");
        result.Capacity.Should().Be(4);
        result.StatusCode.Should().Be("AVAILABLE");
        result.TypeName.Should().Be("REGULAR");
        result.ZoneName.Should().Be("INDOOR");
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTableByIdAsync")]
    public async Task GetTableByIdAsync_WhenTableHasQrToken_ReturnsQrCodeUrl()
    {
        // Arrange
        var table = MakeValidTable(id: 2, code: "TB-002");
        table.QrToken = "token456";
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.GetTableByIdAsync(2);

        // Assert
        result.QrCodeUrl.Should().Contain("table=TB-002");
        result.QrCodeUrl.Should().Contain("token=token456");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTableByIdAsync")]
    public async Task GetTableByIdAsync_WhenIdIsMaxLong_AndNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(long.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.GetTableByIdAsync(long.MaxValue);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Table not found");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetTableByIdAsync")]
    public async Task GetTableByIdAsync_WhenTableNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.GetTableByIdAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Table not found");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // ██  CreateTableAsync
    // ══════════════════════════════════════════════════════════════════

    #region CreateTableAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenValidRequest_ReturnsTableDetailDto()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "TB-NEW",
            Capacity = 6,
            IsOnline = true,
            StatusLvId = AvailableStatusId,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        SetupValidLookups();
        SetupQrGeneration();

        _tableRepoMock.Setup(r => r.TableCodeExistsAsync("TB-NEW", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var savedTable = MakeValidTable(id: 5, code: "TB-NEW");
        savedTable.Capacity = 6;
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedTable);

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.CreateTableAsync(request, Array.Empty<MediaFileInput>());

        // Assert
        result.Should().NotBeNull();
        result.TableCode.Should().Be("TB-NEW");
        result.Capacity.Should().Be(6);
        _tableRepoMock.Verify(r => r.Add(It.Is<RestaurantTable>(t => t.TableCode == "TB-NEW")), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenValidRequestWithImages_UploadsImages()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "TB-IMG",
            Capacity = 4,
            IsOnline = false,
            StatusLvId = AvailableStatusId,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        SetupValidLookups();
        SetupQrGeneration();

        _tableRepoMock.Setup(r => r.TableCodeExistsAsync("TB-IMG", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var savedTable = MakeValidTable(id: 6, code: "TB-IMG");
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedTable);

        _fileStorageMock.Setup(f => f.SaveManyAsync(It.IsAny<IReadOnlyList<FileUploadRequest>>(), "table-media", It.IsAny<FileValidationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileUploadResult>
            {
                new() { RelativePath = "table-media/img1.jpg", PublicUrl = "/uploads/table-media/img1.jpg", OriginalFileName = "photo.jpg" }
            });

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var images = new List<MediaFileInput>
        {
            new() { Stream = new MemoryStream(new byte[] { 1, 2, 3 }), FileName = "photo.jpg", ContentType = "image/jpeg" }
        };

        var svc = CreateService();

        // Act
        var result = await svc.CreateTableAsync(request, images);

        // Assert
        result.Should().NotBeNull();
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenTableCodeIsEmpty_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "",
            Capacity = 4,
            StatusLvId = AvailableStatusId,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        var svc = CreateService();

        // Act
        var act = () => svc.CreateTableAsync(request, Array.Empty<MediaFileInput>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Table code is required");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenTableCodeIsWhitespace_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "   ",
            Capacity = 4,
            StatusLvId = AvailableStatusId,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        var svc = CreateService();

        // Act
        var act = () => svc.CreateTableAsync(request, Array.Empty<MediaFileInput>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Table code is required");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenCapacityIsZero_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "TB-001",
            Capacity = 0,
            StatusLvId = AvailableStatusId,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        var svc = CreateService();

        // Act
        var act = () => svc.CreateTableAsync(request, Array.Empty<MediaFileInput>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Capacity must be a positive integer");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenCapacityIsNegative_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "TB-001",
            Capacity = -1,
            StatusLvId = AvailableStatusId,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        var svc = CreateService();

        // Act
        var act = () => svc.CreateTableAsync(request, Array.Empty<MediaFileInput>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Capacity must be a positive integer");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenTableCodeAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "TB-DUP",
            Capacity = 4,
            StatusLvId = AvailableStatusId,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        _tableRepoMock.Setup(r => r.TableCodeExistsAsync("TB-DUP", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService();

        // Act
        var act = () => svc.CreateTableAsync(request, Array.Empty<MediaFileInput>());

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*TB-DUP*already exists*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateTableAsync")]
    public async Task CreateTableAsync_WhenInvalidStatusLookup_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateTableRequest
        {
            TableCode = "TB-001",
            Capacity = 4,
            StatusLvId = 9999,
            TypeLvId = RegularTypeId,
            ZoneLvId = IndoorZoneId
        };

        _tableRepoMock.Setup(r => r.TableCodeExistsAsync("TB-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(9999u, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = CreateService();

        // Act
        var act = () => svc.CreateTableAsync(request, Array.Empty<MediaFileInput>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid*table status*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // ██  UpdateTableAsync
    // ══════════════════════════════════════════════════════════════════

    #region UpdateTableAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenValidUpdate_ReturnsUpdatedTableDetailDto()
    {
        // Arrange
        var table = MakeValidTable(id: 1, code: "TB-001");
        table.TableQrImg = 10L; // Prevent QR regeneration branch
        table.TableQrImgNavigation = new MediaAsset { MediaId = 10, Url = "table-qr/qr-1.png", MimeType = "image/png", MediaTypeLvId = ImageMediaTypeId };

        var updatedTable = MakeValidTable(id: 1, code: "TB-001");
        updatedTable.Capacity = 8;
        updatedTable.IsOnline = false;

        _tableRepoMock.SetupSequence(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(updatedTable);

        var request = new UpdateTableRequest
        {
            Capacity = 8,
            IsOnline = false
        };

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.UpdateTableAsync(1, request, Array.Empty<MediaFileInput>(), Array.Empty<long>());

        // Assert
        result.Should().NotBeNull();
        result.Capacity.Should().Be(8);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenUpdatingTableCode_ReturnsNewCode()
    {
        // Arrange
        var table = MakeValidTable(id: 1, code: "TB-OLD");
        table.TableQrImg = 10L;
        table.TableQrImgNavigation = new MediaAsset { MediaId = 10, Url = "table-qr/qr-1.png", MimeType = "image/png", MediaTypeLvId = ImageMediaTypeId };

        var updatedTable = MakeValidTable(id: 1, code: "TB-NEW");

        _tableRepoMock.SetupSequence(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(updatedTable);
        _tableRepoMock.Setup(r => r.TableCodeExistsAsync("TB-NEW", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new UpdateTableRequest { TableCode = "TB-NEW" };

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.UpdateTableAsync(1, request, Array.Empty<MediaFileInput>(), Array.Empty<long>());

        // Assert
        result.Should().NotBeNull();
        table.TableCode.Should().Be("TB-NEW");
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenTableNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var request = new UpdateTableRequest { Capacity = 4 };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateTableAsync(999, request, Array.Empty<MediaFileInput>(), Array.Empty<long>());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Table not found");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenNewTableCodeIsBlank_ThrowsValidationException()
    {
        // Arrange
        var table = MakeValidTable(id: 1, code: "TB-001");
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var request = new UpdateTableRequest { TableCode = "   " };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateTableAsync(1, request, Array.Empty<MediaFileInput>(), Array.Empty<long>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Table code cannot be blank");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenNewTableCodeAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var table = MakeValidTable(id: 1, code: "TB-001");
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.TableCodeExistsAsync("TB-DUP", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateTableRequest { TableCode = "TB-DUP" };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateTableAsync(1, request, Array.Empty<MediaFileInput>(), Array.Empty<long>());

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*TB-DUP*already exists*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenCapacityIsZero_ThrowsValidationException()
    {
        // Arrange
        var table = MakeValidTable(id: 1, code: "TB-001");
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var request = new UpdateTableRequest { Capacity = 0 };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateTableAsync(1, request, Array.Empty<MediaFileInput>(), Array.Empty<long>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Capacity must be a positive integer");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenCapacityIsNegative_ThrowsValidationException()
    {
        // Arrange
        var table = MakeValidTable(id: 1, code: "TB-001");
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var request = new UpdateTableRequest { Capacity = -5 };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateTableAsync(1, request, Array.Empty<MediaFileInput>(), Array.Empty<long>());

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Capacity must be a positive integer");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateTableAsync")]
    public async Task UpdateTableAsync_WhenRemovingImages_DeletesMediaAndCommits()
    {
        // Arrange
        var mediaAsset = new MediaAsset { MediaId = 50, Url = "table-media/old.jpg", MimeType = "image/jpeg", MediaTypeLvId = ImageMediaTypeId };
        var tableMedia = new TableMedium { TableId = 1, MediaId = 50, IsPrimary = false, Media = mediaAsset };
        var table = MakeValidTable(id: 1, code: "TB-001");
        table.TableQrImg = 10L;
        table.TableQrImgNavigation = new MediaAsset { MediaId = 10, Url = "table-qr/qr-1.png", MimeType = "image/png", MediaTypeLvId = ImageMediaTypeId };

        _tableRepoMock.SetupSequence(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.GetTableMediaAsync(1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tableMedia);

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var request = new UpdateTableRequest();

        var svc = CreateService();

        // Act
        var result = await svc.UpdateTableAsync(1, request, Array.Empty<MediaFileInput>(), new long[] { 50 });

        // Assert
        _tableRepoMock.Verify(r => r.RemoveTableMedia(tableMedia), Times.Once);
        _mediaRepoMock.Verify(m => m.RemoveMediaAsync(mediaAsset, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // ██  DeleteTableAsync
    // ══════════════════════════════════════════════════════════════════

    #region DeleteTableAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteTableAsync")]
    public async Task DeleteTableAsync_WhenNoConflicts_SoftDeletesTable()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.CountActiveOrdersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _tableRepoMock.Setup(r => r.CountUpcomingReservationsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var svc = CreateService();

        // Act
        await svc.DeleteTableAsync(1);

        // Assert
        table.IsDeleted.Should().BeTrue();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteTableAsync")]
    public async Task DeleteTableAsync_WhenTableNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _tableRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.DeleteTableAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Table not found");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteTableAsync")]
    public async Task DeleteTableAsync_WhenHasActiveOrders_ThrowsConflictException()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.CountActiveOrdersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _tableRepoMock.Setup(r => r.CountUpcomingReservationsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var svc = CreateService();

        // Act
        var act = () => svc.DeleteTableAsync(1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Cannot delete*2 active order(s)*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteTableAsync")]
    public async Task DeleteTableAsync_WhenHasUpcomingReservations_ThrowsConflictException()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.CountActiveOrdersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _tableRepoMock.Setup(r => r.CountUpcomingReservationsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var svc = CreateService();

        // Act
        var act = () => svc.DeleteTableAsync(1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Cannot delete*3 upcoming reservation(s)*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteTableAsync")]
    public async Task DeleteTableAsync_WhenHasBothOrdersAndReservations_ThrowsConflictWithBoth()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.CountActiveOrdersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _tableRepoMock.Setup(r => r.CountUpcomingReservationsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var svc = CreateService();

        // Act
        var act = () => svc.DeleteTableAsync(1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*1 active order(s)*and*2 upcoming reservation(s)*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "DeleteTableAsync")]
    public async Task DeleteTableAsync_WhenZeroOrdersAndZeroReservations_SoftDeletesSuccessfully()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.CountActiveOrdersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _tableRepoMock.Setup(r => r.CountUpcomingReservationsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var svc = CreateService();

        // Act
        await svc.DeleteTableAsync(1);

        // Assert
        table.IsDeleted.Should().BeTrue();
        table.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // ██  UpdateStatusAsync
    // ══════════════════════════════════════════════════════════════════

    #region UpdateStatusAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateStatusAsync")]
    public async Task UpdateStatusAsync_WhenAvailableToOccupied_ReturnsUpdatedDto()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        table.TableStatusLvId = AvailableStatusId;
        table.TableStatusLv = MakeStatusLv("AVAILABLE", AvailableStatusId);

        var updatedTable = MakeValidTable(id: 1);
        updatedTable.TableStatusLvId = OccupiedStatusId;
        updatedTable.TableStatusLv = MakeStatusLv("OCCUPIED", OccupiedStatusId);

        _tableRepoMock.SetupSequence(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(updatedTable);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(OccupiedStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.GetLookupValueCodeAsync(OccupiedStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("OCCUPIED");

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var request = new UpdateTableStatusRequest { StatusLvId = OccupiedStatusId };

        var svc = CreateService();

        // Act
        var result = await svc.UpdateStatusAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationMock.Verify(n => n.PublishAsync(It.IsAny<Core.DTO.Notification.PublishNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateStatusAsync")]
    public async Task UpdateStatusAsync_WhenAvailableToLocked_Succeeds()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        table.TableStatusLv = MakeStatusLv("AVAILABLE", AvailableStatusId);

        var updatedTable = MakeValidTable(id: 1);
        updatedTable.TableStatusLvId = LockedStatusId;
        updatedTable.TableStatusLv = MakeStatusLv("LOCKED", LockedStatusId);

        _tableRepoMock.SetupSequence(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(updatedTable);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(LockedStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.GetLookupValueCodeAsync(LockedStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("LOCKED");

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var request = new UpdateTableStatusRequest { StatusLvId = LockedStatusId };

        var svc = CreateService();

        // Act
        var result = await svc.UpdateStatusAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        _notificationMock.Verify(n => n.PublishAsync(It.IsAny<Core.DTO.Notification.PublishNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateStatusAsync")]
    public async Task UpdateStatusAsync_WhenTableNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _tableRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var request = new UpdateTableStatusRequest { StatusLvId = OccupiedStatusId };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateStatusAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Table not found");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateStatusAsync")]
    public async Task UpdateStatusAsync_WhenInvalidStatusLookup_ThrowsValidationException()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(9999u, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new UpdateTableStatusRequest { StatusLvId = 9999 };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateStatusAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid*table status*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateStatusAsync")]
    public async Task UpdateStatusAsync_WhenInvalidTransition_OccupiedToAvailable_ThrowsValidationException()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        table.TableStatusLvId = OccupiedStatusId;
        table.TableStatusLv = MakeStatusLv("OCCUPIED", OccupiedStatusId);

        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(AvailableStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.GetLookupValueCodeAsync(AvailableStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("AVAILABLE");

        var request = new UpdateTableStatusRequest { StatusLvId = AvailableStatusId };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateStatusAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid status transition*OCCUPIED*AVAILABLE*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateStatusAsync")]
    public async Task UpdateStatusAsync_WhenInvalidTransition_LockedToOccupied_ThrowsValidationException()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        table.TableStatusLvId = LockedStatusId;
        table.TableStatusLv = MakeStatusLv("LOCKED", LockedStatusId);

        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(OccupiedStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.GetLookupValueCodeAsync(OccupiedStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("OCCUPIED");

        var request = new UpdateTableStatusRequest { StatusLvId = OccupiedStatusId };

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateStatusAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid status transition*LOCKED*OCCUPIED*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateStatusAsync")]
    public async Task UpdateStatusAsync_WhenLockedToAvailable_Succeeds()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        table.TableStatusLvId = LockedStatusId;
        table.TableStatusLv = MakeStatusLv("LOCKED", LockedStatusId);

        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(r => r.IsValidLookupAsync(AvailableStatusId, (ushort)LookupTypeEnum.TableStatus, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tableRepoMock.Setup(r => r.GetLookupValueCodeAsync(AvailableStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("AVAILABLE");

        var updatedTable = MakeValidTable(id: 1);
        updatedTable.TableStatusLvId = AvailableStatusId;
        updatedTable.TableStatusLv = MakeStatusLv("AVAILABLE", AvailableStatusId);
        // The second call to GetByIdAsync returns the updated table
        var callCount = 0;
        _tableRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? table : updatedTable;
            });

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var request = new UpdateTableStatusRequest { StatusLvId = AvailableStatusId };

        var svc = CreateService();

        // Act
        var result = await svc.UpdateStatusAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // ██  RegenerateQrCodeAsync
    // ══════════════════════════════════════════════════════════════════

    #region RegenerateQrCodeAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "RegenerateQrCodeAsync")]
    public async Task RegenerateQrCodeAsync_WhenTableHasNoOldQrImage_ReturnsNewQrCodeDto()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        table.TableQrImgNavigation = null;
        table.TableQrImg = null;

        _tableRepoMock.SetupSequence(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(table);

        SetupQrGeneration();

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.RegenerateQrCodeAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.QrCodeUrl.Should().Contain("table=TB-001");
        result.QrCodeUrl.Should().NotBeNullOrEmpty();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "RegenerateQrCodeAsync")]
    public async Task RegenerateQrCodeAsync_WhenTableHasOldQrImage_DeletesOldAndCreatesNew()
    {
        // Arrange
        var table = MakeTableWithQrImage(id: 1);
        var oldMedia = table.TableQrImgNavigation!;

        // After regeneration, the table has new QR image
        var updatedTable = MakeValidTable(id: 1);
        updatedTable.QrToken = "newtoken";
        updatedTable.TableQrImg = 20L;
        updatedTable.TableQrImgNavigation = new MediaAsset
        {
            MediaId = 20,
            Url = "table-qr/qr-1.png",
            MimeType = "image/png",
            MediaTypeLvId = ImageMediaTypeId
        };

        _tableRepoMock.SetupSequence(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(updatedTable);

        SetupQrGeneration();

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.RegenerateQrCodeAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.QrCodeUrl.Should().NotBeNullOrEmpty();
        result.QrCodeImageUrl.Should().NotBeNullOrEmpty();
        _mediaRepoMock.Verify(m => m.RemoveMediaAsync(oldMedia, It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteAsync("table-qr/old-qr.png"), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RegenerateQrCodeAsync")]
    public async Task RegenerateQrCodeAsync_WhenTableNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _tableRepoMock.Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.RegenerateQrCodeAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Table not found");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "RegenerateQrCodeAsync")]
    public async Task RegenerateQrCodeAsync_WhenQrImageGenerationFails_StillReturnsQrCodeUrl()
    {
        // Arrange
        var table = MakeValidTable(id: 1);
        table.TableQrImgNavigation = null;
        table.TableQrImg = null;

        var updatedTable = MakeValidTable(id: 1);
        updatedTable.TableQrImgNavigation = null;

        _tableRepoMock.SetupSequence(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(updatedTable);

        // QR generation succeeds but image save throws
        _qrCodeGenMock.Setup(q => q.GeneratePng(It.IsAny<string>()))
            .Returns(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        _fileStorageMock.Setup(f => f.SaveAsync(It.IsAny<FileUploadRequest>(), "table-qr", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.MediaType, nameof(MediaTypeCode.IMAGE), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageMediaTypeId);

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.RegenerateQrCodeAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.QrCodeUrl.Should().Contain("table=TB-001");
        // QR image URL should be null because generation failed
        result.QrCodeImageUrl.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "RegenerateQrCodeAsync")]
    public async Task RegenerateQrCodeAsync_WhenOldFileDeleteFails_StillReturnsSuccessfully()
    {
        // Arrange
        var table = MakeTableWithQrImage(id: 1);

        var updatedTable = MakeValidTable(id: 1);
        updatedTable.QrToken = "newtoken";
        updatedTable.TableQrImg = 20L;
        updatedTable.TableQrImgNavigation = new MediaAsset
        {
            MediaId = 20,
            Url = "table-qr/qr-1.png",
            MimeType = "image/png",
            MediaTypeLvId = ImageMediaTypeId
        };

        _tableRepoMock.SetupSequence(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table)
            .ReturnsAsync(updatedTable);

        SetupQrGeneration();

        // Old file delete fails
        _fileStorageMock.Setup(f => f.DeleteAsync("table-qr/old-qr.png"))
            .ThrowsAsync(new IOException("File locked"));

        _fileStorageMock.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns((string p) => $"/uploads/{p}");

        var svc = CreateService();

        // Act
        var result = await svc.RegenerateQrCodeAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.QrCodeUrl.Should().NotBeNullOrEmpty();
        result.QrCodeImageUrl.Should().NotBeNullOrEmpty();
    }

    #endregion
}
