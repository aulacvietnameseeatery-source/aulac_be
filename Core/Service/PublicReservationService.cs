using Core.DTO.Reservation;
using Core.Entity;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Others;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Core.Service;

/// <summary>
/// Service implementation for public reservation operations.
/// Uses Redis cache for soft-lock mechanism.
/// </summary>
public class PublicReservationService : IPublicReservationService
{
    private const long GuestCustomerId = 68;

    private readonly ITableRepository _tableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationBroadcastService _broadcastService;
    private readonly ILogger<PublicReservationService> _logger;
    private readonly ILookupResolver _lookupResolver;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ICustomerService _customerService;

    private const string SettingReservationDuration = "reservation.default_duration_minutes";
    private const string SettingImmediateWindow = "reservation.immediate_window_minutes";

    public PublicReservationService(
        ITableRepository tableRepository,
        IReservationRepository reservationRepository,
        IReservationBroadcastService broadcastService,
        ILogger<PublicReservationService> logger,
        ILookupResolver lookupResolver,
        IUnitOfWork uow,
        IOrderRepository orderRepository,
        ISystemSettingService systemSettingService,
        ICustomerService customerService)
    {
        _tableRepository = tableRepository;
        _reservationRepository = reservationRepository;
        _broadcastService = broadcastService;
        _logger = logger;
        _lookupResolver = lookupResolver;
        _uow = uow;
        _orderRepository = orderRepository;
        _systemSettingService = systemSettingService;
        _customerService = customerService;
    }

    public async Task<ReservationFitCheckResponse> CheckReservationFitAsync(
        ReservationFitCheckRequest request,
        CancellationToken ct = default)
    {
        if (request.PartySize <= 0)
        {
            return new ReservationFitCheckResponse
            {
                CanBookOnline = false,
                Message = "Party size is invalid."
            };
        }

        var candidates = await FindCandidateTablesAsync(request.ReservedTime, request.PartySize, ct);

        return new ReservationFitCheckResponse
        {
            CanBookOnline = candidates.Count > 0,
            Message = candidates.Count > 0
                ? "Tables can be arranged online."
                : "No suitable online table arrangement found at this time."
        };
    }

    public async Task<List<TableAvailabilityDto>> GetAvailableTablesAsync(
        DateTime? reservedTime,
        int? partySize,
        string? zone,
        CancellationToken ct = default)
    {
        // Get configurations
        var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
        var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

        // Get all non-maintenance tables
        var tables = await _tableRepository.GetAvailableTablesAsync(ct);

        // Fetch all required status IDs via _lookupResolver extension methods
        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);

        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
        var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

        // Filter by party size if specified
        if (partySize.HasValue)
        {
            tables = tables.Where(t => t.Capacity >= partySize.Value).ToList();
        }

