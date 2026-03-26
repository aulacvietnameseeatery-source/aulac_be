using Core.Data;
using Core.DTO.Notification;
using Core.DTO.Reservation;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Others;
using Core.Service.Utils;
using Microsoft.Extensions.Logging;
using Core.Extensions;
using System.Text.RegularExpressions;
using Hangfire;
using Core.Interface.Service.Reservation;
using Core.Entity;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Email;
using Core.DTO.Email;
using Core.Interface.Service;

namespace Core.Service
{
    public class AdminReservationService : IAdminReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ILogger<AdminReservationService> _logger;
        private readonly ITableRepository _tableRepository;
        private readonly ILookupResolver _lookupResolver;
        private readonly IRealtimeNotificationService _realtimeNotification;
        private readonly IUnitOfWork _uow;
        private readonly IJobSchedulerService _jobScheduler;
        private readonly ISystemSettingService _systemSettingService;
        private readonly IReservationBroadcastService _broadcastService;
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerService _customerService;
        private readonly INotificationService _notificationService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IEmailQueue _emailQueue;

        private const string TemplateCodeReservationConfirmation = "RESERVATION_CONFIRM";

        private const string SettingReservationDuration = "reservation.default_duration_minutes";
        private const string SettingImmediateWindow = "reservation.immediate_window_minutes";

