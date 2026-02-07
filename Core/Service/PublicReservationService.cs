using Core.DTO.Reservation;
using Core.Entity;
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
    private readonly ICacheService _cacheService;
    private readonly IReservationBroadcastService _broadcastService;
    private readonly ILogger<PublicReservationService> _logger;

    // Reservation source lookup value ID for ONLINE
    private const uint ReservationSourceOnline = 63;
    
    // Reservation status lookup value ID for PENDING
    private const uint ReservationStatusPending = 21;

    // Lock configuration
    private const int LockDurationMinutes = 10;
    private const string LockKeyPrefix = "table:lock:";

    public PublicReservationService(
        ITableRepository tableRepository,
        IReservationRepository reservationRepository,
        ICacheService cacheService,
        IReservationBroadcastService broadcastService,
        ILogger<PublicReservationService> logger)
    {
        _tableRepository = tableRepository;
        _reservationRepository = reservationRepository;
        _cacheService = cacheService;
        _broadcastService = broadcastService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<TableAvailabilityDto>> GetAvailableTablesAsync(
        DateTime? reservedTime,
        int? partySize,
        CancellationToken ct = default)
    {
        // Get all non-maintenance tables
        var tables = await _tableRepository.GetAvailableTablesAsync(ct);

        // Filter by party size if specified
        if (partySize.HasValue)
        {
            tables = tables.Where(t => t.Capacity >= partySize.Value).ToList();
        }

        var result = new List<TableAvailabilityDto>();

        foreach (var table in tables)
        {
            // Check if table is soft-locked in Redis
            var lockKey = $"{LockKeyPrefix}{table.TableId}";
            var lockDataJson = await _cacheService.GetAsync<string>(lockKey);
            var isLocked = !string.IsNullOrEmpty(lockDataJson);
            DateTime? lockedUntil = null;

            if (isLocked)
            {
                try
                {
                    var lockData = JsonSerializer.Deserialize<TableLockData>(lockDataJson!);
                    if (lockData != null)
                    {
                        lockedUntil = lockData.CreatedAt.AddMinutes(LockDurationMinutes);
                        // If it's already past, treat as available? 
                        // Redis should have expired the key, but just in case.
                        if (lockedUntil <= DateTime.UtcNow)
                        {
                            isLocked = false;
                            lockedUntil = null;
                        }
                    }
                }
                catch
                {
                    // Ignore deserialization errors, treat as locked but unknown time? 
                    // Or just treat as locked.
                }
            }

            // Check for existing reservations at the specified time
            var hasConflict = false;
            if (reservedTime.HasValue)
            {
                var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                    table.TableId, reservedTime.Value, 120, ct);
                hasConflict = conflicts.Any();
            }

            result.Add(new TableAvailabilityDto
            {
                TableId = table.TableId,
                TableCode = table.TableCode,
                Capacity = table.Capacity,
                TableType = table.TableTypeLv?.ValueName ?? "Standard",
                IsAvailable = !isLocked && !hasConflict,
                LockedUntil = isLocked ? lockedUntil : null
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ReservationLockResponseDto> LockTableAsync(
        CreateReservationLockRequest request,
        CancellationToken ct = default)
    {
        // Validate table exists
        var table = await _tableRepository.GetByIdAsync(request.TableId, ct);
        if (table == null)
        {
            throw new KeyNotFoundException($"Table with ID {request.TableId} not found.");
        }

        // Check if table is under maintenance
        if (table.TableStatusLvId == 17) // LOCKED status
        {
            throw new InvalidOperationException($"Table {table.TableCode} is under maintenance.");
        }

        // Check if table is already soft-locked
        var lockKey = $"{LockKeyPrefix}{request.TableId}";
        if (await _cacheService.ExistsAsync(lockKey))
        {
            throw new InvalidOperationException($"Table {table.TableCode} is already reserved by another customer.");
        }

        // Check for existing reservations at the requested time
        var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
            request.TableId, request.ReservedTime, 120, ct);
        if (conflicts.Any())
        {
            throw new InvalidOperationException(
                $"Table {table.TableCode} already has a reservation around the requested time.");
        }

        // Generate lock token
        var lockToken = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(LockDurationMinutes);

        // Store lock in Redis
        var lockData = new TableLockData
        {
            LockToken = lockToken,
            TableId = request.TableId,
            CustomerName = request.CustomerName,
            Phone = request.Phone,
            PartySize = request.PartySize,
            ReservedTime = request.ReservedTime,
            CreatedAt = DateTime.UtcNow
        };

        await _cacheService.SetAsync(
            lockKey,
            JsonSerializer.Serialize(lockData),
            TimeSpan.FromMinutes(LockDurationMinutes));

        // Broadcast lock event
        await _broadcastService.BroadcastTableLockedAsync(table.TableId, expiresAt);

        _logger.LogInformation(
            "Table {TableId} ({TableCode}) locked for {CustomerName} until {ExpiresAt}",
            table.TableId, table.TableCode, request.CustomerName, expiresAt);

        return new ReservationLockResponseDto
        {
            LockToken = lockToken,
            ExpiresAt = expiresAt,
            TableId = table.TableId,
            TableCode = table.TableCode
        };
    }

    /// <inheritdoc />
    public async Task<ReservationResponseDto> CreateReservationAsync(
        CreateReservationRequest request,
        CancellationToken ct = default)
    {
        // Validate lock token
        var lockKey = $"{LockKeyPrefix}{request.TableId}";
        var lockDataJson = await _cacheService.GetAsync<string>(lockKey);
        if (string.IsNullOrEmpty(lockDataJson))
        {
            throw new InvalidOperationException(
                "Lock has expired or is invalid. Please lock the table again.");
        }

        var lockData = JsonSerializer.Deserialize<TableLockData>(lockDataJson);
        if (lockData == null || lockData.LockToken != request.LockToken)
        {
            throw new InvalidOperationException(
                "Invalid lock token. Please lock the table again.");
        }

        // Get table for response
        var table = await _tableRepository.GetByIdAsync(request.TableId, ct);
        if (table == null)
        {
            throw new KeyNotFoundException($"Table with ID {request.TableId} not found.");
        }

        // Create reservation
        var reservation = new Reservation
        {
            CustomerName = request.CustomerName,
            Phone = request.Phone,
            Email = request.Email,
            PartySize = request.PartySize,
            ReservedTime = request.ReservedTime,
            CreatedAt = DateTime.UtcNow,
            SourceLvId = ReservationSourceOnline,
            ReservationStatusLvId = ReservationStatusPending, // Pending
            Tables = new List<RestaurantTable> { table }
        };

        var created = await _reservationRepository.CreateAsync(reservation, ct);

        // Remove lock from Redis
        await _cacheService.RemoveAsync(lockKey);

        // Broadcast updates
        // 1. Table is no longer soft-locked (implicit by remove), but now it has a reservation.
        // We might want to broadcast a specific "ReservationCreated" which implies availability changes.
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
            Status = "Pending",
            CreatedAt = created.CreatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Internal class for storing lock data in Redis.
    /// </summary>
    private class TableLockData
    {
        public string LockToken { get; set; } = null!;
        public long TableId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int PartySize { get; set; }
        public DateTime ReservedTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
