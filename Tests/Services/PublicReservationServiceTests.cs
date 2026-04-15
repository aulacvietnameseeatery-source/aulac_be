using Core.Data;
using Core.DTO.Customer;
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
using Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace Tests.Services;

/// <summary>
/// Unit Test — PublicReservationService
/// Code Module : Core/Service/PublicReservationService.cs
/// Method      : CheckReservationFitAsync, GetAvailableTablesAsync, CreateReservationAsync
/// Created By  : quantm 
/// Executed By : quantm
/// Test Req.   : Kiểm tra logic đặt bàn công khai bao gồm kiểm tra khả năng đặt bàn,
///               lấy danh sách bàn có sẵn, và tạo đơn đặt bàn mới.
///               Xử lý xung đột đả vập và kiểm tra tính hợp lệ của lựa chọn bàn.
/// </summary>
public class PublicReservationServiceTests
{
    // Mocks
    private readonly Mock<ITableRepository>                 _tableRepoMock              = new();
    private readonly Mock<IReservationRepository>           _reservationRepoMock        = new();
    private readonly Mock<ILogger<PublicReservationService>> _loggerMock               = new();
    private readonly Mock<ILookupResolver>                  _lookupResolverMock         = new();
    private readonly Mock<IUnitOfWork>                      _uowMock                    = new();
    private readonly Mock<ISystemSettingService>            _systemSettingServiceMock   = new();
    private readonly Mock<ICustomerService>                 _customerServiceMock        = new();
    private readonly Mock<IEmailTemplateService>            _emailTemplateServiceMock   = new();
    private readonly Mock<IEmailQueue>                      _emailQueueMock             = new();
    private readonly Mock<IRealtimeNotificationService>     _realtimeNotificationMock   = new();
    private readonly Mock<INotificationService>             _notificationServiceMock    = new();
    private readonly Mock<IJobSchedulerService>             _jobSchedulerMock           = new();

    // IOptions cho RestaurantOptions
    private readonly IOptions<RestaurantOptions> _restaurantOptions =
        Options.Create(new RestaurantOptions { TimeZoneId = "UTC" });

    // Status IDs (lookup constants)
    private const uint RESERVATION_CANCELLED_ID = 1;
    private const uint RESERVATION_NO_SHOW_ID = 2;
    private const uint RESERVATION_COMPLETED_ID = 3;
    private const uint RESERVATION_PENDING_ID = 4;
    private const uint TABLE_OCCUPIED_ID = 5;
    private const uint TABLE_RESERVED_ID = 6;
    private const uint TABLE_LOCKED_ID = 7;
    private const uint RESERVATION_SOURCE_ONLINE_ID = 8;

    // Helper: tạo PublicReservationService
    private PublicReservationService CreateService() => new(
        _tableRepoMock.Object,
        _reservationRepoMock.Object,
        _loggerMock.Object,
        _lookupResolverMock.Object,
        _uowMock.Object,
        _systemSettingServiceMock.Object,
        _customerServiceMock.Object,
        _emailTemplateServiceMock.Object,
        _emailQueueMock.Object,
        _realtimeNotificationMock.Object,
        _notificationServiceMock.Object,
        _jobSchedulerMock.Object,
        _restaurantOptions);

    // Helper: tạo RestaurantTable giả
    private static RestaurantTable MakeTable(
        long tableId = 1,
        string tableCode = "T001",
        int capacity = 2,
        uint statusLvId = 1u) => new()
    {
        TableId = tableId,
        TableCode = tableCode,
        Capacity = capacity,
        TableStatusLvId = statusLvId,
        IsOnline = true,
        TableTypeLv = new LookupValue { ValueName = "Table" },
        ZoneLv = new LookupValue { ValueName = "Indoor" }
    };

