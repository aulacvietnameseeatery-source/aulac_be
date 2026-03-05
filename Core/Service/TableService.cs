using Core.DTO.General;
using Core.DTO.LookUpValue;
using Core.DTO.Table;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.LookUp;
using Core.Interface.Service.Table;

namespace Core.Service;

public class TableService : ITableService
{
    private readonly ITableRepository _tableRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupResolver _lookupResolver;
    private readonly ILookupService _lookupService;
    private readonly IFileStorage _fileStorage;
    private readonly IMediaRepository _mediaRepository;

    /// <summary>
    /// Allowed status transitions — key: current code, value: set of permitted next codes.
    /// AVAILABLE → OCCUPIED, RESERVED, LOCKED
    /// OCCUPIED  → LOCKED
    /// RESERVED  → OCCUPIED, AVAILABLE
    /// LOCKED    → AVAILABLE
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
    {
        [nameof(TableStatusCode.AVAILABLE)] = [nameof(TableStatusCode.OCCUPIED), nameof(TableStatusCode.RESERVED), nameof(TableStatusCode.LOCKED)],
        [nameof(TableStatusCode.OCCUPIED)] = [nameof(TableStatusCode.LOCKED)],
        [nameof(TableStatusCode.RESERVED)] = [nameof(TableStatusCode.OCCUPIED), nameof(TableStatusCode.AVAILABLE)],
        [nameof(TableStatusCode.LOCKED)] = [nameof(TableStatusCode.AVAILABLE)],
    };

    public TableService(
        ITableRepository tableRepository,
        IUnitOfWork unitOfWork,
        ILookupResolver lookupResolver,
        ILookupService lookupService,
        IFileStorage fileStorage,
        IMediaRepository mediaRepository)
    {
        _tableRepository = tableRepository;
        _unitOfWork = unitOfWork;
        _lookupResolver = lookupResolver;
        _lookupService = lookupService;
        _fileStorage = fileStorage;
        _mediaRepository = mediaRepository;
    }

    #region ── List / Select ──

    /// <inheritdoc />
    public async Task<(List<TableManagementDto> Items, int TotalCount)> GetTablesForManagementAsync(
        GetTableManagementRequest request, CancellationToken ct = default)
    {
        var (tables, totalCount) = await _tableRepository.GetTablesForManagementAsync(request, ct);
        return (tables.Select(MapToManagementDto).ToList(), totalCount);
    }

    /// <inheritdoc />
    public async Task<List<TableSelectDto>> GetTablesForSelectAsync(CancellationToken ct = default)
    {
        var tables = await _tableRepository.GetTablesWithRelationsAsync(ct);
        if (tables is not { Count: > 0 })
            return [];

        var now = DateTime.UtcNow;

        return tables.Select(t =>
        {
            var activeOrder = t.Orders.FirstOrDefault(o =>
           o.OrderStatusLv.ValueCode != nameof(OrderStatusCode.CANCELLED) &&
           o.OrderStatusLv.ValueCode != nameof(OrderStatusCode.COMPLETED));

            var upcomingReservation = t.Reservations
                    .Where(r => r.ReservedTime > now)
                .OrderBy(r => r.ReservedTime)
                .FirstOrDefault();

            return new TableSelectDto
            {
                TableId = t.TableId,
                TableCode = t.TableCode,
                Capacity = t.Capacity,
                ZoneId = t.ZoneLvId,
                ZoneName = t.ZoneLv.ValueName,
                StatusCode = t.TableStatusLv.ValueCode,
                HasActiveOrder = activeOrder is not null,
                ActiveOrderId = activeOrder?.OrderId,
                UpcomingReservationTime = upcomingReservation?.ReservedTime
            };
        }).ToList();
    }

    #endregion

    #region ── Table CRUD ──

