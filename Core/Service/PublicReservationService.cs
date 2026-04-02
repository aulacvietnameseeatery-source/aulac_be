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
using Core.Interface.Service.Others;
using Core.Interface.Service.Notification;
using Core.DTO.Notification;
using Core.Data;


namespace Core.Service;

/// <summary>
/// Service implementation for public reservation operations.
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
    private readonly IRealtimeNotificationService _realtimeNotification;
    private readonly INotificationService _notificationService;
    private readonly IJobSchedulerService _jobScheduler;

    private const string SettingReservationDuration = "reservation.default_duration_minutes";
    private const string SettingImmediateWindow = "reservation.immediate_window_minutes";
    private const string TemplateCodeReservationConfirmation = "RESERVATION_CONFIRM";
    private const string TemplateCodeReservationConfirmationAdmin = "RESERVATION_CONFIRM_ADMIN";
    private const string SettingStoreEmail = "store.email";

    public PublicReservationService(
        ITableRepository tableRepository,
        IReservationRepository reservationRepository,
        ILogger<PublicReservationService> logger,
        ILookupResolver lookupResolver,
        IUnitOfWork uow,
        ISystemSettingService systemSettingService,
        ICustomerService customerService,
        IEmailTemplateService emailTemplateService,
        IEmailQueue emailQueue,
        IRealtimeNotificationService realtimeNotification,
        INotificationService notificationService,
        IJobSchedulerService jobScheduler)
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
        _realtimeNotification = realtimeNotification;
        _notificationService = notificationService;
        _jobScheduler = jobScheduler;
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
        CancellationToken ct = default)
    {
        // Get configurations
        var duration = (int)await _systemSettingService.GetIntAsync(SettingReservationDuration, 120, ct);
        var immediateWindow = (int)await _systemSettingService.GetIntAsync(SettingImmediateWindow, 120, ct);

        _logger.LogInformation("[STEP 1] Fetching tables from Repo (isOnline=true)...");
        // Fetch online tables
        var tables = await _tableRepository.GetManualAvailableTablesAsync(isOnline: true, ct);
        _logger.LogInformation("[STEP 1] Found {Count} raw tables: {Codes}", tables.Count, string.Join(", ", tables.Select(t => t.TableCode)));

        // Fetch required status IDs
        var cancelledStatusId = await ReservationStatusCode.CANCELLED.ToReservationStatusIdAsync(_lookupResolver, ct);
        var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, ct);
        var completedStatusId = await ReservationStatusCode.COMPLETED.ToReservationStatusIdAsync(_lookupResolver, ct);

        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct);
        var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, ct);

        var result = new List<TableAvailabilityDto>();
        var now = DateTime.UtcNow;

        _logger.LogInformation("[STEP 4] Starting availability checks for {Count} tables at {Time}...", tables.Count, reservedTime);

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
                        _logger.LogInformation("[STEP 4] Table {TableCode} marked as NOT available due to real-time status (Occupied/Reserved)", table.TableCode);
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

            await _uow.CommitAsync(ct);

            // Fire-and-forget via Hangfire — does NOT block the response
            if (!string.IsNullOrWhiteSpace(created.Email))
                _jobScheduler.EnqueueReservationCustomerEmail(
                    created.ReservationId, created.Email, created.CustomerName,
                    created.ReservedTime, created.PartySize, tableCodes);

            _jobScheduler.EnqueueReservationAdminEmail(
                created.ReservationId, created.CustomerName,
                created.ReservedTime, created.PartySize, tableCodes);
            var zones = candidates
                .Select(x => x.Zone)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation(
                "Reservation {ReservationId} created for {CustomerName} in PENDING status with tables: {TableCodes}",
                created.ReservationId, request.CustomerName, tableCodes);

            // Send notifications to dashboard
            await _realtimeNotification.NotifyReservationUpdatedAsync(created.ReservationId, ReservationStatusCode.PENDING.ToString());
            await _notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.RESERVATION_CREATED),
                Title = "New Public Reservation",
                Body = $"Reservation #{created.ReservationId} for {request.CustomerName} ({request.PartySize} guests) at {tableCodes}",
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
                    ["tableCode"] = tableCodes,
                    ["reservedTime"] = request.ReservedTime.ToString("yyyy-MM-dd HH:mm")
                },
                TargetPermissions = new List<string> { Permissions.ViewReservation }
            }, ct);

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
        _logger.LogInformation("Starting table search for Public Reservation: PartySize={PartySize}, ReservedTime={ReservedTime}", partySize, reservedTime);

        var available = await GetAvailableTablesAsync(reservedTime, ct);
        var pool = available
            .Where(x => x.IsAvailable)
            .ToList();
        
        _logger.LogInformation("[STEP 5] Finalizing candidates. Total available in pool: {Count}, Capacity: {Cap}", 
            pool.Count, pool.Sum(x => x.Capacity));

        var options = ReservationTableSelectionUtil.FindAllTableOptions(pool, partySize);
        if (options.Count == 0)
        {
            _logger.LogWarning("[STEP 6] No valid table combinations found for PartySize={PartySize}", partySize);
            return new List<TableAvailabilityDto>();
        }

        _logger.LogInformation("[STEP 6] Generated {Count} options. Picking Best Fit...", options.Count);

        var bestOption = options
            .OrderBy(x => x.ExcessCapacity)
            .ThenBy(x => x.TableCount)
            .ThenBy(x => x.TableCodes, StringComparer.OrdinalIgnoreCase)
            .First();

        _logger.LogInformation("[STEP 6] SUCCESS: Selected Tables={TableCodes}, Exc={Exc}", 
            bestOption.TableCodes, bestOption.ExcessCapacity);

        var selectedTableIds = new HashSet<long>(bestOption.TableIds);
        var result = pool.Where(t => selectedTableIds.Contains(t.TableId)).ToList();
        
        foreach (var table in result)
        {
            _logger.LogInformation("Selected Table: ID={Id}, Code={Code}, Capacity={Capacity}, Zone={Zone}", 
                table.TableId, table.TableCode, table.Capacity, table.Zone);
        }

        return result;
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

    private async Task SendReservationConfirmationEmailToAdminAsync(Reservation reservation, string tableCodes)
    {
        try
        {
            var template = await _emailTemplateService.GetByCodeAsync(TemplateCodeReservationConfirmationAdmin);
            if (template == null)
            {
                _logger.LogWarning("Email template {TemplateCode} not found. Skipping email.", TemplateCodeReservationConfirmationAdmin);
                return;
            }

            var body = template.BodyHtml
                .Replace("{{CustomerName}}", reservation.CustomerName)
                .Replace("{{ReservedTime}}", reservation.ReservedTime.ToString("dd/MM/yyyy HH:mm"))
                .Replace("{{PartySize}}", reservation.PartySize.ToString())
                .Replace("{{TableCode}}", tableCodes)
                .Replace("{{TableCodes}}", tableCodes)
                .Replace("{{ReservationId}}", reservation.ReservationId.ToString());

            var adminEmail = await _systemSettingService.GetStringAsync(SettingStoreEmail);

            var queuedEmail = new QueuedEmail(
                To: adminEmail!,
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
