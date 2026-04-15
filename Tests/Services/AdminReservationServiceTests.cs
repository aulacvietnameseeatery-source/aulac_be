using Core.Data;
using Core.DTO.Customer;
using Core.DTO.Notification;
using Core.DTO.Reservation;
using Core.Entity;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Others;
using Core.Interface.Service.Reservation;
using Core.DTO.Email;
using Core.DTO.EmailTemplate;
using Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace Tests.Services;

/// <summary>
/// Unit Test — AdminReservationService
/// Code Module : Core/Service/AdminReservationService.cs
/// Method      : GetReservationsAsync, GetReservationStatusesAsync, GetReservationDetailAsync,
///               CreateManualReservationAsync, AssignTableAndConfirmAsync,
///               UpdateReservationStatusAsync (string), UpdateReservationStatusAsync (enum),
///               CheckAndMarkNoShowAsync, LockTablesForReservationAsync,
///               UpdateReservationAsync, DeleteReservationAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Kiểm tra toàn bộ logic quản lý đặt bàn phía admin bao gồm:
///               CRUD đơn đặt bàn, duyệt + gán bàn, chuyển trạng thái,
///               Hangfire job (No-Show, Lock Tables), chỉnh sửa đơn, xóa đơn.
/// </summary>
public class AdminReservationServiceTests
{
    // ── Mocks ──────────────────────────────────────────────────────────────
    private readonly Mock<IReservationRepository>           _reservationRepoMock        = new();
    private readonly Mock<ITableRepository>                 _tableRepoMock              = new();
    private readonly Mock<ILogger<AdminReservationService>> _loggerMock                 = new();
    private readonly Mock<ILookupResolver>                  _lookupResolverMock         = new();
    private readonly Mock<IRealtimeNotificationService>     _realtimeNotificationMock   = new();
    private readonly Mock<IUnitOfWork>                      _uowMock                    = new();
    private readonly Mock<IJobSchedulerService>             _jobSchedulerMock           = new();
    private readonly Mock<ISystemSettingService>            _systemSettingServiceMock   = new();
    private readonly Mock<IReservationBroadcastService>     _broadcastServiceMock       = new();
    private readonly Mock<IOrderRepository>                 _orderRepoMock              = new();
    private readonly Mock<ICustomerService>                 _customerServiceMock        = new();
    private readonly Mock<INotificationService>             _notificationServiceMock    = new();
    private readonly Mock<IEmailTemplateService>            _emailTemplateServiceMock   = new();
    private readonly Mock<IEmailQueue>                      _emailQueueMock             = new();

    private readonly IOptions<RestaurantOptions> _restaurantOptions =
        Options.Create(new RestaurantOptions { TimeZoneId = "UTC" });

    // ── Status ID Constants ────────────────────────────────────────────────
    private const uint CANCELLED_STATUS_ID   = 10;
    private const uint NO_SHOW_STATUS_ID     = 11;
    private const uint COMPLETED_STATUS_ID   = 12;
    private const uint PENDING_STATUS_ID     = 13;
    private const uint CONFIRMED_STATUS_ID   = 14;
    private const uint CHECKED_IN_STATUS_ID  = 15;

    private const uint TABLE_AVAILABLE_ID    = 20;
    private const uint TABLE_OCCUPIED_ID     = 21;
    private const uint TABLE_RESERVED_ID     = 22;
    private const uint TABLE_LOCKED_ID       = 23;

    private const uint SOURCE_PHONE_ID       = 30;
    private const uint SOURCE_WALK_IN_ID     = 31;
    private const uint SOURCE_ONLINE_ID      = 32;

    private const uint ORDER_STATUS_PENDING_ID = 40;
    private const uint ORDER_SOURCE_DINE_IN_ID = 41;

    // ── Factory Method ─────────────────────────────────────────────────────
    private AdminReservationService CreateService() => new(
        _reservationRepoMock.Object,
        _tableRepoMock.Object,
        _loggerMock.Object,
        _lookupResolverMock.Object,
        _realtimeNotificationMock.Object,
        _uowMock.Object,
        _jobSchedulerMock.Object,
        _systemSettingServiceMock.Object,
        _broadcastServiceMock.Object,
        _orderRepoMock.Object,
        _customerServiceMock.Object,
        _notificationServiceMock.Object,
        _emailTemplateServiceMock.Object,
        _emailQueueMock.Object,
        _restaurantOptions);

