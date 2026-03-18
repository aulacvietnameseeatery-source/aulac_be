using Core.DTO.Reservation;
using Core.Entity;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Others;
using Core.Service.Utils;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Core.Service;

public class ManualReservationService : IManualReservationService
{
    private const long GuestCustomerId = 68;

    private readonly ITableRepository _tableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationBroadcastService _broadcastService;
    private readonly ILogger<ManualReservationService> _logger;
    private readonly ILookupResolver _lookupResolver;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingService _systemSettingService;

    private const string SettingReservationDuration = "reservation.default_duration_minutes";
    private const string SettingImmediateWindow = "reservation.immediate_window_minutes";

    public ManualReservationService(
        ITableRepository tableRepository,
        IReservationRepository reservationRepository,
        IReservationBroadcastService broadcastService,
        ILogger<ManualReservationService> logger,
        ILookupResolver lookupResolver,
        IUnitOfWork uow,
        IOrderRepository orderRepository,
        ISystemSettingService systemSettingService)
    {
        _tableRepository = tableRepository;
        _reservationRepository = reservationRepository;
        _broadcastService = broadcastService;
        _logger = logger;
        _lookupResolver = lookupResolver;
        _uow = uow;
        _orderRepository = orderRepository;
        _systemSettingService = systemSettingService;
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
            Tables = selectedTables
        };

        var created = await _reservationRepository.CreateAsync(reservation, ct);

        foreach (var table in selectedTables)
        {
            await _broadcastService.BroadcastReservationCreatedAsync(created.ReservationId, table.TableId);
        }

        _logger.LogInformation(
            "Reservation {ReservationId} created for {CustomerName} at tables {TableCodes}",
            created.ReservationId,
            request.CustomerName,
            string.Join(", ", selectedTables.Select(x => x.TableCode)));

        var tableCodes = string.Join(", ", selectedTables
            .Select(x => x.TableCode)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

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
            ReservedTime = created.ReservedTime,
            TableCode = tableCodes,
            Zone = zones.Count > 0 ? string.Join(", ", zones) : string.Empty,
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

    private static int ParseTableOrder(string tableCode)
    {
        var match = Regex.Match(tableCode, "(\\d+)(?!.*\\d)");
        if (!match.Success)
        {
            return int.MaxValue;
        }

        return int.TryParse(match.Value, out var value) ? value : int.MaxValue;
    }
}