    /// <inheritdoc />
    public async Task<TableDetailDto> GetTableByIdAsync(long id, CancellationToken ct = default)
    {
        var table = await _tableRepository.GetByIdWithDetailsAsync(id, ct)
    ?? throw new NotFoundException("Table not found");

        var now = DateTime.UtcNow;

        return new TableDetailDto
        {
            // ── base fields ──
            TableId = table.TableId,
            TableCode = table.TableCode,
            Capacity = table.Capacity,
            IsOnline = table.IsOnline ?? false,

            // ── status ──
            StatusId = table.TableStatusLvId,
            StatusCode = table.TableStatusLv?.ValueCode ?? "UNKNOWN",
            StatusName = table.TableStatusLv?.ValueName ?? "Unknown",

            // ── type ──
            TypeId = table.TableTypeLvId,
            TypeName = table.TableTypeLv?.ValueName ?? "Unknown",

            // ── zone ──
            ZoneId = table.ZoneLvId,
            ZoneName = table.ZoneLv?.ValueName ?? "Unknown",

            // ── QR ──
            QrCodeUrl = table.QrToken,
            QrCodeImageUrl = table.TableQrImgNavigation?.Url,

            // ── media ──
            Images = table.TableMedia
            .OrderByDescending(tm => tm.IsPrimary ?? false)
            .Select(tm => new TableMediaDto
            {
                MediaId = tm.MediaId,
                Url = tm.Media.Url,
                IsPrimary = tm.IsPrimary ?? false
            }).ToList(),

            // ── operational state ──
            ActiveOrdersCount = table.Orders.Count(o =>
                o.OrderStatusLv.ValueCode == nameof(OrderStatusCode.PENDING) ||
                o.OrderStatusLv.ValueCode == nameof(OrderStatusCode.IN_PROGRESS)),
            HasErrors = table.ServiceErrors.Any(se => se.IsResolved == false),

            UpcomingReservations = table.Reservations
            .Where(r => r.ReservedTime > now &&
                (r.ReservationStatusLv.ValueCode == nameof(ReservationStatusCode.PENDING) ||
                r.ReservationStatusLv.ValueCode == nameof(ReservationStatusCode.CONFIRMED)))
            .OrderBy(r => r.ReservedTime)
            .Take(5)
            .Select(r => new UpcomingReservationDto
            {
                ReservationId = r.ReservationId,
                GuestName = r.CustomerName,
                Pax = r.PartySize,
                ReservedTime = r.ReservedTime,
                StatusCode = r.ReservationStatusLv.ValueCode
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<TableManagementDto> CreateTableAsync(CreateTableRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.TableCode))
            throw new ValidationException("Table code is required");

        if (request.Capacity <= 0)
            throw new ValidationException("Capacity must be a positive integer");

        if (await _tableRepository.TableCodeExistsAsync(request.TableCode, ct: ct))
            throw new ConflictException($"Table code '{request.TableCode}' already exists");

        await ValidateLookupAsync(request.StatusLvId, (ushort)LookupType.TableStatus, "table status", ct);
        await ValidateLookupAsync(request.TypeLvId, (ushort)LookupType.TableType, "table type", ct);
        await ValidateLookupAsync(request.ZoneLvId, (ushort)LookupType.TableZone, "table zone", ct);

        var entity = new Entity.RestaurantTable
        {
            TableCode = request.TableCode.Trim(),
            Capacity = request.Capacity,
            IsOnline = request.IsOnline,
            TableStatusLvId = request.StatusLvId,
            TableTypeLvId = request.TypeLvId,
            ZoneLvId = request.ZoneLvId,
            QrToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tableRepository.Add(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        var saved = await _tableRepository.GetByIdAsync(entity.TableId, ct)
            ?? throw new NotFoundException("Table not found after creation");

        return MapToManagementDto(saved);
    }

    /// <inheritdoc />
    public async Task<TableManagementDto> UpdateTableAsync(long id, UpdateTableRequest request, CancellationToken ct = default)
    {
        var table = await _tableRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Table not found");

        if (request.TableCode is not null && request.TableCode != table.TableCode)
        {
            var trimmed = request.TableCode.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Table code cannot be blank");

            if (await _tableRepository.TableCodeExistsAsync(trimmed, excludeId: id, ct: ct))
                throw new ConflictException($"Table code '{trimmed}' already exists");

            table.TableCode = trimmed;
        }

        if (request.Capacity.HasValue)
        {
            if (request.Capacity.Value <= 0)
                throw new ValidationException("Capacity must be a positive integer");
            table.Capacity = request.Capacity.Value;
        }

        if (request.IsOnline.HasValue)
            table.IsOnline = request.IsOnline.Value;

        if (request.StatusLvId.HasValue)
        {
            await ValidateLookupAsync(request.StatusLvId.Value, (ushort)LookupType.TableStatus, "table status", ct);
            table.TableStatusLvId = request.StatusLvId.Value;
        }

        if (request.TypeLvId.HasValue)
        {
            await ValidateLookupAsync(request.TypeLvId.Value, (ushort)LookupType.TableType, "table type", ct);
            table.TableTypeLvId = request.TypeLvId.Value;
        }

        if (request.ZoneLvId.HasValue)
        {
            await ValidateLookupAsync(request.ZoneLvId.Value, (ushort)LookupType.TableZone, "table zone", ct);
            table.ZoneLvId = request.ZoneLvId.Value;
        }

        table.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _tableRepository.GetByIdAsync(id, ct)!;
        return MapToManagementDto(updated!);
    }

    /// <inheritdoc />
    public async Task DeleteTableAsync(long id, CancellationToken ct = default)
    {
        var table = await _tableRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Table not found");

        var activeOrders = await _tableRepository.CountActiveOrdersAsync(id, ct);
        var upcomingReservations = await _tableRepository.CountUpcomingReservationsAsync(id, ct);

        if (activeOrders > 0 || upcomingReservations > 0)
        {
            var parts = new List<string>();
            if (activeOrders > 0) parts.Add($"{activeOrders} active order(s)");
            if (upcomingReservations > 0) parts.Add($"{upcomingReservations} upcoming reservation(s)");

            throw new ConflictException($"Cannot delete table '{table.TableCode}': {string.Join(" and ", parts)}");
        }

        table.IsDeleted = true;
        table.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TableManagementDto> UpdateStatusAsync(long id, UpdateTableStatusRequest request, CancellationToken ct = default)
    {
        var table = await _tableRepository.GetByIdAsync(id, ct)
         ?? throw new NotFoundException("Table not found");

        await ValidateLookupAsync(request.StatusLvId, (ushort)LookupType.TableStatus, "table status", ct);

        var currentCode = table.TableStatusLv?.ValueCode ?? "UNKNOWN";
        var newCode = await _tableRepository.GetLookupValueCodeAsync(request.StatusLvId, ct) ?? "UNKNOWN";

        if (AllowedTransitions.TryGetValue(currentCode, out var allowed) && !allowed.Contains(newCode))
        {
            throw new UnprocessableEntityException($"Invalid status transition: {currentCode} → {newCode}. "
                + $"Allowed from {currentCode}: {string.Join(", ", allowed)}");
        }

        table.TableStatusLvId = request.StatusLvId;
        table.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _tableRepository.GetByIdAsync(id, ct)!;
        return MapToManagementDto(updated!);
    }

    #endregion

    #region ── Zone & Type lookups — delegate to ILookupService ──

    /// <inheritdoc />
    public Task<List<LookupValueI18nDto>> GetZonesAsync(CancellationToken ct = default)
        => _lookupService.GetAllActiveByTypeAsync((ushort)LookupType.TableZone, ct);

    /// <inheritdoc />
    public Task<List<LookupValueI18nDto>> GetTableTypesAsync(CancellationToken ct = default)
    => _lookupService.GetAllActiveByTypeAsync((ushort)LookupType.TableType, ct);

    /// <inheritdoc />
    public Task<LookupValueI18nDto> CreateZoneAsync(CreateLookupValueRequest request, CancellationToken ct = default)
 => _lookupService.CreateAsync((ushort)LookupType.TableZone, request, ct);

    /// <inheritdoc />
    public Task<LookupValueI18nDto> CreateTableTypeAsync(CreateLookupValueRequest request, CancellationToken ct = default)
        => _lookupService.CreateAsync((ushort)LookupType.TableType, request, ct);

    /// <inheritdoc />
    public Task<LookupValueI18nDto> UpdateZoneAsync(uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default)
        => _lookupService.UpdateAsync((ushort)LookupType.TableZone, valueId, request, ct);

    /// <inheritdoc />
    public Task<LookupValueI18nDto> UpdateTableTypeAsync(uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default)
  => _lookupService.UpdateAsync((ushort)LookupType.TableType, valueId, request, ct);

    /// <inheritdoc />
    public Task DeleteZoneAsync(uint valueId, CancellationToken ct = default)
        => _lookupService.DeleteAsync((ushort)LookupType.TableZone, valueId, "zone", ct);

    /// <inheritdoc />
    public Task DeleteTableTypeAsync(uint valueId, CancellationToken ct = default)
        => _lookupService.DeleteAsync((ushort)LookupType.TableType, valueId, "type", ct);

    #endregion

    #region ── Bulk online toggle ──

    /// <inheritdoc />
    public async Task<int> BulkSetOnlineAsync(BulkOnlineRequest request, CancellationToken ct = default)
    {
        await ValidateLookupAsync(request.ZoneId, (ushort)LookupType.TableZone, "table zone", ct);
        return await _tableRepository.BulkSetOnlineByZoneAsync(request.ZoneId, request.IsOnline, ct);
    }

    #endregion

    #region ── QR code ──

    /// <inheritdoc />
    public async Task<QrCodeDto> RegenerateQrCodeAsync(long tableId, CancellationToken ct = default)
    {
        var table = await _tableRepository.GetByIdAsync(tableId, ct)
            ?? throw new NotFoundException("Table not found");

        table.QrToken = Guid.NewGuid().ToString("N");
        table.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        return new QrCodeDto
        {
            QrCodeUrl = table.QrToken,
            QrCodeImageUrl = table.TableQrImgNavigation?.Url
        };
    }

    #endregion

    #region ── Table media ──

    /// <inheritdoc />
    public async Task<List<TableMediaDto>> UploadTableMediaAsync(long tableId, List<MediaFileInput> files, CancellationToken ct = default)
    {
        _ = await _tableRepository.GetByIdAsync(tableId, ct)
          ?? throw new NotFoundException("Table not found");

        // Convert to FileUploadRequest and let IFileStorage handle all validation
        var uploadRequests = files.Select(f => new FileUploadRequest
        {
            Stream = f.Stream,
            FileName = f.FileName,
            ContentType = f.ContentType
        }).ToList();

        // SaveManyAsync validates batch count, file size, MIME type, extension — then saves all
        var uploadResults = await _fileStorage.SaveManyAsync(
   uploadRequests,
            "table-media",
            FileValidationOptions.ImageUpload,
        ct);

        var imageTypeLvId = await _lookupResolver.GetIdAsync(
      (ushort)LookupType.MediaType, nameof(MediaTypeCode.IMAGE), ct);

        var result = new List<TableMediaDto>(uploadResults.Count);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            foreach (var uploaded in uploadResults)
            {
                var asset = await _mediaRepository.AddMediaAsync(new Entity.MediaAsset
                {
                    Url = uploaded.PublicUrl,
                    MimeType = files.First(f => f.FileName == uploaded.OriginalFileName).ContentType,
                    MediaTypeLvId = imageTypeLvId,
                    CreatedAt = DateTime.UtcNow
                }, ct);

                _tableRepository.AddTableMedia(new Entity.TableMedium
                {
                    TableId = tableId,
                    MediaId = asset.MediaId,
                    IsPrimary = false
                });

                result.Add(new TableMediaDto
                {
                    MediaId = asset.MediaId,
                    Url = uploaded.PublicUrl,
                    IsPrimary = false
                });
            }

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            // Clean up already-saved files since DB transaction failed
            await _fileStorage.DeleteManyAsync(uploadResults.Select(r => r.RelativePath));
            throw;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task DeleteTableMediaAsync(long tableId, long mediaId, CancellationToken ct = default)
    {
        _ = await _tableRepository.GetByIdAsync(tableId, ct)
                ?? throw new NotFoundException("Table not found");

        var link = await _tableRepository.GetTableMediaAsync(tableId, mediaId, ct)
                ?? throw new NotFoundException("Media not found for this table");

        var fileUrl = link.Media.Url;

        _tableRepository.RemoveTableMedia(link);
        await _mediaRepository.RemoveMediaAsync(link.Media, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Best-effort file cleanup — orphaned files can be swept by a cleanup job
        try { await _fileStorage.DeleteAsync(fileUrl); } catch { /* ignored */ }
    }

    #endregion

    #region ── Private helpers ──

    private static TableManagementDto MapToManagementDto(Entity.RestaurantTable t) => new()
    {
        TableId = t.TableId,
        TableCode = t.TableCode,
        Capacity = t.Capacity,
        IsOnline = t.IsOnline ?? false,
        StatusId = t.TableStatusLvId,
        StatusCode = t.TableStatusLv?.ValueCode ?? "UNKNOWN",
        StatusName = t.TableStatusLv?.ValueName ?? "Unknown",
        TypeId = t.TableTypeLvId,
        TypeName = t.TableTypeLv?.ValueName ?? "Unknown",
        ZoneId = t.ZoneLvId,
        ZoneName = t.ZoneLv?.ValueName ?? "Unknown"
    };

    private async Task ValidateLookupAsync(uint lvId, ushort typeId, string label, CancellationToken ct)
    {
        if (!await _tableRepository.IsValidLookupAsync(lvId, typeId, ct))
            throw new ValidationException($"Invalid {label} (LvId={lvId})");
    }

    /// <inheritdoc />
    public async Task OccupyTableByCodeAsync(string tableCode, CancellationToken ct = default)
    {
        // 1. Find table by code
        var table = await _tableRepository.GetByCodeAsync(tableCode, ct)
            ?? throw new NotFoundException($"Table '{tableCode}' not found");

        // 2. Get status IDs
        var availableLvId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, ct);
        var occupiedLvId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);

        // 3. Atomic update: Only allow AVAILABLE -> OCCUPIED
        // This ensures only one customer can occupy the table (race condition safe)
        var updated = await _tableRepository.TryOccupyIfAvailableAsync(
            table.TableId,
            availableLvId,
            occupiedLvId,
            ct);

        if (!updated)
        {
            throw new ConflictException($"Table '{tableCode}' is already occupied by another customer. Please choose another available table.");
        }
    }

    #endregion
}