    // ── Test Data Helpers ──────────────────────────────────────────────────
    private static RestaurantTable MakeTable(
        long tableId = 1,
        string tableCode = "T001",
        int capacity = 4,
        uint statusLvId = 20) => new()
    {
        TableId = tableId,
        TableCode = tableCode,
        Capacity = capacity,
        TableStatusLvId = statusLvId,
        IsOnline = true,
        TableTypeLv = new LookupValue { ValueName = "Table", ValueCode = "NORMAL" },
        ZoneLv = new LookupValue { ValueName = "Indoor", ValueCode = "INDOOR" },
        TableMedia = new List<TableMedium>()
    };

    private static Reservation MakeReservation(
        long reservationId = 1,
        long? customerId = 1,
        string customerName = "Nguyen Van A",
        string phone = "0901234567",
        string? email = "test@example.com",
        int partySize = 4,
        uint statusId = CONFIRMED_STATUS_ID,
        DateTime? reservedTime = null,
        List<RestaurantTable>? tables = null) => new()
    {
        ReservationId = reservationId,
        CustomerId = customerId,
        CustomerName = customerName,
        Phone = phone,
        Email = email,
        PartySize = partySize,
        ReservedTime = reservedTime ?? DateTime.UtcNow.AddHours(3),
        CreatedAt = DateTime.UtcNow,
        ReservationStatusLvId = statusId,
        SourceLvId = SOURCE_PHONE_ID,
        ReservationStatusLv = new LookupValue { ValueId = statusId, ValueCode = "CONFIRMED", ValueName = "Confirmed" },
        SourceLv = new LookupValue { ValueId = SOURCE_PHONE_ID, ValueCode = "PHONE", ValueName = "Phone" },
        Tables = tables ?? new List<RestaurantTable>()
    };