        // Filter by zone if specified and not "All"
        if (!string.IsNullOrEmpty(zone) && !zone.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            tables = tables.Where(t => t.ZoneLv?.ValueName?.Equals(zone, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        var result = new List<TableAvailabilityDto>();
        var now = DateTime.UtcNow;

        foreach (var table in tables)
        {
            var isAvailable = true;

            // 1. Check for real-time occupancy if reservedTime is "near-now"
            if (reservedTime.HasValue)
            {
                var timeDiff = (reservedTime.Value - now).TotalMinutes;

                if (timeDiff >= 0 && timeDiff <= immediateWindow)
                {
                    // If searching for "now", don't allow tables that are physically occupied or reserved manually
                    if (table.TableStatusLvId == occupiedTableStatusId || table.TableStatusLvId == reservedTableStatusId)
                    {
                        isAvailable = false;
                    }
                }
            }

            // 2. Check for existing reservation conflicts (overlap check)
            if (isAvailable && reservedTime.HasValue)
            {
                var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                    table.TableId, reservedTime.Value, duration, cancelledStatusId, noShowStatusId, ct);
                
                if (conflicts.Any())
                {
                    isAvailable = false;
                }
            }

            result.Add(new TableAvailabilityDto
            {
                TableId = table.TableId,
                TableCode = table.TableCode,
                Capacity = table.Capacity,
                TableType = table.TableTypeLv?.ValueName ?? TableTypeCode.NORMAL.ToString(),
                Zone = table.ZoneLv?.ValueName ?? TableZoneCode.INDOOR.ToString(),
                IsAvailable = isAvailable,
                LockedUntil = null,
                ImageUrl = table.TableMedia?.FirstOrDefault()?.Media?.Url
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ReservationResponseDto> CreateReservationAsync(
        CreateReservationRequest request,
        CancellationToken ct = default)
    {
        var candidates = await FindCandidateTablesAsync(request.ReservedTime, request.PartySize, ct);
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Hiện không còn bàn online phù hợp cho số lượng khách và thời gian bạn chọn.");
        }

        var onlineSourceId = await ReservationSourceCode.ONLINE.IdAsync(
            _lookupResolver,
            (ushort)Core.Enum.LookupType.ReservationSource,
            ct);
        var pendingStatusId = await ReservationStatusCode.PENDING.ToReservationStatusIdAsync(_lookupResolver, ct);
        var customerId = await _customerService.FindOrCreateCustomerIdAsync(
            request.Phone,
            request.CustomerName,
            request.Email,
            ct);

        var selectedTables = new List<RestaurantTable>();
        foreach (var candidate in candidates)
        {
            var table = await _tableRepository.GetByIdAsync(candidate.TableId, ct);
            if (table == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bàn với ID {candidate.TableId}.");
            }

            selectedTables.Add(table);
        }

        var reservation = new Reservation
        {
            CustomerId = customerId,
            CustomerName = request.CustomerName,
            Phone = request.Phone,
            Email = request.Email,
            PartySize = request.PartySize,
            ReservedTime = request.ReservedTime,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.UtcNow,
            SourceLvId = onlineSourceId,
            ReservationStatusLvId = pendingStatusId, // Pending
        };

        foreach (var table in selectedTables)
        {
            reservation.Tables.Add(table);
        }

        var created = await _reservationRepository.CreateAsync(reservation, ct);

        var tableCodes = string.Join(", ", candidates.Select(x => x.TableCode));
        var zones = candidates
            .Select(x => x.Zone)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation(
            "Reservation {ReservationId} created for {CustomerName} in PENDING status with tables: {TableCodes}",
            created.ReservationId, request.CustomerName, tableCodes);

        return new ReservationResponseDto
        {
            ReservationId = created.ReservationId,
            CustomerName = created.CustomerName,
            Phone = created.Phone,
            Email = created.Email,
            PartySize = created.PartySize,
            ReservedTime = created.ReservedTime,
            TableCode = tableCodes,
            Zone = zones.Count > 0 ? string.Join(", ", zones) : string.Empty,
            Status = ReservationStatusCode.PENDING.ToString(),
            CreatedAt = created.CreatedAt ?? DateTime.UtcNow
        };
    }

    private async Task<List<TableAvailabilityDto>> FindCandidateTablesAsync(
        DateTime reservedTime,
        int partySize,
        CancellationToken ct)
    {
        var available = await GetAvailableTablesAsync(reservedTime, null, null, ct);
        var candidates = available.Where(x => x.IsAvailable).ToList();

        var single = candidates
            .Where(x => x.Capacity >= partySize)
            .OrderBy(x => x.Capacity)
            .ThenBy(x => x.TableCode)
            .FirstOrDefault();

        if (single != null)
        {
            return new List<TableAvailabilityDto> { single };
        }

        foreach (var zoneGroup in candidates.GroupBy(x => x.Zone))
        {
            var sorted = zoneGroup
                .OrderBy(x => ParseTableOrder(x.TableCode))
                .ThenBy(x => x.TableCode)
                .ToList();

            var contiguous = TryFindContiguousCombination(sorted, partySize);
            if (contiguous.Count > 0)
            {
                return contiguous;
            }

            var sameZone = TryFindSameZoneCombination(sorted, partySize);
            if (sameZone.Count > 0)
            {
                return sameZone;
            }
        }

        return new List<TableAvailabilityDto>();
    }

    private static List<TableAvailabilityDto> TryFindContiguousCombination(
        List<TableAvailabilityDto> sorted,
        int partySize)
    {
        for (var i = 0; i < sorted.Count; i++)
        {
            var sum = sorted[i].Capacity;
            var pick = new List<TableAvailabilityDto> { sorted[i] };
            var prevOrder = ParseTableOrder(sorted[i].TableCode);

            if (sum >= partySize)
            {
                return pick;
            }

            for (var j = i + 1; j < sorted.Count; j++)
            {
                var currentOrder = ParseTableOrder(sorted[j].TableCode);
                if (currentOrder - prevOrder > 1)
                {
                    break;
                }

                pick.Add(sorted[j]);
                sum += sorted[j].Capacity;
                prevOrder = currentOrder;

                if (sum >= partySize)
                {
                    return pick;
                }
            }
        }

        return new List<TableAvailabilityDto>();
    }

    private static List<TableAvailabilityDto> TryFindSameZoneCombination(
        List<TableAvailabilityDto> sorted,
        int partySize)
    {
        var descByCapacity = sorted
            .OrderByDescending(x => x.Capacity)
            .ThenBy(x => x.TableCode)
            .ToList();

        var pick = new List<TableAvailabilityDto>();
        var sum = 0;

        foreach (var table in descByCapacity)
        {
            pick.Add(table);
            sum += table.Capacity;
            if (sum >= partySize)
            {
                return pick;
            }
        }

        return new List<TableAvailabilityDto>();
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

    public async Task<List<ManualTableAvailabilityDto>> GetManualAvailableTablesAsync(
    DateTime? reservedTime,
    int? partySize,
    CancellationToken ct = default)
    {
        // Get configurations
        var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
        var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

        // Get all non-maintenance tables
        var tables = await _tableRepository.GetManualAvailableTablesAsync(ct);

        // Fetch required status IDs
        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);

        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
        var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

        // Filter by party size if specified
        if (partySize.HasValue)
        {
            tables = tables.Where(t => t.Capacity >= partySize.Value).ToList();
        }

        var result = new List<ManualTableAvailabilityDto>();
        var now = DateTime.UtcNow;

        foreach (var table in tables)
        {
            var isAvailable = true;

            // 1. Check for real-time occupancy if reservedTime is "near-now"
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

            // 2. Check for existing reservations at the specified time
            if (isAvailable && reservedTime.HasValue)
            {
                var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                    table.TableId, reservedTime.Value, duration, cancelledStatusId, noShowStatusId, ct);
                
                if (conflicts.Any())
                {
                    isAvailable = false;
                }
            }

            if (isAvailable)
            {
                result.Add(new ManualTableAvailabilityDto
                {
                    TableId = table.TableId,
                    TableCode = table.TableCode,
                    Capacity = table.Capacity,
                    TableType = table.TableTypeLv?.ValueName ?? TableTypeCode.NORMAL.ToString(),
                    Zone = table.ZoneLv?.ValueName ?? TableZoneCode.INDOOR.ToString(),
                });
            }
        }

        return result;
    }

    public async Task<ReservationResponseDto> CreateManualReservationAsync(
        CreateManualReservationRequest request,
        CancellationToken ct = default)
    {
        // Get table for validation and response
        var table = await _tableRepository.GetByIdAsync(request.TableId, ct);
        if (table == null)
        {
            throw new KeyNotFoundException($"Table with ID {request.TableId} not found.");
        }

        // Check if table is under maintenance or occupied (if near-now)
        var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
        var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

        var now = DateTime.UtcNow;

        var lockedTableStatusId = await TableStatusCode.LOCKED.ToTableStatusIdAsync(_lookupResolver, ct);
        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
        var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

        if (table.TableStatusLvId == lockedTableStatusId)
        {
            throw new InvalidOperationException($"Table {table.TableCode} is under maintenance.");
        }

        var timeDiff = (request.ReservedTime - now).TotalMinutes;
        if (timeDiff >= 0 && timeDiff <= immediateWindow)
        {
            if (table.TableStatusLvId == occupiedTableStatusId || table.TableStatusLvId == reservedTableStatusId)
            {
                throw new InvalidOperationException($"Table {table.TableCode} is currently occupied or reserved.");
            }
        }

        // Direct reservation flow (no lock token)
        // Check for existing reservations at the requested time
        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);

        var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
            request.TableId, request.ReservedTime, duration, cancelledStatusId, noShowStatusId, ct);
        if (conflicts.Any())
        {
            throw new InvalidOperationException(
                $"Table {table.TableCode} already has a reservation around the requested time.");
        }

        // Create reservation
        var phoneSourceId = await ReservationSourceCode.PHONE.IdAsync(
            _lookupResolver,
            (ushort)Core.Enum.LookupType.ReservationSource,
            ct);
        var walkInSourceId = await ReservationSourceCode.WALK_IN.IdAsync(
            _lookupResolver,
            (ushort)Core.Enum.LookupType.ReservationSource,
            ct);
        var confirmedStatusId = await ReservationStatusCode.CONFIRMED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var checkedInStatusId = await ReservationStatusCode.CHECKED_IN.ToReservationStatusIdAsync(_lookupResolver, ct);

        var reservation = new Reservation
        {
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
            Tables = new List<RestaurantTable> { table }
        };

        var created = await _reservationRepository.CreateAsync(reservation, ct);

        // Broadcast reservation created event
        await _broadcastService.BroadcastReservationCreatedAsync(created.ReservationId, table.TableId);

        _logger.LogInformation(
            "Reservation {ReservationId} created for {CustomerName} at table {TableCode}",
            created.ReservationId, request.CustomerName, table.TableCode);

        return new ReservationResponseDto
        {
            ReservationId = created.ReservationId,
            CustomerName = created.CustomerName,
            Phone = created.Phone,
            Email = created.Email,
            PartySize = created.PartySize,
            ReservedTime = created.ReservedTime,
            TableCode = table.TableCode,
            Zone = table.ZoneLv?.ValueName ?? TableZoneCode.INDOOR.ToString(),
            Status = request.Status,
            CreatedAt = created.CreatedAt ?? DateTime.UtcNow
        };
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
            CustomerId = reservation.CustomerId ?? GuestCustomerId,
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
}
