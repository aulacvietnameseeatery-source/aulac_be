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
        string? zone,
        CancellationToken ct = default)
    {
        // Get all non-maintenance tables
        var tables = await _tableRepository.GetAvailableTablesAsync(ct);

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
                Zone = table.ZoneLv?.ValueName ?? "Indoor",
                IsAvailable = !isLocked && !hasConflict,
                LockedUntil = isLocked ? lockedUntil : null,
                ImageUrl = table.TableMedia?.FirstOrDefault()?.Media?.Url
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ReservationLockResponseDto> LockTableAsync(
        CreateReservationLockRequest request,
        CancellationToken ct = default)
    {
        // Normalize table IDs: use TableIds if provided, otherwise TableId
        var tableIds = (request.TableIds != null && request.TableIds.Any())
            ? request.TableIds.Distinct().ToList()
            : new List<long> { request.TableId };
        
        if (!tableIds.Any())
        {
             throw new ArgumentException("At least one table must be selected.");
        }

        // 1. Validate all tables exist and are not under maintenance
        // We fetch all tables first
        var tables = new List<RestaurantTable>();
        foreach (var id in tableIds)
        {
            var t = await _tableRepository.GetByIdAsync(id, ct);
             if (t == null)
            {
                throw new KeyNotFoundException($"Table with ID {id} not found.");
            }
             if (t.TableStatusLvId == 17) // LOCKED status
            {
                throw new InvalidOperationException($"Table {t.TableCode} is under maintenance.");
            }
            tables.Add(t);
        }

        // 2. Check locks and conflicts for ALL tables
        foreach (var t in tables)
        {
            var lockKey = $"{LockKeyPrefix}{t.TableId}";
            if (await _cacheService.ExistsAsync(lockKey))
            {
                throw new InvalidOperationException($"Table {t.TableCode} is already reserved by another customer.");
            }

            var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                t.TableId, request.ReservedTime, 120, ct);
            if (conflicts.Any())
            {
                throw new InvalidOperationException(
                    $"Table {t.TableCode} already has a reservation around the requested time.");
            }
        }

        // 3. Generate lock token
        var lockToken = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(LockDurationMinutes);

        // 4. Lock ALL tables
        // Note: This is not strictly atomic in Redis without a Lua script, 
        // but close enough for this use case. If it fails halfway, we might leave some locks.
        // A better approach would be to set them and if error, rollback.
        
        try 
        {
            foreach (var t in tables)
            {
                 var lockKey = $"{LockKeyPrefix}{t.TableId}";
                 var lockData = new TableLockData
                {
                    LockToken = lockToken,
                    TableId = t.TableId,
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
                 
                 // Broadcast individually for now as frontend expects individual updates
                 await _broadcastService.BroadcastTableLockedAsync(t.TableId, expiresAt);
            }
        } 
        catch (Exception)
        {
            // Rollback (best effort)
             foreach (var t in tables)
            {
                 var lockKey = $"{LockKeyPrefix}{t.TableId}";
                 await _cacheService.RemoveAsync(lockKey);
            }
            throw;
        }

        var firstTable = tables.First();

        _logger.LogInformation(
            "Tables {TableCodes} locked for {CustomerName} until {ExpiresAt}",
            string.Join(", ", tables.Select(t => t.TableCode)), request.CustomerName, expiresAt);

        return new ReservationLockResponseDto
        {
            LockToken = lockToken,
            ExpiresAt = expiresAt,
            TableId = firstTable.TableId, // Return first table ID for compatibility
            TableCode = string.Join(", ", tables.Select(t => t.TableCode)) // Return combined codes
        };
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
        foreach(var id in tableIds)
        {
             var t = await _tableRepository.GetByIdAsync(id, ct);
             if (t == null) throw new KeyNotFoundException($"Table with ID {id} not found.");
             if (t.TableStatusLvId == 17) throw new InvalidOperationException($"Table {t.TableCode} is under maintenance.");
             tables.Add(t);
        }

        // 2. Validate Locks (if token provided) OR Direct Availability (if no token)
        foreach(var t in tables)
        {
             var lockKey = $"{LockKeyPrefix}{t.TableId}";
             
             if (!string.IsNullOrEmpty(request.LockToken))
             {
                 // Lock Flow
                 var lockDataJson = await _cacheService.GetAsync<string>(lockKey);
                 if (string.IsNullOrEmpty(lockDataJson))
                 {
                     throw new InvalidOperationException($"Lock for table {t.TableCode} has expired.");
                 }
                 var lockData = JsonSerializer.Deserialize<TableLockData>(lockDataJson);
                 if (lockData == null || lockData.LockToken != request.LockToken)
                 {
                      throw new InvalidOperationException($"Invalid lock token for table {t.TableCode}.");
                 }
             }
             else
             {
                 // Direct Flow
                 if (await _cacheService.ExistsAsync(lockKey))
                 {
                      throw new InvalidOperationException($"Table {t.TableCode} is currently reserved by another customer.");
                 }
                 var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(t.TableId, request.ReservedTime, 120, ct);
                 if (conflicts.Any())
                 {
                      throw new InvalidOperationException($"Table {t.TableCode} already has a reservation around the requested time.");
                 }
             }
        }

        // 3. Create Reservation
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
            Tables = tables // Add all tables
        };

        var created = await _reservationRepository.CreateAsync(reservation, ct);

        // 4. Cleanup Locks & Broadcast
        foreach(var t in tables)
        {
             var lockKey = $"{LockKeyPrefix}{t.TableId}";
             await _cacheService.RemoveAsync(lockKey);
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
            Zone = firstTable.ZoneLv?.ValueName ?? "Indoor", // Just take first zone
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
