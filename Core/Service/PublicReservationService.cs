using Core.DTO.Reservation;
using Core.Entity;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Reservation;
using Core.Service.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using Core.Interface.Service.Email;
using Core.DTO.Email;
using Core.Interface.Service;

namespace Core.Service;

/// <summary>
/// Service implementation for public reservation operations.
/// Uses Redis cache for soft-lock mechanism.
/// </summary>
public class PublicReservationService : IPublicReservationService
{
    private readonly ITableRepository _tableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<PublicReservationService> _logger;
    private readonly ILookupResolver _lookupResolver;
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ICustomerService _customerService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailQueue _emailQueue;

    private const string SettingReservationDuration = "reservation.default_duration_minutes";
    private const string SettingImmediateWindow = "reservation.immediate_window_minutes";
    private const string TemplateCodeReservationConfirmation = "RESERVATION_CONFIRM";

    public PublicReservationService(
        ITableRepository tableRepository,
        IReservationRepository reservationRepository,
        ILogger<PublicReservationService> logger,
        ILookupResolver lookupResolver,
        IUnitOfWork uow,
        ISystemSettingService systemSettingService,
        ICustomerService customerService,
        IEmailTemplateService emailTemplateService,
        IEmailQueue emailQueue)
    {
        _tableRepository = tableRepository;
        _reservationRepository = reservationRepository;
        _logger = logger;
        _lookupResolver = lookupResolver;
        _uow = uow;
        _systemSettingService = systemSettingService;
        _customerService = customerService;
        _emailTemplateService = emailTemplateService;
        _emailQueue = emailQueue;
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

        // Use the same table pool as admin manual flow to avoid channel mismatch.
        var tables = await _tableRepository.GetManualAvailableTablesAsync(ct);

        // Fetch all required status IDs via _lookupResolver extension methods
        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);
        var completedStatusId = await ReservationStatusCode.COMPLETED.ToReservationStatusIdAsync(_lookupResolver, ct);

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
                    table.TableId, reservedTime.Value, duration, cancelledStatusId, noShowStatusId, completedStatusId, ct);
                
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
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var candidates = await FindCandidateTablesAsync(request.ReservedTime, request.PartySize, ct);
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("Hiện không còn bàn online phù hợp cho số lượng khách và thời gian bạn chọn.");
            }

            await _tableRepository.LockTablesForUpdateAsync(candidates.Select(x => x.TableId), ct);

            var stillValid = await ValidateCandidateTablesAsync(candidates, request.ReservedTime, ct);
            if (!stillValid)
            {
                throw new InvalidOperationException("Bàn vừa được khách khác giữ trước bạn vài giây. Vui lòng chọn lại giờ hoặc số lượng khách.");
            }

            var onlineSourceId = await ReservationSourceCode.ONLINE.IdAsync(
                _lookupResolver,
                (ushort)Core.Enum.LookupType.ReservationSource,
                ct);
            var pendingStatusId = await ReservationStatusCode.PENDING.ToReservationStatusIdAsync(_lookupResolver, ct);

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
                ReservationStatusLvId = pendingStatusId,
            };

            foreach (var table in selectedTables)
            {
                reservation.Tables.Add(table);
            }

            var created = await _reservationRepository.CreateAsync(reservation, ct);
            var tableCodes = string.Join(", ", candidates.Select(x => x.TableCode));

            // Send confirmation email (inside transaction to ensure connection lives)
            if (!string.IsNullOrWhiteSpace(created.Email))
            {
                await SendReservationConfirmationEmailAsync(created, tableCodes);
            }

            await _uow.CommitAsync(ct);

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
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<List<TableAvailabilityDto>> FindCandidateTablesAsync(
        DateTime reservedTime,
        int partySize,
        CancellationToken ct)
    {
        var available = await GetAvailableTablesAsync(reservedTime, null, null, ct);
        var candidates = available.Where(x => x.IsAvailable).ToList();

        return SelectCandidateTablesFromAvailablePool(candidates, partySize);
    }

    private static List<TableAvailabilityDto> SelectCandidateTablesFromAvailablePool(
        List<TableAvailabilityDto> candidates,
        int partySize)
    {
        if (partySize <= 0 || candidates.Count == 0)
        {
            return new List<TableAvailabilityDto>();
        }

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

            var sameZone = ReservationTableSelectionUtil.SelectBestFitTablesWithinZone(sorted, partySize);
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

    private async Task<bool> ValidateCandidateTablesAsync(
        List<TableAvailabilityDto> candidates,
        DateTime reservedTime,
        CancellationToken ct)
    {
        var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
        var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);
        var completedStatusId = await ReservationStatusCode.COMPLETED.ToReservationStatusIdAsync(_lookupResolver, ct);

        var lockedTableStatusId = await TableStatusCode.LOCKED.ToTableStatusIdAsync(_lookupResolver, ct);
        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
        var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

        var now = DateTime.UtcNow;
        var timeDiff = (reservedTime - now).TotalMinutes;

        foreach (var candidate in candidates)
        {
            var table = await _tableRepository.GetByIdAsync(candidate.TableId, ct);
            if (table == null)
            {
                return false;
            }

            if (table.TableStatusLvId == lockedTableStatusId)
            {
                return false;
            }

            if (timeDiff >= 0 && timeDiff <= immediateWindow &&
                (table.TableStatusLvId == occupiedTableStatusId || table.TableStatusLvId == reservedTableStatusId))
            {
                return false;
            }

            var conflicts = await _reservationRepository.GetTableReservationsForTimeAsync(
                table.TableId,
                reservedTime,
                duration,
                cancelledStatusId,
                noShowStatusId,
                completedStatusId,
                ct);

            if (conflicts.Any())
            {
                return false;
            }
        }

        return true;
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
            _logger.LogInformation("Enqueued confirmation email for reservation {ReservationId} to {Email}", reservation.ReservationId, reservation.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing confirmation email for reservation {ReservationId}", reservation.ReservationId);
        }
    }
}
