using Core.Data;
using Core.DTO.Notification;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Notification;
using Microsoft.Extensions.Options;

namespace Api.Background;

/// <summary>
/// Periodically scans for shift assignments that are past their start time
/// (+ threshold) with no attendance record, and sends SHIFT_NO_SHOW notifications.
/// </summary>
public sealed class NoShowDetectionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NoShowDetectionJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public NoShowDetectionJob(
        IServiceScopeFactory scopeFactory,
        ILogger<NoShowDetectionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NoShowDetectionJob started. Checking every {Interval} minutes.", _interval.TotalMinutes);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DetectNoShowsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during no-show detection scan");
            }
        }

        _logger.LogInformation("NoShowDetectionJob stopped.");
    }

    private async Task DetectNoShowsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var assignmentRepo = scope.ServiceProvider.GetRequiredService<IShiftAssignmentRepository>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AttendanceOptions>>().Value;

        var threshold = DateTime.UtcNow.AddMinutes(-options.NoShowThresholdMinutes);
        var candidates = await assignmentRepo.GetNoShowCandidatesAsync(threshold, ct);

        if (candidates.Count == 0) return;

        _logger.LogInformation("No-show detection found {Count} candidate(s)", candidates.Count);

        foreach (var assignment in candidates)
        {
            await notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.SHIFT_NO_SHOW),
                Title = "No-Show Alert",
                Body = $"{assignment.Staff?.FullName ?? "Staff"} has not checked in for " +
                       $"{assignment.ShiftTemplate?.TemplateName ?? "shift"} " +
                       $"on {assignment.WorkDate:yyyy-MM-dd} (started {assignment.PlannedStartAt:HH:mm})",
                Priority = nameof(NotificationPriority.High),
                SoundKey = "notification_alert",
                ActionUrl = "/dashboard/shift-management",
                EntityType = "ShiftAssignment",
                EntityId = assignment.ShiftAssignmentId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["shiftAssignmentId"] = assignment.ShiftAssignmentId.ToString(),
                    ["staffId"] = assignment.StaffId.ToString(),
                    ["staffName"] = assignment.Staff?.FullName ?? "Unknown",
                    ["workDate"] = assignment.WorkDate.ToString("yyyy-MM-dd"),
                    ["plannedStart"] = assignment.PlannedStartAt.ToString("HH:mm")
                },
                // Notify managers — no specific target; broadcast to those with ViewShift permission
                TargetUserIds = new List<long>()
            }, ct);
        }
    }
}