        public AdminReservationService(
            IReservationRepository reservationRepository,
            ITableRepository tableRepository,
            ILogger<AdminReservationService> logger,
            ILookupResolver lookupResolver,
            IRealtimeNotificationService realtimeNotification,
            IUnitOfWork uow,
            IJobSchedulerService jobScheduler,
            ISystemSettingService systemSettingService,
            IReservationBroadcastService broadcastService,
            IOrderRepository orderRepository,
            ICustomerService customerService,
            INotificationService notificationService,
            IEmailTemplateService emailTemplateService,
            IEmailQueue emailQueue)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _logger = logger;
            _lookupResolver = lookupResolver;
            _realtimeNotification = realtimeNotification;
            _uow = uow;
            _jobScheduler = jobScheduler;
            _systemSettingService = systemSettingService;
            _broadcastService = broadcastService;
            _orderRepository = orderRepository;
            _customerService = customerService;
            _notificationService = notificationService;
            _emailTemplateService = emailTemplateService;
            _emailQueue = emailQueue;
        }

        public async Task<(List<ReservationManagementDto> Items, int TotalCount)> GetReservationsAsync(GetReservationsRequest request, CancellationToken cancellationToken = default)
        {
            try { return await _reservationRepository.GetReservationsAsync(request, cancellationToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error getting reservation list"); throw; }
        }

        public async Task<List<ReservationStatusDto>> GetReservationStatusesAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetReservationStatusesAsync(cancellationToken);
            return entities.Select(x => new ReservationStatusDto { StatusId = x.ValueId, StatusName = x.ValueName, StatusCode = x.ValueCode }).ToList();
        }

        public async Task<ReservationDetailDto> GetReservationDetailAsync(long reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);
            if (reservation == null) throw new KeyNotFoundException($"Reservation with ID {reservationId} not found.");

            return new ReservationDetailDto
            {
                ReservationId = reservation.ReservationId,
                CustomerName = reservation.CustomerName,
                Phone = reservation.Phone,
                Email = reservation.Email,
                PartySize = reservation.PartySize,
                ReservedTime = DateTime.SpecifyKind(reservation.ReservedTime, DateTimeKind.Utc),
                CreatedAt = reservation.CreatedAt,
                StatusId = reservation.ReservationStatusLvId,
                StatusName = reservation.ReservationStatusLv.ValueName,
                StatusCode = reservation.ReservationStatusLv.ValueCode,
                SourceId = reservation.SourceLvId,
                SourceName = reservation.SourceLv.ValueName,
                SourceCode = reservation.SourceLv.ValueCode,
                Tables = reservation.Tables.Select(t => new ReservationTableDto
                {
                    TableId = t.TableId,
                    TableCode = t.TableCode,
                    Capacity = t.Capacity,
                    TableType = t.TableTypeLv.ValueName,
                    Zone = t.ZoneLv.ValueName
                }).ToList()
            };
        }

        public async Task<List<ManualTableOptionDto>> GetManualAvailableTablesAsync(
            DateTime? reservedTime,
            int? partySize,
            CancellationToken ct = default)
        {
            var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
            var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

            var tables = await _tableRepository.GetManualAvailableTablesAsync(ct);

            var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
            var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);
            var completedStatusId = await ReservationStatusCode.COMPLETED.ToReservationStatusIdAsync(_lookupResolver, ct);

            var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
            var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

            var availableTables = new List<ManualTableAvailabilityDto>();
            var now = DateTime.UtcNow;

            foreach (var table in tables)
            {
                var isAvailable = true;

                if (reservedTime.HasValue)
                {
                    var timeDiff = (reservedTime.Value - now).TotalMinutes;

                    if (timeDiff >= 0 && timeDiff <= immediateWindow)
                    {
                        if (table.TableStatusLvId == occupiedTableStatusId || table.TableStatusLvId == reservedTableStatusId)
                        {
                            isAvailable = false;
                        }
                    }
                }

                if (isAvailable && reservedTime.HasValue)
                {
                    var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                        table.TableId, reservedTime.Value, duration, cancelledStatusId, noShowStatusId, completedStatusId, ct);

                    if (conflicts.Any())
                    {
                        isAvailable = false;
                    }
                }

                if (isAvailable)
                {
                    availableTables.Add(new ManualTableAvailabilityDto
                    {
                        TableId = table.TableId,
                        TableCode = table.TableCode,
                        Capacity = table.Capacity,
                        TableType = table.TableTypeLv?.ValueName ?? TableTypeCode.NORMAL.ToString(),
                        Zone = table.ZoneLv?.ValueName ?? TableZoneCode.INDOOR.ToString(),
                    });
                }
            }

            if (!partySize.HasValue || partySize.Value <= 0)
            {
                return availableTables
                    .OrderBy(x => x.TableCode, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new ManualTableOptionDto
                    {
                        OptionId = x.TableId.ToString(),
                        TableIds = new List<long> { x.TableId },
                        TableCodes = x.TableCode,
                        Zone = x.Zone,
                        TotalCapacity = x.Capacity,
                        ExcessCapacity = 0,
                        TableCount = 1,
                        IsBestFit = false,
                    })
                    .ToList();
            }

            var availabilityPool = availableTables.Select(x => new TableAvailabilityDto
            {
                TableId = x.TableId,
                TableCode = x.TableCode,
                Capacity = x.Capacity,
                TableType = x.TableType,
                Zone = x.Zone,
                IsAvailable = true,
            }).ToList();

            var party = partySize.Value;
            var optionMap = new Dictionary<string, ManualTableOptionDto>(StringComparer.Ordinal);

            void AddOption(List<TableAvailabilityDto> optionTables)
            {
                if (optionTables.Count == 0)
                {
                    return;
                }

                var totalCapacity = optionTables.Sum(x => x.Capacity);
                if (totalCapacity < party)
                {
                    return;
                }

                var sortedById = optionTables
                    .Select(x => x.TableId)
                    .OrderBy(x => x)
                    .ToList();

                var key = string.Join("-", sortedById);
                if (optionMap.ContainsKey(key))
                {
                    return;
                }

                var distinctZones = optionTables
                    .Select(x => x.Zone)
                    .Where(z => !string.IsNullOrWhiteSpace(z))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                optionMap[key] = new ManualTableOptionDto
                {
                    OptionId = key,
                    TableIds = sortedById,
                    TableCodes = string.Join(", ", optionTables
                        .Select(x => x.TableCode)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
                    Zone = distinctZones.Count == 1 ? distinctZones[0] : "MIXED",
                    TotalCapacity = totalCapacity,
                    ExcessCapacity = totalCapacity - party,
                    TableCount = optionTables.Count,
                    IsBestFit = false,
                };
            }

            foreach (var single in availabilityPool.Where(x => x.Capacity >= party))
            {
                AddOption(new List<TableAvailabilityDto> { single });
            }

            foreach (var zoneGroup in availabilityPool.GroupBy(x => x.Zone))
            {
                var sortedByOrder = zoneGroup
                    .OrderBy(x => ParseTableOrder(x.TableCode))
                    .ThenBy(x => x.TableCode)
                    .ToList();

                for (var i = 0; i < sortedByOrder.Count; i++)
                {
                    var pick = new List<TableAvailabilityDto> { sortedByOrder[i] };
                    var sum = sortedByOrder[i].Capacity;
                    var prevOrder = ParseTableOrder(sortedByOrder[i].TableCode);

                    if (sum >= party)
                    {
                        AddOption(pick);
                        continue;
                    }

                    for (var j = i + 1; j < sortedByOrder.Count; j++)
                    {
                        var currentOrder = ParseTableOrder(sortedByOrder[j].TableCode);
                        if (currentOrder - prevOrder > 1)
                        {
                            break;
                        }

                        pick.Add(sortedByOrder[j]);
                        sum += sortedByOrder[j].Capacity;
                        prevOrder = currentOrder;

                        if (sum >= party)
                        {
                            AddOption(new List<TableAvailabilityDto>(pick));
                            break;
                        }
                    }
                }

                var bestFitInZone = ReservationTableSelectionUtil.SelectBestFitTablesWithinZone(sortedByOrder, party);
                if (bestFitInZone.Count > 0)
                {
                    AddOption(bestFitInZone);
                }
            }

            if (optionMap.Count == 0)
            {
                return new List<ManualTableOptionDto>();
            }

            var bestOption = optionMap.Values
                .OrderBy(x => x.ExcessCapacity)
                .ThenBy(x => x.TableCount)
                .ThenBy(x => x.TableCodes, StringComparer.OrdinalIgnoreCase)
                .First();

            bestOption.IsBestFit = true;

            return optionMap.Values
                .OrderByDescending(x => x.IsBestFit)
                .ThenBy(x => x.ExcessCapacity)
                .ThenBy(x => x.TableCount)
                .ThenBy(x => x.TableCodes, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<ReservationResponseDto> CreateManualReservationAsync(
            CreateManualReservationRequest request,
            CancellationToken ct = default)
        {
            var selectedTableIds = (request.TableIds ?? new List<long>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (selectedTableIds.Count == 0 && request.TableId.HasValue && request.TableId.Value > 0)
            {
                selectedTableIds.Add(request.TableId.Value);
            }

            if (selectedTableIds.Count == 0)
            {
                throw new InvalidOperationException("At least one table must be selected.");
            }

            var selectedTables = new List<RestaurantTable>();
            foreach (var tableId in selectedTableIds)
            {
                var table = await _tableRepository.GetByIdAsync(tableId, ct);
                if (table == null)
                {
                    throw new KeyNotFoundException($"Table with ID {tableId} not found.");
                }

                selectedTables.Add(table);
            }

            var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
            var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

            var now = DateTime.UtcNow;

            var lockedTableStatusId = await TableStatusCode.LOCKED.ToTableStatusIdAsync(_lookupResolver, ct);
            var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
            var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

            var timeDiff = (request.ReservedTime - now).TotalMinutes;
            foreach (var table in selectedTables)
            {
                if (table.TableStatusLvId == lockedTableStatusId)
                {
                    throw new InvalidOperationException($"Table {table.TableCode} is under maintenance.");
                }

                if (timeDiff >= 0 && timeDiff <= immediateWindow)
                {
                    if (table.TableStatusLvId == occupiedTableStatusId || table.TableStatusLvId == reservedTableStatusId)
                    {
                        throw new InvalidOperationException($"Table {table.TableCode} is currently occupied or reserved.");
                    }
                }
            }

            var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
            var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);
            var completedStatusId = await ReservationStatusCode.COMPLETED.ToReservationStatusIdAsync(_lookupResolver, ct);

            foreach (var table in selectedTables)
            {
                var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                    table.TableId, request.ReservedTime, duration, cancelledStatusId, noShowStatusId, completedStatusId, ct);
                if (conflicts.Any())
                {
                    throw new InvalidOperationException(
                        $"Table {table.TableCode} already has a reservation around the requested time.");
                }
            }

            var totalCapacity = selectedTables.Sum(x => x.Capacity);
            if (totalCapacity < request.PartySize)
            {
                throw new InvalidOperationException("Selected tables do not have enough capacity for this party size.");
            }

            var phoneSourceId = await ReservationSourceCode.PHONE.IdAsync(
                _lookupResolver,
                (ushort)Enum.LookupType.ReservationSource,
                ct);
            var walkInSourceId = await ReservationSourceCode.WALK_IN.IdAsync(
                _lookupResolver,
                (ushort)Enum.LookupType.ReservationSource,
                ct);
            var confirmedStatusId = await ReservationStatusCode.CONFIRMED.ToReservationStatusIdAsync(_lookupResolver, ct);
            var checkedInStatusId = await ReservationStatusCode.CHECKED_IN.ToReservationStatusIdAsync(_lookupResolver, ct);

            await _uow.BeginTransactionAsync(ct);
            try
            {
                long customerId;
                if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
                {
                    var existingCustomer = await _customerService.GetByIdAsync(request.CustomerId.Value, ct);
                    if (existingCustomer != null)
                    {
                        customerId = request.CustomerId.Value;
                    }
                    else
                    {
                        customerId = await _customerService.FindOrCreateCustomerIdAsync(request.Phone, request.CustomerName, request.Email, ct);
                    }
                }
                else
                {
                    customerId = await _customerService.FindOrCreateCustomerIdAsync(request.Phone, request.CustomerName, request.Email, ct);
                }

            var reservation = new Reservation
            {
                CustomerId = customerId,
                CustomerName = request.CustomerName,
                Phone = request.Phone,
                Email = request.Email,
                PartySize = request.PartySize,
                ReservedTime = request.ReservedTime,
                CreatedAt = DateTime.UtcNow,
                SourceLvId = string.Equals(request.Source, nameof(ReservationSourceCode.PHONE), StringComparison.OrdinalIgnoreCase)
                    ? phoneSourceId : walkInSourceId,
                ReservationStatusLvId = string.Equals(request.Status, nameof(ReservationStatusCode.CONFIRMED), StringComparison.OrdinalIgnoreCase)
                    ? confirmedStatusId : checkedInStatusId,
                Tables = selectedTables
            };

            var created = await _reservationRepository.CreateAsync(reservation, ct);

            var tableCodes_log = string.Join(", ", selectedTables.Select(x => x.TableCode));

            await _notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.RESERVATION_CREATED),
                Title = "New Reservation",
                Body = $"Reservation #{created.ReservationId} for {request.CustomerName} ({request.PartySize} guests) at {tableCodes_log}",
                Priority = nameof(NotificationPriority.Normal),
                SoundKey = "notification_normal",
                ActionUrl = $"/dashboard/reservations/{created.ReservationId}",
                EntityType = "Reservation",
                EntityId = created.ReservationId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["reservationId"] = created.ReservationId.ToString(),
                    ["customerName"] = request.CustomerName,
                    ["partySize"] = request.PartySize.ToString(),
                    ["tableCode"] = tableCodes_log,
                    ["reservedTime"] = request.ReservedTime.ToString("yyyy-MM-dd HH:mm")
                },
                TargetPermissions = new List<string> { Permissions.ViewReservation }
            });

            _logger.LogInformation(
                "Reservation {ReservationId} created for {CustomerName} at tables {TableCodes}",
                created.ReservationId,
                request.CustomerName,
                string.Join(", ", selectedTables.Select(x => x.TableCode)));

            var tableCodes = string.Join(", ", selectedTables
                .Select(x => x.TableCode)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

            // Send confirmation email (inside transaction to ensure connection lives)
            if (!string.IsNullOrWhiteSpace(created.Email))
            {
                await SendReservationConfirmationEmailAsync(created, tableCodes);
            }

            await _uow.CommitAsync(ct);

            var zones = selectedTables
                .Select(x => x.ZoneLv?.ValueName ?? TableZoneCode.INDOOR.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ReservationResponseDto
            {
                ReservationId = created.ReservationId,
                CustomerName = created.CustomerName,
                Phone = created.Phone,
                Email = created.Email,
                PartySize = created.PartySize,
                ReservedTime = DateTime.SpecifyKind(created.ReservedTime, DateTimeKind.Utc),
                TableCode = tableCodes,
                Zone = zones.Count > 0 ? string.Join(", ", zones) : string.Empty,
                Status = request.Status,
                CreatedAt = created.CreatedAt ?? DateTime.UtcNow
            };
            }
            catch (Exception)
            {
                await _uow.RollbackAsync(ct);
                throw;
            }
        }

        // 1. CONFIRM ĐƠN (BÀN ĐÃ ĐƯỢC GÁN Ở PUBLIC)
        public async Task AssignTableAndConfirmAsync(long reservationId, List<long> tableIds, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");
            if (reservation.Tables == null || !reservation.Tables.Any())
                throw new InvalidOperationException("Đơn chưa có bàn được gán. Vui lòng chỉnh sửa đơn trước khi duyệt.");

            var confirmedStatusId = await ReservationStatusCode.CONFIRMED.ToReservationStatusIdAsync(_lookupResolver, cancellationToken);
            var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

            await _uow.BeginTransactionAsync(cancellationToken);
            try
            {
                reservation.ReservationStatusLvId = confirmedStatusId;

                // [ KHÓA BÀN TRƯỚC 2 TIẾNG]
                var lockTime = reservation.ReservedTime.AddHours(-2);
                var timeUntilLock = lockTime - DateTime.UtcNow;

                if (timeUntilLock <= TimeSpan.Zero)
                {
                    // Nếu thời gian đến lúc ăn < 2 tiếng -> Khóa bàn (RESERVED) ngay lập tức
                    foreach (var tableId in reservation.Tables.Select(t => t.TableId))
                    {
                        await _tableRepository.UpdateStatusAsync(tableId, reservedTableStatusId, cancellationToken);
                        await _realtimeNotification.NotifyTableStatusChangedAsync(tableId, "RESERVED");
                        await _notificationService.PublishAsync(new PublishNotificationRequest
                        {
                            Type = nameof(NotificationType.TABLE_STATUS_CHANGED),
                            Title = "Table Status Changed",
                            Priority = nameof(NotificationPriority.Low),
                            SoundKey = "notification_low",
                            ActionUrl = "/dashboard/tables",
                            EntityType = "Table",
                            EntityId = tableId.ToString(),
                            Metadata = new Dictionary<string, object>
                            {
                                ["tableId"] = tableId.ToString(),
                                ["newStatus"] = "RESERVED"
                            },
                            TargetPermissions = new List<string> { Permissions.ViewTable }
                        }, cancellationToken);
                    }
                }
                else
                {
                    // Nếu thời gian đến lúc ăn còn > 2 tiếng -> Lập lịch khóa bàn sau khoảng thời gian tính được
                    _jobScheduler.ScheduleTableLock(reservation.ReservationId, timeUntilLock);
                }

                await _uow.SaveChangesAsync(cancellationToken);

                // Lập lịch Hangfire đếm ngược 15p No-Show
                TimeSpan delay = reservation.ReservedTime.AddMinutes(15) > DateTime.UtcNow
                    ? reservation.ReservedTime.AddMinutes(15) - DateTime.UtcNow
                    : TimeSpan.FromMinutes(1);
                _jobScheduler.ScheduleNoShowCheck(reservation.ReservationId, delay);

                await _uow.CommitAsync(cancellationToken);

                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, "CONFIRMED");
                await _notificationService.PublishAsync(new PublishNotificationRequest
                {
                    Type = nameof(NotificationType.RESERVATION_STATUS_CHANGED),
                    Title = "Reservation Confirmed",
                    Body = $"Reservation #{reservationId} has been confirmed",
                    Priority = nameof(NotificationPriority.Normal),
                    SoundKey = "notification_normal",
                    ActionUrl = $"/dashboard/reservations/{reservationId}",
                    EntityType = "Reservation",
                    EntityId = reservationId.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["reservationId"] = reservationId.ToString(),
                        ["newStatus"] = "CONFIRMED"
                    },
                    TargetPermissions = new List<string> { Permissions.ViewReservation }
                }, cancellationToken);
            }
            catch (Exception)
            {
                await _uow.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // 2. CẬP NHẬT TRẠNG THÁI (Lúc Khách Đến hoặc Khách Hủy)
        public async Task UpdateReservationStatusAsync(long reservationId, string newStatusCode, string? note = null, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            var newStatusId = await _lookupResolver.GetIdAsync((ushort)Enum.LookupType.ReservationStatus, newStatusCode, cancellationToken);

            await _uow.BeginTransactionAsync(cancellationToken);
            try
            {
                reservation.ReservationStatusLvId = newStatusId;

                if (!string.IsNullOrWhiteSpace(note))
                {
                    reservation.Notes = string.IsNullOrWhiteSpace(reservation.Notes) ? note.Trim() : $"{reservation.Notes} | {note.Trim()}";
                }

                if (reservation.Tables != null && reservation.Tables.Any())
                {
                    uint targetTableStatusId = 0;
                    string targetTableStatusCode = "";

                    if (newStatusCode == ReservationStatusCode.CHECKED_IN.ToString())
                    {
                        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

                        foreach (var table in reservation.Tables)
                        {
                            var currentTable = await _tableRepository.GetByIdAsync(table.TableId, cancellationToken);
                            if (currentTable != null && currentTable.TableStatusLvId == occupiedTableStatusId)
                            {
                                throw new InvalidOperationException($"Bàn {currentTable.TableCode} đã được xếp cho khách khác. Vui lòng gỡ bàn cũ và xếp bàn mới cho đơn này!");
                            }
                        }

                        targetTableStatusId = occupiedTableStatusId;
                        targetTableStatusCode = TableStatusCode.OCCUPIED.ToString();
                    }
                    else if (newStatusCode == ReservationStatusCode.CANCELLED.ToString() ||
                             newStatusCode == ReservationStatusCode.COMPLETED.ToString() ||
                             newStatusCode == ReservationStatusCode.NO_SHOW.ToString())
                    {
                        targetTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
                        targetTableStatusCode = TableStatusCode.AVAILABLE.ToString();
                    }
                    else if (newStatusCode == ReservationStatusCode.CONFIRMED.ToString())
                    {
                        // [ KHÓA BÀN TRƯỚC 2 TIẾNG CHO ĐƠN ONLINE]
                        var lockTime = reservation.ReservedTime.AddHours(-2);
                        var timeUntilLock = lockTime - DateTime.UtcNow;

                        if (timeUntilLock <= TimeSpan.Zero)
                        {
                            targetTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
                            targetTableStatusCode = TableStatusCode.RESERVED.ToString();
                        }
                        else
                        {
                            _jobScheduler.ScheduleTableLock(reservationId, timeUntilLock);
                            targetTableStatusId = 0; 
                        }

                        // Lập lịch Hangfire đếm ngược 15p No-Show
                        TimeSpan delay = reservation.ReservedTime.AddMinutes(15) > DateTime.UtcNow
                            ? reservation.ReservedTime.AddMinutes(15) - DateTime.UtcNow
                            : TimeSpan.FromMinutes(1);
                        _jobScheduler.ScheduleNoShowCheck(reservation.ReservationId, delay);
                    }

                    if (targetTableStatusId != 0)
                    {
                        foreach (var table in reservation.Tables)
                        {
                            await _tableRepository.UpdateStatusAsync(table.TableId, targetTableStatusId, cancellationToken);
                            await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, targetTableStatusCode);
                            await _notificationService.PublishAsync(new PublishNotificationRequest
                            {
                                Type = nameof(NotificationType.TABLE_STATUS_CHANGED),
                                Title = "Table Status Changed",
                                Priority = nameof(NotificationPriority.Low),
                                SoundKey = "notification_low",
                                ActionUrl = "/dashboard/tables",
                                EntityType = "Table",
                                EntityId = table.TableId.ToString(),
                                Metadata = new Dictionary<string, object>
                                {
                                    ["tableId"] = table.TableId.ToString(),
                                    ["tableCode"] = table.TableCode,
                                    ["newStatus"] = targetTableStatusCode
                                },
                                TargetPermissions = new List<string> { Permissions.ViewTable }
                            }, cancellationToken);
                        }
                    }
                }

                await _uow.SaveChangesAsync(cancellationToken);
                await _uow.CommitAsync(cancellationToken);

                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, newStatusCode);
                await _notificationService.PublishAsync(new PublishNotificationRequest
                {
                    Type = nameof(NotificationType.RESERVATION_STATUS_CHANGED),
                    Title = "Reservation Status Changed",
                    Body = $"Reservation #{reservationId} status changed to {newStatusCode}",
                    Priority = nameof(NotificationPriority.Normal),
                    SoundKey = "notification_normal",
                    ActionUrl = $"/dashboard/reservations/{reservationId}",
                    EntityType = "Reservation",
                    EntityId = reservationId.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["reservationId"] = reservationId.ToString(),
                        ["newStatus"] = newStatusCode
                    },
                    TargetPermissions = new List<string> { Permissions.ViewReservation }
                }, cancellationToken);
            }
            catch (Exception)
            {
                await _uow.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<ReservationStatusResponseDTO> UpdateReservationStatusAsync(
            long reservationId,
            long staffId,
            UpdateReservationStatusRequest request,
            CancellationToken ct)
        {
            var reservation = await _reservationRepository
                .GetByIdWithTablesAsync(reservationId, ct)
                ?? throw new Exception("Reservation not found");

            var currentStatus = System.Enum.Parse<ReservationStatusCode>(
                reservation.ReservationStatusLv.ValueCode);

            ValidateStatusTransition(currentStatus, request.Status);

            await _uow.BeginTransactionAsync(ct);

            try
            {
                long? createdOrderId = null;

                if (request.Status == ReservationStatusCode.CHECKED_IN)
                {
                    createdOrderId = await HandleCheckIn(
                        reservation,
                        staffId,
                        ct);
                }

                var statusId = await _lookupResolver.GetIdAsync(
                    (ushort)Enum.LookupType.ReservationStatus,
                    request.Status,
                    ct);

                reservation.ReservationStatusLvId = statusId;

                await _reservationRepository.UpdateAsync(reservation, ct);

                await _uow.CommitAsync(ct);

                return new ReservationStatusResponseDTO
                {
                    ReservationId = reservation.ReservationId,
                    Status = request.Status,
                    CreatedOrderId = createdOrderId
                };
            }
            catch
            {
                await _uow.RollbackAsync(ct);
                throw;
            }
        }

        // 3. HANGFIRE JOB: ĐÁNH DẤU NO-SHOW (Sau 15 phút)
        [AutomaticRetry(Attempts = 2)]
        public async Task CheckAndMarkNoShowAsync(long reservationId)
        {
            try
            {
                var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, CancellationToken.None);
                if (reservation == null || reservation.ReservationStatusLv.ValueCode != "CONFIRMED") return;

                var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, CancellationToken.None);
                var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, CancellationToken.None);

                await _uow.BeginTransactionAsync(CancellationToken.None);

                reservation.ReservationStatusLvId = noShowStatusId;

                if (reservation.Tables != null)
                {
                    foreach (var table in reservation.Tables)
                    {
                        await _tableRepository.UpdateStatusAsync(table.TableId, availableTableStatusId, CancellationToken.None);
                    }
                }

                await _uow.SaveChangesAsync(CancellationToken.None);
                await _uow.CommitAsync(CancellationToken.None);

                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, "NO_SHOW");
                await _notificationService.PublishAsync(new PublishNotificationRequest
                {
                    Type = nameof(NotificationType.RESERVATION_STATUS_CHANGED),
                    Title = "Reservation No-Show",
                    Body = $"Reservation #{reservationId} marked as no-show",
                    Priority = nameof(NotificationPriority.Normal),
                    SoundKey = "notification_normal",
                    ActionUrl = $"/dashboard/reservations/{reservationId}",
                    EntityType = "Reservation",
                    EntityId = reservationId.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["reservationId"] = reservationId.ToString(),
                        ["newStatus"] = "NO_SHOW"
                    },
                    TargetPermissions = new List<string> { Permissions.ViewReservation }
                });
                if (reservation.Tables != null)
                {
                    foreach (var table in reservation.Tables)
                    {
                        await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, "AVAILABLE");
                        await _notificationService.PublishAsync(new PublishNotificationRequest
                        {
                            Type = nameof(NotificationType.TABLE_STATUS_CHANGED),
                            Title = "Table Status Changed",
                            Priority = nameof(NotificationPriority.Low),
                            SoundKey = "notification_low",
                            ActionUrl = "/dashboard/tables",
                            EntityType = "Table",
                            EntityId = table.TableId.ToString(),
                            Metadata = new Dictionary<string, object>
                            {
                                ["tableId"] = table.TableId.ToString(),
                                ["tableCode"] = table.TableCode,
                                ["newStatus"] = "AVAILABLE"
                            },
                            TargetPermissions = new List<string> { Permissions.ViewTable }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync(CancellationToken.None);
                _logger.LogError(ex, "Lỗi khi chạy Hangfire Job No-Show cho Reservation {Id}", reservationId);
                throw;
            }
        }

        // 4. HANGFIRE JOB: TỰ ĐỘNG KHÓA BÀN (Trước 2 tiếng)
        [AutomaticRetry(Attempts = 2)]
        public async Task LockTablesForReservationAsync(long reservationId)
        {
            try
            {
                var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, CancellationToken.None);

                // Chỉ khóa khi đơn vẫn còn giữ trạng thái CONFIRMED và giờ hiện tại thực sự đã đến gần (< 2 tiếng 5 phút để bù trừ delay)
                if (reservation == null || reservation.ReservationStatusLv.ValueCode != "CONFIRMED") return;

                if (reservation.ReservedTime.AddHours(-2) > DateTime.UtcNow.AddMinutes(5)) return;

                var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, CancellationToken.None);
                var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, CancellationToken.None);

                await _uow.BeginTransactionAsync(CancellationToken.None);

                if (reservation.Tables != null)
                {
                    foreach (var table in reservation.Tables)
                    {
                        // Lấy trạng thái hiện tại của bàn dưới DB
                        var currentTable = await _tableRepository.GetByIdAsync(table.TableId, CancellationToken.None);

                        // Tránh tình trạng nhét đè lên bàn đang OCCUPIED của khách Walk-in chưa ăn xong.
                        if (currentTable != null && currentTable.TableStatusLvId == availableTableStatusId)
                        {
                            await _tableRepository.UpdateStatusAsync(table.TableId, reservedTableStatusId, CancellationToken.None);
                            await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, "RESERVED");
                            await _notificationService.PublishAsync(new PublishNotificationRequest
                            {
                                Type = nameof(NotificationType.TABLE_STATUS_CHANGED),
                                Title = "Table Status Changed",
                                Priority = nameof(NotificationPriority.Low),
                                SoundKey = "notification_low",
                                ActionUrl = "/dashboard/tables",
                                EntityType = "Table",
                                EntityId = table.TableId.ToString(),
                                Metadata = new Dictionary<string, object>
                                {
                                    ["tableId"] = table.TableId.ToString(),
                                    ["newStatus"] = "RESERVED"
                                },
                                TargetPermissions = new List<string> { Permissions.ViewTable }
                            });
                        }
                    }
                }

                await _uow.SaveChangesAsync(CancellationToken.None);
                await _uow.CommitAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync(CancellationToken.None);
                _logger.LogError(ex, "Lỗi khi chạy Hangfire Job Lock Tables cho Reservation {Id}", reservationId);
                throw;
            }
        }

        public async Task UpdateReservationAsync(long id, UpdateReservationRequest request, CancellationToken ct = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(id, ct);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            var currentTableIds = reservation.Tables.Select(t => t.TableId).Distinct().OrderBy(x => x).ToList();
            var requestedTableIds = (request.TableIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var hasExplicitTableSelection = request.TableIds != null;
            var effectiveTableIds = hasExplicitTableSelection ? requestedTableIds : currentTableIds;

            if (effectiveTableIds.Count == 0)
                throw new InvalidOperationException("Reservation must have at least one table.");

            var isTimeChanged = request.ReservedTime != reservation.ReservedTime;
            var isPartySizeChanged = request.PartySize != reservation.PartySize;
            var isTableChanged = !effectiveTableIds.SequenceEqual(currentTableIds);
            var mustRevalidateTables = isTimeChanged || isPartySizeChanged || isTableChanged;

            await _uow.BeginTransactionAsync(ct);
            try
            {
                var validatedTables = reservation.Tables.ToList();

                if (mustRevalidateTables)
                {
                    validatedTables = await ValidateAndResolveTablesForUpdateAsync(
                        reservationId: reservation.ReservationId,
                        tableIds: effectiveTableIds,
                        reservedTime: request.ReservedTime,
                        partySize: request.PartySize,
                        ct: ct);
                }

                reservation.CustomerName = request.CustomerName;
                reservation.Phone = request.Phone;
                reservation.Email = request.Email;
                reservation.PartySize = request.PartySize;
                reservation.ReservedTime = request.ReservedTime;
                reservation.Notes = request.Notes;

                if (request.StatusId.HasValue && request.StatusId.Value != reservation.ReservationStatusLvId)
                {
                    reservation.ReservationStatusLvId = request.StatusId.Value;
                }

                if (mustRevalidateTables)
                {
                    var newTableIds = validatedTables.Select(t => t.TableId).ToList();

                    var tablesToRemove = reservation.Tables.Where(t => !newTableIds.Contains(t.TableId)).ToList();
                    var tableIdsToAdd = newTableIds.Where(tableId => !currentTableIds.Contains(tableId)).ToList();

                    var availableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, ct);

                    foreach (var table in tablesToRemove)
                    {
                        reservation.Tables.Remove(table);
                        await _tableRepository.UpdateStatusAsync(table.TableId, availableStatusId, ct);
                        await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, "AVAILABLE");
                    }

                    if (tableIdsToAdd.Any())
                    {
                        var reservationStatusCode = (await _reservationRepository.GetReservationStatusesAsync(ct))
                            .FirstOrDefault(s => s.ValueId == reservation.ReservationStatusLvId)?.ValueCode;

                        TableStatusCode targetTableStatus = TableStatusCode.RESERVED;
                        if (reservationStatusCode == ReservationStatusCode.CHECKED_IN.ToString())
                            targetTableStatus = TableStatusCode.OCCUPIED;

                        var targetTableStatusId = await targetTableStatus.ToTableStatusIdAsync(_lookupResolver, ct);

                        foreach (var tableId in tableIdsToAdd)
                        {
                            var table = validatedTables.FirstOrDefault(t => t.TableId == tableId)
                                ?? await _tableRepository.GetByIdAsync(tableId, ct);
                            if (table != null)
                            {
                                reservation.Tables.Add(table);

                                // Nếu thời gian còn > 2 tiếng thì chưa khóa bàn 
                                if (targetTableStatus == TableStatusCode.RESERVED && reservation.ReservedTime.AddHours(-2) > DateTime.UtcNow)
                                {
                                }
                                else
                                {
                                    await _tableRepository.UpdateStatusAsync(tableId, targetTableStatusId, ct);
                                    await _realtimeNotification.NotifyTableStatusChangedAsync(tableId, targetTableStatus.ToString());
                                }
                            }
                        }
                    }
                }

                await _reservationRepository.UpdateAsync(reservation, ct);
                await _uow.CommitAsync(ct);

                var status = (await _reservationRepository.GetReservationStatusesAsync(ct)).FirstOrDefault(s => s.ValueId == reservation.ReservationStatusLvId);
                if (status != null) await _realtimeNotification.NotifyReservationUpdatedAsync(id, status.ValueCode);
            }
            catch (Exception) { await _uow.RollbackAsync(ct); throw; }
        }

        private async Task<List<RestaurantTable>> ValidateAndResolveTablesForUpdateAsync(
            long reservationId,
            List<long> tableIds,
            DateTime reservedTime,
            int partySize,
            CancellationToken ct)
        {
            if (partySize <= 0)
                throw new InvalidOperationException("Party size must be greater than zero.");

            var selectedTables = new List<RestaurantTable>();
            foreach (var tableId in tableIds)
            {
                var table = await _tableRepository.GetByIdAsync(tableId, ct);
                if (table == null)
                    throw new KeyNotFoundException($"Table with ID {tableId} not found.");

                selectedTables.Add(table);
            }

            var totalCapacity = selectedTables.Sum(t => t.Capacity);
            if (totalCapacity < partySize)
                throw new InvalidOperationException("Selected tables do not have enough capacity for this party size.");

            var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
            var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

            var lockedTableStatusId = await TableStatusCode.LOCKED.ToTableStatusIdAsync(_lookupResolver, ct);
            var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
            var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

            var timeDiff = (reservedTime - DateTime.UtcNow).TotalMinutes;

            foreach (var table in selectedTables)
            {
                if (table.TableStatusLvId == lockedTableStatusId)
                    throw new InvalidOperationException($"Table {table.TableCode} is under maintenance.");

                if (timeDiff >= 0 && timeDiff <= immediateWindow)
                {
                    if (table.TableStatusLvId == occupiedTableStatusId || table.TableStatusLvId == reservedTableStatusId)
                        throw new InvalidOperationException($"Table {table.TableCode} is currently occupied or reserved.");
                }
            }

            var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
            var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);
            var completedStatusId = await ReservationStatusCode.COMPLETED.ToReservationStatusIdAsync(_lookupResolver, ct);

            foreach (var table in selectedTables)
            {
                var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                    table.TableId,
                    reservedTime,
                    duration,
                    cancelledStatusId,
                    noShowStatusId,
                    completedStatusId,
                    ct);

                if (conflicts.Any(x => x.ReservationId != reservationId))
                    throw new InvalidOperationException($"Table {table.TableCode} already has a reservation around the requested time.");
            }

            return selectedTables;
        }

        private void ValidateStatusTransition(
            ReservationStatusCode current,
            ReservationStatusCode target)
        {
            if (current == target)
                throw new InvalidOperationException("Reservation already in this status.");

            if (current == ReservationStatusCode.CANCELLED ||
                current == ReservationStatusCode.NO_SHOW)
                throw new InvalidOperationException("Reservation already closed.");
        }

        private void ValidateReservationTime(Reservation reservation)
        {
            var now = DateTime.UtcNow;

            var start = reservation.ReservedTime.AddMinutes(-30);
            var end = reservation.ReservedTime.AddMinutes(30);

            if (now < start || now > end)
                throw new InvalidOperationException(
                    "Check-in allowed only within reservation time window.");
        }

        private async Task<long> HandleCheckIn(
            Reservation reservation,
            long staffId,
            CancellationToken ct)
        {
            ValidateReservationTime(reservation);

            var table = reservation.Tables.FirstOrDefault()
                ?? throw new InvalidOperationException("Reservation has no table.");

            var existingOrder = await _orderRepository
                .GetActiveOrderByTableAsync(table.TableId, ct);

            if (existingOrder != null)
                throw new InvalidOperationException("Table already has active order.");

            var orderStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderStatus,
                OrderStatusCode.PENDING,
                ct);

            var sourceId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderSource,
                OrderSourceCode.DINE_IN,
                ct);

            var occupiedStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.TableStatus,
                TableStatusCode.OCCUPIED,
                ct);

            var order = new Order
            {
                StaffId = staffId,
                CustomerId = reservation.CustomerId ?? await _customerService.GetGuestCustomerIdAsync(ct),
                TableId = table.TableId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalAmount = 0,
                SourceLvId = sourceId,
                OrderStatusLvId = orderStatusId
            };

            await _orderRepository.AddAsync(order, ct);

            table.TableStatusLvId = occupiedStatusId;

            await _tableRepository.UpdateAsync(table, ct);

            return order.OrderId;
        }

        private static int ParseTableOrder(string tableCode)
        {
            var match = Regex.Match(tableCode, "(\\d+)(?!.*\\d)");
            if (!match.Success)
            {
                return int.MaxValue;
            }

            return int.TryParse(match.Value, out var value) ? value : int.MaxValue;
        }

        public async Task DeleteReservationAsync(long id, CancellationToken ct = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id, ct);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            await _uow.BeginTransactionAsync(ct);
            try
            {
                if (reservation.Tables != null && reservation.Tables.Any())
                {
                    var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, ct);
                    foreach (var table in reservation.Tables)
                    {
                        await _tableRepository.UpdateStatusAsync(table.TableId, availableTableStatusId, ct);
                        await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, TableStatusCode.AVAILABLE.ToString());
                    }
                }

                await _reservationRepository.DeleteAsync(reservation, ct);
                await _uow.CommitAsync(ct);
            }
            catch (Exception) { await _uow.RollbackAsync(ct); throw; }
        }

        private async Task SendReservationConfirmationEmailAsync(Reservation reservation, string tableCodes)
        {
            try
            {
                var template = await _emailTemplateService.GetByCodeAsync(TemplateCodeReservationConfirmation);
                if (template == null)
                {
                    _logger.LogWarning("Email template {TemplateCode} not found. Skipping email.", TemplateCodeReservationConfirmation);
                    return;
                }

                var body = template.BodyHtml
                    .Replace("{{CustomerName}}", reservation.CustomerName)
                    .Replace("{{ReservedTime}}", reservation.ReservedTime.ToString("dd/MM/yyyy HH:mm"))
                    .Replace("{{PartySize}}", reservation.PartySize.ToString())
                    .Replace("{{TableCode}}", tableCodes)
                    .Replace("{{TableCodes}}", tableCodes)
                    .Replace("{{ReservationId}}", reservation.ReservationId.ToString());

                var queuedEmail = new QueuedEmail(
                    To: reservation.Email!,
                    Subject: template.Subject,
                    HtmlBody: body,
                    CorrelationId: $"Res-{reservation.ReservationId}"
                );

                await _emailQueue.EnqueueAsync(queuedEmail);
                _logger.LogInformation("Enqueued confirmation email for manual reservation {ReservationId} to {Email}", reservation.ReservationId, reservation.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing confirmation email for manual reservation {ReservationId}", reservation.ReservationId);
            }
        }
    }
}