    // ── Common Setup Helpers ───────────────────────────────────────────────
    private void SetupDefaultLookupBehavior()
    {
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ushort typeId, System.Enum code, CancellationToken ct) =>
            {
                return code switch
                {
                    ReservationStatusCode.CANCELLED   => CANCELLED_STATUS_ID,
                    ReservationStatusCode.NO_SHOW     => NO_SHOW_STATUS_ID,
                    ReservationStatusCode.COMPLETED   => COMPLETED_STATUS_ID,
                    ReservationStatusCode.PENDING     => PENDING_STATUS_ID,
                    ReservationStatusCode.CONFIRMED   => CONFIRMED_STATUS_ID,
                    ReservationStatusCode.CHECKED_IN  => CHECKED_IN_STATUS_ID,
                    TableStatusCode.AVAILABLE         => TABLE_AVAILABLE_ID,
                    TableStatusCode.OCCUPIED          => TABLE_OCCUPIED_ID,
                    TableStatusCode.RESERVED          => TABLE_RESERVED_ID,
                    TableStatusCode.LOCKED            => TABLE_LOCKED_ID,
                    ReservationSourceCode.PHONE       => SOURCE_PHONE_ID,
                    ReservationSourceCode.WALK_IN     => SOURCE_WALK_IN_ID,
                    ReservationSourceCode.ONLINE      => SOURCE_ONLINE_ID,
                    OrderStatusCode.PENDING           => ORDER_STATUS_PENDING_ID,
                    OrderSourceCode.DINE_IN           => ORDER_SOURCE_DINE_IN_ID,
                    _ => 0u
                };
            });

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ushort typeId, string code, CancellationToken ct) =>
            {
                return code switch
                {
                    "CANCELLED"  => CANCELLED_STATUS_ID,
                    "NO_SHOW"    => NO_SHOW_STATUS_ID,
                    "COMPLETED"  => COMPLETED_STATUS_ID,
                    "PENDING"    => PENDING_STATUS_ID,
                    "CONFIRMED"  => CONFIRMED_STATUS_ID,
                    "CHECKED_IN" => CHECKED_IN_STATUS_ID,
                    "AVAILABLE"  => TABLE_AVAILABLE_ID,
                    "OCCUPIED"   => TABLE_OCCUPIED_ID,
                    "RESERVED"   => TABLE_RESERVED_ID,
                    "LOCKED"     => TABLE_LOCKED_ID,
                    _ => 0u
                };
            });
    }

    private void SetupDefaultSystemSettings()
    {
        _systemSettingServiceMock
            .Setup(s => s.GetIntAsync("reservation.default_duration_minutes", 120, It.IsAny<CancellationToken>()))
            .ReturnsAsync(120);

        _systemSettingServiceMock
            .Setup(s => s.GetIntAsync("reservation.immediate_window_minutes", 120, It.IsAny<CancellationToken>()))
            .ReturnsAsync(120);
    }

    private void SetupDefaultUoW()
    {
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetReservationsAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetReservationsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetReservationsAsync")]
    public async Task GetReservationsAsync_WhenDataExists_ReturnsList()
    {
        // Arrange
        var items = new List<ReservationManagementDto>
        {
            new() { ReservationId = 1, CustomerName = "Nguyen Van A" },
            new() { ReservationId = 2, CustomerName = "Tran Thi B" }
        };
        _reservationRepoMock
            .Setup(r => r.GetReservationsAsync(It.IsAny<GetReservationsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 2));

        var service = CreateService();
        var request = new GetReservationsRequest { PageIndex = 1, PageSize = 10 };

        // Act
        var (resultItems, totalCount) = await service.GetReservationsAsync(request);

        // Assert
        resultItems.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetReservationsAsync")]
    public async Task GetReservationsAsync_WhenNoData_ReturnsEmptyList()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetReservationsAsync(It.IsAny<GetReservationsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ReservationManagementDto>(), 0));

        var service = CreateService();
        var request = new GetReservationsRequest { PageIndex = 1, PageSize = 10 };

        // Act
        var (resultItems, totalCount) = await service.GetReservationsAsync(request);

        // Assert
        resultItems.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetReservationsAsync")]
    public async Task GetReservationsAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetReservationsAsync(It.IsAny<GetReservationsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB connection failed"));

        var service = CreateService();
        var request = new GetReservationsRequest { PageIndex = 1, PageSize = 10 };

        // Act & Assert
        await service.Invoking(s => s.GetReservationsAsync(request))
            .Should().ThrowAsync<Exception>().WithMessage("DB connection failed");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetReservationsAsync")]
    public async Task GetReservationsAsync_WhenPageSizeIsOne_ReturnsOneResult()
    {
        // Arrange — minimum valid page size boundary
        var items = new List<ReservationManagementDto>
        {
            new() { ReservationId = 1, CustomerName = "Nguyen Van A" }
        };
        _reservationRepoMock
            .Setup(r => r.GetReservationsAsync(It.IsAny<GetReservationsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 50));

        var service = CreateService();
        var request = new GetReservationsRequest { PageIndex = 1, PageSize = 1 };

        // Act
        var (resultItems, totalCount) = await service.GetReservationsAsync(request);

        // Assert
        resultItems.Should().HaveCount(1);
        totalCount.Should().Be(50);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetReservationStatusesAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetReservationStatusesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetReservationStatusesAsync")]
    public async Task GetReservationStatusesAsync_WhenStatusesExist_ReturnsMappedList()
    {
        // Arrange
        var lookupValues = new List<LookupValue>
        {
            new() { ValueId = 1, ValueName = "Pending", ValueCode = "PENDING" },
            new() { ValueId = 2, ValueName = "Confirmed", ValueCode = "CONFIRMED" }
        };
        _reservationRepoMock
            .Setup(r => r.GetReservationStatusesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookupValues);

        var service = CreateService();

        // Act
        var result = await service.GetReservationStatusesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].StatusId.Should().Be(1);
        result[0].StatusName.Should().Be("Pending");
        result[0].StatusCode.Should().Be("PENDING");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetReservationStatusesAsync")]
    public async Task GetReservationStatusesAsync_WhenNoStatuses_ReturnsEmptyList()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetReservationStatusesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LookupValue>());

        var service = CreateService();

        // Act
        var result = await service.GetReservationStatusesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetReservationStatusesAsync")]
    public async Task GetReservationStatusesAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange — repository failure (e.g., DB connection error)
        _reservationRepoMock
            .Setup(r => r.GetReservationStatusesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetReservationStatusesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Database connection*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetReservationDetailAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetReservationDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetReservationDetailAsync")]
    public async Task GetReservationDetailAsync_WhenExists_ReturnsDetail()
    {
        // Arrange
        var table = MakeTable(1, "T001", 4);
        var reservation = MakeReservation(tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        var result = await service.GetReservationDetailAsync(1);

        // Assert
        result.ReservationId.Should().Be(1);
        result.CustomerName.Should().Be("Nguyen Van A");
        result.Tables.Should().HaveCount(1);
        result.Tables[0].TableCode.Should().Be("T001");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetReservationDetailAsync")]
    public async Task GetReservationDetailAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetReservationDetailAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetReservationDetailAsync")]
    public async Task GetReservationDetailAsync_WhenNoTables_ReturnsEmptyTableList()
    {
        // Arrange
        var reservation = MakeReservation(tables: new List<RestaurantTable>());
        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        var result = await service.GetReservationDetailAsync(1);

        // Assert
        result.Tables.Should().BeEmpty();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — CreateManualReservationAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region CreateManualReservationAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenValidRequest_CreatesReservation()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var createdReservation = MakeReservation(reservationId: 10);
        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdReservation);

        _customerServiceMock
            .Setup(c => c.FindOrCreateCustomerIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = new List<long> { 1 },
            CustomerName = "Nguyen Van A",
            Phone = "0901234567",
            Email = "test@example.com",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act
        var result = await service.CreateManualReservationAsync(request);

        // Assert
        result.ReservationId.Should().Be(10);
        result.CustomerName.Should().Be("Nguyen Van A");
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenNoTableSelected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = new List<long>(),
            TableId = null,
            CustomerName = "Nguyen Van A",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act & Assert
        await service.Invoking(s => s.CreateManualReservationAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least one table*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenTableNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        _tableRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = new List<long> { 999 },
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act & Assert
        await service.Invoking(s => s.CreateManualReservationAsync(request))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenTableLockedForMaintenance_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var lockedTable = MakeTable(1, "T001", 4, TABLE_LOCKED_ID);
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockedTable);

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = new List<long> { 1 },
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act & Assert
        await service.Invoking(s => s.CreateManualReservationAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maintenance*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenTableHasConflict_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                1, It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { MakeReservation() }); // conflict

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = new List<long> { 1 },
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act & Assert
        await service.Invoking(s => s.CreateManualReservationAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has a reservation*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenCapacityInsufficient_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var smallTable = MakeTable(1, "T001", 2, TABLE_AVAILABLE_ID); // capacity=2 < partySize=10
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(smallTable);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = new List<long> { 1 },
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 10,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act & Assert
        await service.Invoking(s => s.CreateManualReservationAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not have enough capacity*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenExistingCustomerId_UsesExistingCustomer()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeReservation());

        _customerServiceMock
            .Setup(c => c.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDto { CustomerId = 50 });

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            CustomerId = 50,
            TableIds = new List<long> { 1 },
            CustomerName = "Nguyen Van A",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act
        await service.CreateManualReservationAsync(request);

        // Assert - FindOrCreateCustomerIdAsync should NOT be called
        _customerServiceMock.Verify(
            c => c.FindOrCreateCustomerIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenEmailProvided_EnqueuesConfirmationEmail()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var created = MakeReservation(email: "test@example.com");
        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        _customerServiceMock
            .Setup(c => c.FindOrCreateCustomerIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        _emailTemplateServiceMock
            .Setup(e => e.GetByCodeAsync("RESERVATION_CONFIRM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailTemplateDto { Subject = "Confirm", BodyHtml = "Hello {{CustomerName}}" });

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = new List<long> { 1 },
            CustomerName = "Test",
            Phone = "0901234567",
            Email = "test@example.com",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act
        await service.CreateManualReservationAsync(request);

        // Assert
        _emailQueueMock.Verify(e => e.EnqueueAsync(It.IsAny<Core.DTO.Email.QueuedEmail>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateManualReservationAsync")]
    public async Task CreateManualReservationAsync_WhenFallbackToSingleTableId_Works()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeReservation());

        _customerServiceMock
            .Setup(c => c.FindOrCreateCustomerIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService();
        var request = new CreateManualReservationRequest
        {
            TableIds = null,      // null TableIds
            TableId = 1,          // fallback to single TableId
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            Status = "CONFIRMED",
            Source = "PHONE"
        };

        // Act
        var result = await service.CreateManualReservationAsync(request);

        // Assert
        result.ReservationId.Should().BeGreaterThan(0);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — AssignTableAndConfirmAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region AssignTableAndConfirmAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AssignTableAndConfirmAsync")]
    public async Task AssignTableAndConfirmAsync_WhenReservationHasTables_ConfirmsAndSchedulesJobs()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4);
        var reservation = MakeReservation(
            statusId: PENDING_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddHours(5),
            tables: new List<RestaurantTable> { table });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "PENDING" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _jobSchedulerMock
            .Setup(j => j.ScheduleTableLock(It.IsAny<long>(), It.IsAny<TimeSpan>()))
            .Returns("job-id");

        _jobSchedulerMock
            .Setup(j => j.ScheduleNoShowCheck(It.IsAny<long>(), It.IsAny<TimeSpan>()))
            .Returns("job-id");

        var service = CreateService();

        // Act
        await service.AssignTableAndConfirmAsync(1, new List<long> { 1 });

        // Assert
        reservation.ReservationStatusLvId.Should().Be(CONFIRMED_STATUS_ID);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jobSchedulerMock.Verify(j => j.ScheduleNoShowCheck(1, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AssignTableAndConfirmAsync")]
    public async Task AssignTableAndConfirmAsync_WhenReservationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.AssignTableAndConfirmAsync(999, new List<long> { 1 }))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AssignTableAndConfirmAsync")]
    public async Task AssignTableAndConfirmAsync_WhenNoTablesAssigned_ThrowsInvalidOperationException()
    {
        // Arrange
        var reservation = MakeReservation(tables: new List<RestaurantTable>());
        reservation.Tables = null!;

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.AssignTableAndConfirmAsync(1, new List<long> { 1 }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chưa có bàn*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "AssignTableAndConfirmAsync")]
    public async Task AssignTableAndConfirmAsync_WhenReservationTimeLessThan2Hours_LocksTableImmediately()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4);
        var reservation = MakeReservation(
            statusId: PENDING_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddMinutes(30), // < 2 hours
            tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _jobSchedulerMock
            .Setup(j => j.ScheduleNoShowCheck(It.IsAny<long>(), It.IsAny<TimeSpan>()))
            .Returns("job-id");

        var service = CreateService();

        // Act
        await service.AssignTableAndConfirmAsync(1, new List<long> { 1 });

        // Assert - Table should be updated to RESERVED immediately
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_RESERVED_ID, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — UpdateReservationStatusAsync (string overload)
    // ═══════════════════════════════════════════════════════════════════════

    #region UpdateReservationStatusAsync_String

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateReservationStatusAsync")]
    public async Task UpdateReservationStatusAsync_WhenCheckedIn_SetsTableToOccupied()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var service = CreateService();

        // Act
        await service.UpdateReservationStatusAsync(1, "CHECKED_IN");

        // Assert
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_OCCUPIED_ID, It.IsAny<CancellationToken>()),
            Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateReservationStatusAsync")]
    public async Task UpdateReservationStatusAsync_WhenCancelled_SetsTableToAvailable()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_RESERVED_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        await service.UpdateReservationStatusAsync(1, "CANCELLED");

        // Assert
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_AVAILABLE_ID, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationStatusAsync")]
    public async Task UpdateReservationStatusAsync_WhenReservationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationStatusAsync(999, "CANCELLED"))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateReservationStatusAsync")]
    public async Task UpdateReservationStatusAsync_WhenNoteProvided_AppendsNote()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var reservation = MakeReservation(statusId: CONFIRMED_STATUS_ID);
        reservation.Notes = "Original note";

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        await service.UpdateReservationStatusAsync(1, "CANCELLED", "Customer called to cancel");

        // Assert
        reservation.Notes.Should().Contain("Original note");
        reservation.Notes.Should().Contain("Customer called to cancel");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationStatusAsync")]
    public async Task UpdateReservationStatusAsync_WhenCheckedInTableAlreadyOccupied_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var occupiedTable = MakeTable(1, "T001", 4, TABLE_OCCUPIED_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            tables: new List<RestaurantTable> { occupiedTable });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occupiedTable);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationStatusAsync(1, "CHECKED_IN"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã được xếp cho khách khác*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateReservationStatusAsync")]
    public async Task UpdateReservationStatusAsync_WhenConfirmedAndTimeFar_SchedulesTableLockInFuture()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4);
        var reservation = MakeReservation(
            statusId: PENDING_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddHours(5), // > 2 hours away
            tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _jobSchedulerMock
            .Setup(j => j.ScheduleTableLock(It.IsAny<long>(), It.IsAny<TimeSpan>()))
            .Returns("job-id");

        _jobSchedulerMock
            .Setup(j => j.ScheduleNoShowCheck(It.IsAny<long>(), It.IsAny<TimeSpan>()))
            .Returns("job-id");

        var service = CreateService();

        // Act
        await service.UpdateReservationStatusAsync(1, "CONFIRMED");

        // Assert - Table lock should be scheduled, not applied immediately
        _jobSchedulerMock.Verify(j => j.ScheduleTableLock(1, It.IsAny<TimeSpan>()), Times.Once);
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(It.IsAny<long>(), TABLE_RESERVED_ID, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — UpdateReservationStatusAsync (enum overload)
    // ═══════════════════════════════════════════════════════════════════════

    #region UpdateReservationStatusAsync_Enum

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateReservationStatusAsync_Enum")]
    public async Task UpdateReservationStatusAsync_EnumOverload_WhenCheckedIn_CreatesOrderAndReturnsId()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddMinutes(10), // within check-in window
            tables: new List<RestaurantTable> { table });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithTablesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepoMock
            .Setup(o => o.GetActiveOrderByTableAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepoMock
            .Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Order, CancellationToken>((order, _) => order.OrderId = 555);

        _tableRepoMock
            .Setup(t => t.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _customerServiceMock
            .Setup(c => c.GetGuestCustomerIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService();
        var request = new UpdateReservationStatusRequest
        {
            Status = ReservationStatusCode.CHECKED_IN
        };

        // Act
        var result = await service.UpdateReservationStatusAsync(1, 100, request, default);

        // Assert
        result.Status.Should().Be(ReservationStatusCode.CHECKED_IN);
        result.CreatedOrderId.Should().Be(555);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationStatusAsync_Enum")]
    public async Task UpdateReservationStatusAsync_EnumOverload_WhenAlreadySameStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var reservation = MakeReservation(statusId: CONFIRMED_STATUS_ID);
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithTablesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();
        var request = new UpdateReservationStatusRequest
        {
            Status = ReservationStatusCode.CONFIRMED
        };

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationStatusAsync(1, 100, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in this status*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationStatusAsync_Enum")]
    public async Task UpdateReservationStatusAsync_EnumOverload_WhenReservationCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var reservation = MakeReservation(statusId: CANCELLED_STATUS_ID);
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CANCELLED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithTablesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();
        var request = new UpdateReservationStatusRequest
        {
            Status = ReservationStatusCode.CONFIRMED
        };

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationStatusAsync(1, 100, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already closed*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationStatusAsync_Enum")]
    public async Task UpdateReservationStatusAsync_EnumOverload_WhenReservationNotFound_ThrowsException()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdWithTablesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();
        var request = new UpdateReservationStatusRequest
        {
            Status = ReservationStatusCode.CHECKED_IN
        };

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationStatusAsync(999, 100, request, default))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Reservation not found");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationStatusAsync_Enum")]
    public async Task UpdateReservationStatusAsync_EnumOverload_WhenCheckInOutsideWindow_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        var table = MakeTable(1, "T001", 4);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddHours(5), // far in the future
            tables: new List<RestaurantTable> { table });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithTablesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new UpdateReservationStatusRequest
        {
            Status = ReservationStatusCode.CHECKED_IN
        };

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationStatusAsync(1, 100, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Check-in allowed only*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationStatusAsync_Enum")]
    public async Task UpdateReservationStatusAsync_EnumOverload_WhenTableHasActiveOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddMinutes(10),
            tables: new List<RestaurantTable> { table });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithTablesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepoMock
            .Setup(o => o.GetActiveOrderByTableAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order { OrderId = 100 });

        var service = CreateService();
        var request = new UpdateReservationStatusRequest
        {
            Status = ReservationStatusCode.CHECKED_IN
        };

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationStatusAsync(1, 100, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has active order*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateReservationStatusAsync_Enum")]
    public async Task UpdateReservationStatusAsync_EnumOverload_WhenCancellingConfirmed_UpdatesStatusAndCommits()
    {
        // Arrange — boundary: cancel from CONFIRMED (non-terminal to terminal status)
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_RESERVED_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            tables: new List<RestaurantTable> { table });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithTablesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new UpdateReservationStatusRequest
        {
            Status = ReservationStatusCode.CANCELLED
        };

        // Act
        var result = await service.UpdateReservationStatusAsync(1, 100, request, default);

        // Assert — status updated, no order created (only CHECKED_IN creates orders)
        result.Status.Should().Be(ReservationStatusCode.CANCELLED);
        result.CreatedOrderId.Should().BeNull();
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — CheckAndMarkNoShowAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region CheckAndMarkNoShowAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CheckAndMarkNoShowAsync")]
    public async Task CheckAndMarkNoShowAsync_WhenConfirmed_MarksNoShowAndReleaseTables()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_RESERVED_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            tables: new List<RestaurantTable> { table });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        await service.CheckAndMarkNoShowAsync(1);

        // Assert
        reservation.ReservationStatusLvId.Should().Be(NO_SHOW_STATUS_ID);
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_AVAILABLE_ID, It.IsAny<CancellationToken>()),
            Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CheckAndMarkNoShowAsync")]
    public async Task CheckAndMarkNoShowAsync_WhenNotConfirmed_DoesNothing()
    {
        // Arrange
        var reservation = MakeReservation(statusId: CHECKED_IN_STATUS_ID);
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CHECKED_IN" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        await service.CheckAndMarkNoShowAsync(1);

        // Assert - No table updates, no commit
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(It.IsAny<long>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CheckAndMarkNoShowAsync")]
    public async Task CheckAndMarkNoShowAsync_WhenReservationNotFound_DoesNothing()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();

        // Act
        await service.CheckAndMarkNoShowAsync(999);

        // Assert - No updates
        _uowMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CheckAndMarkNoShowAsync")]
    public async Task CheckAndMarkNoShowAsync_WhenNoTables_StillUpdatesStatus()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var reservation = MakeReservation(statusId: CONFIRMED_STATUS_ID, tables: new List<RestaurantTable>());
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };
        reservation.Tables = null!;

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        await service.CheckAndMarkNoShowAsync(1);

        // Assert
        reservation.ReservationStatusLvId.Should().Be(NO_SHOW_STATUS_ID);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — LockTablesForReservationAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region LockTablesForReservationAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LockTablesForReservationAsync")]
    public async Task LockTablesForReservationAsync_WhenConfirmedAndWithin2Hours_LocksAvailableTables()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddHours(1), // within 2h window
            tables: new List<RestaurantTable> { table });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var service = CreateService();

        // Act
        await service.LockTablesForReservationAsync(1);

        // Assert
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_RESERVED_ID, It.IsAny<CancellationToken>()),
            Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LockTablesForReservationAsync")]
    public async Task LockTablesForReservationAsync_WhenNotConfirmed_DoesNothing()
    {
        // Arrange
        var reservation = MakeReservation(statusId: CHECKED_IN_STATUS_ID);
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CHECKED_IN" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var service = CreateService();

        // Act
        await service.LockTablesForReservationAsync(1);

        // Assert
        _uowMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "LockTablesForReservationAsync")]
    public async Task LockTablesForReservationAsync_WhenReservationNotFound_DoesNothing()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();

        // Act
        await service.LockTablesForReservationAsync(999);

        // Assert
        _uowMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "LockTablesForReservationAsync")]
    public async Task LockTablesForReservationAsync_WhenTableAlreadyOccupied_SkipsLock()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var occupiedTable = MakeTable(1, "T001", 4, TABLE_OCCUPIED_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            reservedTime: DateTime.UtcNow.AddHours(1),
            tables: new List<RestaurantTable> { occupiedTable });
        reservation.ReservationStatusLv = new LookupValue { ValueCode = "CONFIRMED" };

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occupiedTable);

        var service = CreateService();

        // Act
        await service.LockTablesForReservationAsync(1);

        // Assert - Should NOT lock an occupied table
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_RESERVED_ID, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — DeleteReservationAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region DeleteReservationAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteReservationAsync")]
    public async Task DeleteReservationAsync_WhenExists_DeletesAndReleaseTables()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_RESERVED_ID);
        var reservation = MakeReservation(tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.DeleteReservationAsync(1);

        // Assert
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_AVAILABLE_ID, It.IsAny<CancellationToken>()),
            Times.Once);
        _reservationRepoMock.Verify(
            r => r.DeleteAsync(reservation, It.IsAny<CancellationToken>()),
            Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteReservationAsync")]
    public async Task DeleteReservationAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.DeleteReservationAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "DeleteReservationAsync")]
    public async Task DeleteReservationAsync_WhenNoTables_DeletesWithoutTableUpdate()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var reservation = MakeReservation(tables: new List<RestaurantTable>());
        reservation.Tables = null!;

        _reservationRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.DeleteReservationAsync(1);

        // Assert
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(It.IsAny<long>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _reservationRepoMock.Verify(
            r => r.DeleteAsync(reservation, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteReservationAsync")]
    public async Task DeleteReservationAsync_WhenDeleteFails_RollsBack()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var reservation = MakeReservation(tables: new List<RestaurantTable>());
        reservation.Tables = null!;

        _reservationRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.DeleteReservationAsync(1))
            .Should().ThrowAsync<Exception>();

        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — UpdateReservationAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region UpdateReservationAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateReservationAsync")]
    public async Task UpdateReservationAsync_WhenValidRequest_UpdatesReservation()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepoMock
            .Setup(r => r.GetReservationStatusesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LookupValue>
            {
                new() { ValueId = CONFIRMED_STATUS_ID, ValueCode = "CONFIRMED" }
            });

        var service = CreateService();
        var request = new UpdateReservationRequest
        {
            CustomerName = "Updated Name",
            Phone = "0999888777",
            Email = "updated@example.com",
            PartySize = 4,
            ReservedTime = reservation.ReservedTime, // same time
            Notes = "Updated notes"
        };

        // Act
        await service.UpdateReservationAsync(1, request);

        // Assert
        reservation.CustomerName.Should().Be("Updated Name");
        reservation.Phone.Should().Be("0999888777");
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationAsync")]
    public async Task UpdateReservationAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var service = CreateService();
        var request = new UpdateReservationRequest
        {
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3)
        };

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationAsync(999, request))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateReservationAsync")]
    public async Task UpdateReservationAsync_WhenNoTablesInRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        var reservation = MakeReservation(tables: new List<RestaurantTable>());

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new UpdateReservationRequest
        {
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = DateTime.UtcNow.AddHours(3),
            TableIds = new List<long>() // explicit empty list
        };

        // Act & Assert
        await service.Invoking(s => s.UpdateReservationAsync(1, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least one table*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateReservationAsync")]
    public async Task UpdateReservationAsync_WhenTableChanged_RevalidatesTables()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();
        SetupDefaultUoW();

        var oldTable = MakeTable(1, "T001", 4);
        var newTable = MakeTable(2, "T002", 6, TABLE_AVAILABLE_ID);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            tables: new List<RestaurantTable> { oldTable });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepoMock
            .Setup(r => r.GetReservationStatusesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LookupValue>
            {
                new() { ValueId = CONFIRMED_STATUS_ID, ValueCode = "CONFIRMED" }
            });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        _tableRepoMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTable);

        var service = CreateService();
        var request = new UpdateReservationRequest
        {
            CustomerName = "Test",
            Phone = "0901234567",
            PartySize = 4,
            ReservedTime = reservation.ReservedTime,
            TableIds = new List<long> { 2 } // changing from T001 to T002
        };

        // Act
        await service.UpdateReservationAsync(1, request);

        // Assert
        _tableRepoMock.Verify(
            t => t.UpdateStatusAsync(1, TABLE_AVAILABLE_ID, It.IsAny<CancellationToken>()),
            Times.Once); // old table released
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateReservationAsync")]
    public async Task UpdateReservationAsync_WhenSameTablesSameTime_SkipsRevalidation()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4);
        var reservedTime = DateTime.UtcNow.AddHours(3);
        var reservation = MakeReservation(
            statusId: CONFIRMED_STATUS_ID,
            partySize: 4,
            reservedTime: reservedTime,
            tables: new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetByIdWithFullDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _reservationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepoMock
            .Setup(r => r.GetReservationStatusesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LookupValue>
            {
                new() { ValueId = CONFIRMED_STATUS_ID, ValueCode = "CONFIRMED" }
            });

        var service = CreateService();
        var request = new UpdateReservationRequest
        {
            CustomerName = "Updated Name",
            Phone = "0901234567",
            PartySize = 4,             // same
            ReservedTime = reservedTime, // same
            Notes = "Update info only"
            // TableIds = null → keeps current tables
        };

        // Act
        await service.UpdateReservationAsync(1, request);

        // Assert - No table revalidation needed
        _tableRepoMock.Verify(
            t => t.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        reservation.CustomerName.Should().Be("Updated Name");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetManualAvailableTablesAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetManualAvailableTablesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetManualAvailableTablesAsync")]
    public async Task GetManualAvailableTablesAsync_WhenNoPartySize_ReturnsAllTablesAsSingleOptions()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var tables = new List<RestaurantTable>
        {
            MakeTable(1, "T001", 2),
            MakeTable(2, "T002", 4)
        };

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tables);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();

        // Act
        var result = await service.GetManualAvailableTablesAsync(
            DateTime.UtcNow.AddHours(3), null);

        // Assert
        result.Should().HaveCount(2);
        result.All(x => x.TableCount == 1).Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetManualAvailableTablesAsync")]
    public async Task GetManualAvailableTablesAsync_WhenPartySizeProvided_ReturnsBestFitOption()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var tables = new List<RestaurantTable>
        {
            MakeTable(1, "T001", 2),
            MakeTable(2, "T002", 4),
            MakeTable(3, "T003", 6)
        };

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tables);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();

        // Act
        var result = await service.GetManualAvailableTablesAsync(
            DateTime.UtcNow.AddHours(3), 4);

        // Assert
        result.Should().NotBeEmpty();
        result.First().IsBestFit.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetManualAvailableTablesAsync")]
    public async Task GetManualAvailableTablesAsync_WhenNoTablesAvailable_ReturnsEmptyList()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable>());

        var service = CreateService();

        // Act
        var result = await service.GetManualAvailableTablesAsync(
            DateTime.UtcNow.AddHours(3), 4);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetManualAvailableTablesAsync")]
    public async Task GetManualAvailableTablesAsync_WhenPartySizeZero_ReturnsAllTablesWithoutGrouping()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var tables = new List<RestaurantTable>
        {
            MakeTable(1, "T001", 2),
            MakeTable(2, "T002", 4)
        };

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tables);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();

        // Act
        var result = await service.GetManualAvailableTablesAsync(
            DateTime.UtcNow.AddHours(3), 0);

        // Assert - partySize <= 0 returns individual tables
        result.Should().HaveCount(2);
        result.All(x => x.IsBestFit == false).Should().BeTrue();
    }

    #endregion
}