    // Helper: tạo Reservation giả
    private static Reservation MakeReservation(
        long reservationId = 1,
        long customerId = 1,
        string customerName = "John Doe",
        string phone = "0123456789",
        string? email = "john@example.com",
        int partySize = 2,
        uint statusId = RESERVATION_PENDING_ID) => new()
    {
        ReservationId = reservationId,
        CustomerId = customerId,
        CustomerName = customerName,
        Phone = phone,
        Email = email,
        PartySize = partySize,
        ReservedTime = DateTime.UtcNow.AddHours(2),
        CreatedAt = DateTime.UtcNow,
        ReservationStatusLvId = statusId,
        SourceLvId = RESERVATION_SOURCE_ONLINE_ID,
        Tables = new List<RestaurantTable>()
    };

    // Helper: setup common mock behaviors
    private void SetupDefaultLookupBehavior()
    {
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ushort typeId, System.Enum code, CancellationToken ct) =>
            {
                return code switch
                {
                    ReservationStatusCode.CANCELLED => RESERVATION_CANCELLED_ID,
                    ReservationStatusCode.NO_SHOW => RESERVATION_NO_SHOW_ID,
                    ReservationStatusCode.COMPLETED => RESERVATION_COMPLETED_ID,
                    ReservationStatusCode.PENDING => RESERVATION_PENDING_ID,
                    TableStatusCode.OCCUPIED => TABLE_OCCUPIED_ID,
                    TableStatusCode.RESERVED => TABLE_RESERVED_ID,
                    TableStatusCode.LOCKED => TABLE_LOCKED_ID,
                    ReservationSourceCode.ONLINE => RESERVATION_SOURCE_ONLINE_ID,
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

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - CheckReservationFitAsync
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [Abnormal] Party size = 0 (không hợp lệ - giá trị mặc định)
    /// Precondition: Không kiểm tra DB
    /// Input: PartySize = 0
    /// Expected: CanBookOnline = false, Message = "Party size is invalid."
    /// </summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CheckReservationFitAsync")]
    public async Task CheckReservationFitAsync_WhenPartySizeIsZero_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var request = new ReservationFitCheckRequest
        {
            PartySize = 0,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CheckReservationFitAsync(request);

        // Assert
        result.CanBookOnline.Should().BeFalse();
        result.Message.Should().Contain("invalid");
    }

    /// <summary>
    /// [Abnormal] Party size = -1 (âm)
    /// Precondition: Không kiểm tra DB
    /// Input: PartySize = -1
    /// Expected: CanBookOnline = false
    /// </summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CheckReservationFitAsync")]
    public async Task CheckReservationFitAsync_WhenPartySizeIsNegative_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var request = new ReservationFitCheckRequest
        {
            PartySize = -5,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CheckReservationFitAsync(request);

        // Assert
        result.CanBookOnline.Should().BeFalse();
    }

    /// <summary>
    /// [Normal] Không có bàn trống phù hợp cho party size
    /// Precondition: DB trả về 0 bàn có sẵn sau khi tìm kiếm
    /// Input: PartySize = 10, nhưng không có bàn nào trống
    /// Expected: CanBookOnline = false
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CheckReservationFitAsync")]
    public async Task CheckReservationFitAsync_WhenNoAvailableTables_ReturnsFalse()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable>()); // 0 bàn

        var service = CreateService();
        var request = new ReservationFitCheckRequest
        {
            PartySize = 10,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CheckReservationFitAsync(request);

        // Assert
        result.CanBookOnline.Should().BeFalse();
        result.Message.Should().Contain("No suitable");
    }

    /// <summary>
    /// [Normal] Có bàn trống phù hợp cho party size
    /// Precondition: DB trả về 1+ bàn có sẵn đủ sức chứa
    /// Input: PartySize = 2, có bàn T001 (capacity=2) trống
    /// Expected: CanBookOnline = true
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CheckReservationFitAsync")]
    public async Task CheckReservationFitAsync_WhenAvailableTablesExist_ReturnsTrue()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var availableTable = MakeTable(1, "T001", 2);
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { availableTable });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();
        var request = new ReservationFitCheckRequest
        {
            PartySize = 2,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CheckReservationFitAsync(request);

        // Assert
        result.CanBookOnline.Should().BeTrue();
        result.Message.Should().Contain("can be arranged");
    }

