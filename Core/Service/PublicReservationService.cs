using Core.DTO.Reservation;
using Core.Entity;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Others;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Core.Service;

/// <summary>
/// Service implementation for public reservation operations.
/// Uses Redis cache for soft-lock mechanism.
/// </summary>
public class PublicReservationService : IPublicReservationService
{
    private readonly ITableRepository _tableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationBroadcastService _broadcastService;
    private readonly ILogger<PublicReservationService> _logger;
    private readonly ILookupResolver _lookupResolver;

    public PublicReservationService(
        ITableRepository tableRepository,
        IReservationRepository reservationRepository,
        IReservationBroadcastService broadcastService,
        ILogger<PublicReservationService> logger,
        ILookupResolver lookupResolver)
    {
        _tableRepository = tableRepository;
        _reservationRepository = reservationRepository;
        _broadcastService = broadcastService;
        _logger = logger;
        _lookupResolver = lookupResolver;
    }

    public async Task<List<TableAvailabilityDto>> GetAvailableTablesAsync(
        DateTime? reservedTime,
        int? partySize,
        string? zone,
        CancellationToken ct = default)
    {
        // Get all non-maintenance tables
        var tables = await _tableRepository.GetAvailableTablesAsync(ct);

        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);

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

        foreach (var table in tables)
        {
            // Check for existing reservations at the specified time
            var hasConflict = false;
            if (reservedTime.HasValue)
            {
                var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                    table.TableId, reservedTime.Value, 120, cancelledStatusId, noShowStatusId, ct);
                hasConflict = conflicts.Any();
            }

            result.Add(new TableAvailabilityDto
            {
                TableId = table.TableId,
                TableCode = table.TableCode,
                Capacity = table.Capacity,
                TableType = table.TableTypeLv?.ValueName ?? TableTypeCode.NORMAL.ToString(),
                Zone = table.ZoneLv?.ValueName ?? TableZoneCode.INDOOR.ToString(),
                IsAvailable = !hasConflict,
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
         // Normalize table IDs
        var tableIds = (request.TableIds != null && request.TableIds.Any())
            ? request.TableIds.Distinct().ToList()
            : new List<long> { request.TableId };

        if (!tableIds.Any())
        {
             throw new ArgumentException("At least one table must be selected.");
        }

        // 1. Validate all tables exist and conditions
        var tables = new List<RestaurantTable>();

        var lockedTableStatusId = await TableStatusCode.LOCKED.ToTableStatusIdAsync(_lookupResolver, ct);
        foreach(var id in tableIds)
        {
             var t = await _tableRepository.GetByIdAsync(id, ct);
             if (t == null) throw new KeyNotFoundException($"Table with ID {id} not found.");
             if (t.TableStatusLvId == lockedTableStatusId) throw new InvalidOperationException($"Table {t.TableCode} is under maintenance.");
             tables.Add(t);
        }

        // 2. Validate Direct Availability
        foreach(var t in tables)
        {
            var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
            var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);

            var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                t.TableId, request.ReservedTime, 120, cancelledStatusId, noShowStatusId, ct);
            if (conflicts.Any())
            {
                    throw new InvalidOperationException($"Table {t.TableCode} already has a reservation around the requested time.");
            }
        }

        // 3. Create Reservation
        var onlineSourceId = await ReservationSourceCode.ONLINE.IdAsync(
            _lookupResolver,
            (ushort)Core.Enum.LookupType.ReservationSource,
            ct);
        var pendingStatusId = await ReservationStatusCode.PENDING.ToReservationStatusIdAsync(_lookupResolver, ct);

        var reservation = new Reservation
        {
            CustomerName = request.CustomerName,
            Phone = request.Phone,
            Email = request.Email,
            PartySize = request.PartySize,
            ReservedTime = request.ReservedTime,
            CreatedAt = DateTime.UtcNow,
            SourceLvId = onlineSourceId,
            ReservationStatusLvId = pendingStatusId, // Pending
            Tables = tables // Add all tables
        };

        var created = await _reservationRepository.CreateAsync(reservation, ct);

        // 4. Cleanup & Broadcast
        foreach(var t in tables)
        {
             await _broadcastService.BroadcastReservationCreatedAsync(created.ReservationId, t.TableId);
        }

        var firstTable = tables.First();

        _logger.LogInformation(
            "Reservation {ReservationId} created for {CustomerName} at tables {TableCodes}",
            created.ReservationId, request.CustomerName, string.Join(", ", tables.Select(t => t.TableCode)));

        return new ReservationResponseDto
        {
            ReservationId = created.ReservationId,
            CustomerName = created.CustomerName,
            Phone = created.Phone,
            Email = created.Email,
            PartySize = created.PartySize,
            ReservedTime = created.ReservedTime,
            TableCode = string.Join(", ", tables.Select(t => t.TableCode)),
            Zone = firstTable.ZoneLv?.ValueName ?? TableZoneCode.INDOOR.ToString(), // Just take first zone
            Status = ReservationStatusCode.PENDING.ToString(),
            CreatedAt = created.CreatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<List<ManualTableAvailabilityDto>> GetManualAvailableTablesAsync(
    DateTime? reservedTime,
    int? partySize,
    CancellationToken ct = default)
    {
        // Get all non-maintenance tables
        var tables = await _tableRepository.GetManualAvailableTablesAsync(ct);

        // Filter by party size if specified
        if (partySize.HasValue)
        {
            tables = tables.Where(t => t.Capacity >= partySize.Value).ToList();
        }
        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);

        var result = new List<ManualTableAvailabilityDto>();

        foreach (var table in tables)
        {
            // Check for existing reservations at the specified time
            var hasConflict = false;
            if (reservedTime.HasValue)
            {
                var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                    table.TableId, reservedTime.Value, 120, cancelledStatusId, noShowStatusId, ct);
                hasConflict = conflicts.Any();
            }

            if (!hasConflict)
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

        // Check if table is under maintenance
        var lockedTableStatusId = await TableStatusCode.LOCKED.ToTableStatusIdAsync(_lookupResolver, ct);
        if (table.TableStatusLvId == lockedTableStatusId) // LOCKED status
        {
            throw new InvalidOperationException($"Table {table.TableCode} is under maintenance.");
        }

        // Direct reservation flow (no lock token)
        // Check for existing reservations at the requested time
        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);

        var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
            request.TableId, request.ReservedTime, 120, cancelledStatusId, noShowStatusId, ct);
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
}