    /// <summary>
    /// [Boundary] Party size = 1 (tối thiểu)
    /// Precondition: Có bàn với capacity >= 1
    /// Input: PartySize = 1
    /// Expected: CanBookOnline = true (nếu có bàn)
    /// </summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CheckReservationFitAsync")]
    public async Task CheckReservationFitAsync_WhenPartySizeIsOne_ReturnsTrue()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var availableTable = MakeTable(1, "T001", 4); // capacity = 4 >= 1
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { availableTable });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();
        var request = new ReservationFitCheckRequest
        {
            PartySize = 1,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CheckReservationFitAsync(request);

        // Assert
        result.CanBookOnline.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - GetAvailableTablesAsync
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [Normal] Lấy danh sách bàn, đánh dấu bàn bận
    /// Precondition: DB có 3 bàn, 1 cái bận (OCCUPIED), 2 cái trống
    /// Input: ReservedTime = 2 giờ tương lai
    /// Expected: IsAvailable = false cho bàn OCCUPIED, true cho 2 cái trống
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailableTablesAsync")]
    public async Task GetAvailableTablesAsync_WhenTableIsOccupied_MarkAsNotAvailable()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var tables = new List<RestaurantTable>
        {
            MakeTable(1, "T001", 2, TABLE_OCCUPIED_ID),  // occupied
            MakeTable(2, "T002", 2),                      // available
            MakeTable(3, "T003", 4)                       // available
        };

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tables);

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();
        var reservedTime = DateTime.UtcNow.AddHours(2);

        // Act
        var result = await service.GetAvailableTablesAsync(reservedTime);

        // Assert
        result.Should().HaveCount(3);
        result.First(t => t.TableCode == "T001").IsAvailable.Should().BeFalse();
        result.First(t => t.TableCode == "T002").IsAvailable.Should().BeTrue();
        result.First(t => t.TableCode == "T003").IsAvailable.Should().BeTrue();
    }

    /// <summary>
    /// [Normal] Bàn tồn tại xung đột lịch đặt trước
    /// Precondition: Bàn T001 có đặt bàn khiến xung đột với thời gian được chọn
    /// Input: ReservedTime = 2 giờ tương lai
    /// Expected: IsAvailable = false cho bàn có xung đột
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailableTablesAsync")]
    public async Task GetAvailableTablesAsync_WhenReservationConflictExists_MarkAsNotAvailable()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var table = MakeTable(1, "T001", 2);
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { table });

        // Bàn T001 (TableId=1) có xung đột
        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(1, It.IsAny<DateTime>(), It.IsAny<int>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { MakeReservation() }); // 1 đặt bàn → xung đột

        var service = CreateService();
        var reservedTime = DateTime.UtcNow.AddHours(2);

        // Act
        var result = await service.GetAvailableTablesAsync(reservedTime);

        // Assert
        result.Should().HaveCount(1);
        result.First().IsAvailable.Should().BeFalse();
    }

    /// <summary>
    /// [Normal] Có bàn trống, không xung đột
    /// Precondition: DB trả về bàn trống, không có đặt bàn
    /// Input: ReservedTime = 2 giờ tương lai
    /// Expected: IsAvailable = true
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailableTablesAsync")]
    public async Task GetAvailableTablesAsync_WhenTableIsAvailable_ReturnsTrue()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var table = MakeTable(1, "T001", 2);
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>()); // 0 xung đột

        var service = CreateService();
        var reservedTime = DateTime.UtcNow.AddHours(2);

        // Act
        var result = await service.GetAvailableTablesAsync(reservedTime);

        // Assert
        result.Should().HaveCount(1);
        result.First().IsAvailable.Should().BeTrue();
    }

    /// <summary>
    /// [Abnormal] Repository throws — exception propagates through service
    /// Precondition: Repository throws an exception when fetching tables
    /// Input: ReservedTime = 2 hours in future
    /// Expected: Exception propagates unchanged
    /// </summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAvailableTablesAsync")]
    public async Task GetAvailableTablesAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange — repository failure (e.g., DB connection error)
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetAvailableTablesAsync(DateTime.UtcNow.AddHours(2)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Database connection*");
    }

    /// <summary>
    /// [Boundary] Table is RESERVED (near real-time window)
    /// Precondition: DB returns 1 table with RESERVED status, reservation time within immediate window
    /// Input: ReservedTime = 30 min in future (within immediateWindow)
    /// Expected: IsAvailable = false for reserved table
    /// </summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAvailableTablesAsync")]
    public async Task GetAvailableTablesAsync_WhenTableIsReserved_MarkedAsNotAvailable()
    {
        // Arrange — reserved table within immediate window boundary
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var reservedTable = MakeTable(1, "T001", 2, TABLE_RESERVED_ID);
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { reservedTable });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = CreateService();
        var reservedTime = DateTime.UtcNow.AddMinutes(30); // within immediate window

        // Act
        var result = await service.GetAvailableTablesAsync(reservedTime);

        // Assert — reserved table should be marked unavailable
        result.Should().HaveCount(1);
        result.First().IsAvailable.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - CreateReservationAsync
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [Abnormal] Không tìm thấy bàn phù hợp
    /// Precondition: FindCandidateTablesAsync trả về 0 bàn
    /// Input: PartySize = 20, không có bàn nào đủ
    /// Expected: Ném InvalidOperationException
    /// </summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateReservationAsync")]
    public async Task CreateReservationAsync_WhenNoSuitableTables_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable>()); // 0 bàn

        _uowMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new CreateReservationRequest
        {
            CustomerName = "John Doe",
            Phone = "0123456789",
            Email = "john@example.com",
            PartySize = 20,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        await service.Invoking(s => s.CreateReservationAsync(request))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*không còn bàn*");
    }

    /// <summary>
    /// [Abnormal] Xung đột đồng thời (concurrency) — bàn vừa được khách khác giữ
    /// Precondition: FindCandidateTablesAsync trả về bàn, nhưng ValidateCandidateTablesAsync trả về false
    /// Input: Tạo đặt bàn, nhưng mô phỏng bàn được lock giữa lức tìm và validate
    /// Expected: Ném InvalidOperationException
    /// </summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateReservationAsync")]
    public async Task CreateReservationAsync_WhenConflictOccursDuringValidation_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var table = MakeTable(1, "T001", 2);
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { table });

        // Lần đầu: không có xung đột (FindCandidateTablesAsync)
        // Lần thứ hai: có bàn không tìm thấy (ValidateCandidateTablesAsync)
        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null); // simulatelock失败

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        _uowMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock
            .Setup(r => r.LockTablesForUpdateAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new CreateReservationRequest
        {
            CustomerName = "John Doe",
            Phone = "0123456789",
            Email = "john@example.com",
            PartySize = 2,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        await service.Invoking(s => s.CreateReservationAsync(request))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Bàn vừa được*");
    }

    /// <summary>
    /// [Normal] Tạo đặt bàn thành công với khách hàng mới
    /// Precondition: Có bàn phù hợp, không xung đột, khách hàng không tồn tại
    /// Input: CreateReservationRequest với CustomerId = null hoặc không tồn tại
    /// Expected: Tạo bản ghi Reservation, enqueue email, return ReservationResponseDto
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateReservationAsync")]
    public async Task CreateReservationAsync_WhenNewCustomer_CreatesReservationAndQueuesEmail()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var table = MakeTable(1, "T001", 2);
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        _uowMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock
            .Setup(r => r.LockTablesForUpdateAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var createdReservation = MakeReservation(customerId: 99, customerName: "John Doe");
        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdReservation);

        // Khách hàng mới (CustomerId = null hoặc không tồn tại)
        _customerServiceMock
            .Setup(c => c.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        _customerServiceMock
            .Setup(c => c.FindOrCreateCustomerIdAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(99);

        var service = CreateService();
        var request = new CreateReservationRequest
        {
            CustomerName = "John Doe",
            Phone = "0123456789",
            Email = "john@example.com",
            PartySize = 2,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CreateReservationAsync(request);

        // Assert
        result.ReservationId.Should().Be(1);
        result.CustomerName.Should().Be("John Doe");
        result.PartySize.Should().Be(2);
        result.Status.Should().Be("PENDING");

        // Verify email was enqueued
        _jobSchedulerMock.Verify(
            j => j.EnqueueReservationEmails(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Once);

        // Verify transaction was committed
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// [Normal] Tạo đặt bàn thành công với khách hàng cũ
    /// Precondition: Có bàn phù hợp, khách hàng tồn tại (CustomerId > 0)
    /// Input: CreateReservationRequest với CustomerId = 50 (khách hàng đã tồn tại)
    /// Expected: Sử dụng CustomerId hiện có, không tạo khách hàng mới
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateReservationAsync")]
    public async Task CreateReservationAsync_WhenExistingCustomer_UsesExistingCustomerId()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var table = MakeTable(1, "T001", 2);
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        _uowMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock
            .Setup(r => r.LockTablesForUpdateAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var createdReservation = MakeReservation(customerId: 50);
        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdReservation);

        // Khách hàng tồn tại với ID = 50
        var existingCustomer = new CustomerDto { CustomerId = 50 };
        _customerServiceMock
            .Setup(c => c.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var service = CreateService();
        var request = new CreateReservationRequest
        {
            CustomerId = 50,
            CustomerName = "John Doe",
            Phone = "0123456789",
            Email = "john@example.com",
            PartySize = 2,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CreateReservationAsync(request);

        // Assert
        result.ReservationId.Should().Be(1);

        // Verify FindOrCreateCustomerIdAsync KHÔNG được gọi (vì khách hàng đã tồn tại)
        _customerServiceMock.Verify(
            c => c.FindOrCreateCustomerIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// [Boundary] Party size = 1 (tối thiểu)
    /// Precondition: Có bàn, không xung đột
    /// Input: PartySize = 1
    /// Expected: Tạo đặt bàn thành công
    /// </summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateReservationAsync")]
    public async Task CreateReservationAsync_WhenPartySizeIsOne_CreatesReservation()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        var table = MakeTable(1, "T001", 4); // capacity = 4 >= 1
        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable> { table });

        _reservationRepoMock
            .Setup(r => r.GetTableReservationsForTimeAsync(
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        _uowMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock
            .Setup(r => r.LockTablesForUpdateAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var createdReservation = MakeReservation(partySize: 1);
        _reservationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdReservation);

        _customerServiceMock
            .Setup(c => c.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        _customerServiceMock
            .Setup(c => c.FindOrCreateCustomerIdAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(99);

        var service = CreateService();
        var request = new CreateReservationRequest
        {
            CustomerName = "John Doe",
            Phone = "0123456789",
            Email = "john@example.com",
            PartySize = 1,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var result = await service.CreateReservationAsync(request);

        // Assert
        result.PartySize.Should().Be(1);
        result.Status.Should().Be("PENDING");
    }

    /// <summary>
    /// [Normal] Kiểm tra rằng khi tạo đặt bàn thất bại, transaction được rollback
    /// Precondition: FindCandidateTablesAsync trả về 0 bàn (throw exception)
    /// Input: PartySize = 20
    /// Expected: RollbackAsync được gọi, không commit
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateReservationAsync")]
    public async Task CreateReservationAsync_OnFailure_CallsRollback()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultSystemSettings();

        _tableRepoMock
            .Setup(r => r.GetManualAvailableTablesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RestaurantTable>());

        _uowMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new CreateReservationRequest
        {
            CustomerName = "John Doe",
            Phone = "0123456789",
            Email = "john@example.com",
            PartySize = 20,
            ReservedTime = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        await service.Invoking(s => s.CreateReservationAsync(request))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        // Verify Rollback được gọi
        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
