using Core.Data;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Shift;
using Microsoft.Extensions.Options;

namespace Core.Service;

/// <summary>
/// Reads shift/attendance settings from <c>systemsetting</c> table,
/// falling back to <see cref="AttendanceOptions"/> (appsettings.json) defaults.
/// </summary>
public class ShiftSettingsProvider : IShiftSettingsProvider
{
    private readonly ISystemSettingService _settings;
    private readonly AttendanceOptions _fallback;

    public ShiftSettingsProvider(
        ISystemSettingService settings,
        IOptions<AttendanceOptions> fallback)
    {
        _settings = settings;
        _fallback = fallback.Value;
    }

    public async Task<int> GetAllowedEarlyCheckInMinutesAsync(CancellationToken ct = default)
        => (int)(await _settings.GetIntAsync("shift.allowed_early_check_in_minutes", _fallback.AllowedEarlyCheckInMinutes, ct))!;

    public async Task<int> GetLateGraceMinutesAsync(CancellationToken ct = default)
        => (int)(await _settings.GetIntAsync("shift.late_grace_minutes", _fallback.LateGraceMinutes, ct))!;

    public async Task<int> GetAbsenceThresholdMinutesAsync(CancellationToken ct = default)
        => (int)(await _settings.GetIntAsync("shift.absence_threshold_minutes", _fallback.AbsenceThresholdMinutes, ct))!;

    public async Task<int> GetEarlyLeaveBufferMinutesAsync(CancellationToken ct = default)
        => (int)(await _settings.GetIntAsync("shift.early_leave_buffer_minutes", _fallback.EarlyLeaveBufferMinutes, ct))!;

    public async Task<int> GetNoShowThresholdMinutesAsync(CancellationToken ct = default)
        => (int)(await _settings.GetIntAsync("shift.no_show_threshold_minutes", _fallback.NoShowThresholdMinutes, ct))!;

    public async Task<double> GetMaxWeeklyHoursAsync(CancellationToken ct = default)
        => (double)(await _settings.GetDecimalAsync("shift.max_weekly_hours", (decimal)_fallback.MaxWeeklyHours, ct))!;

    public async Task<double?> GetGeofenceBaseLatitudeAsync(CancellationToken ct = default)
    {
        var val = await _settings.GetDecimalAsync("shift.geofence_base_latitude", null, ct);
        if (val.HasValue) return (double)val.Value;
        return _fallback.BaseLatitude;
    }

    public async Task<double?> GetGeofenceBaseLongitudeAsync(CancellationToken ct = default)
    {
        var val = await _settings.GetDecimalAsync("shift.geofence_base_longitude", null, ct);
        if (val.HasValue) return (double)val.Value;
        return _fallback.BaseLongitude;
    }

    public async Task<int> GetGeofenceMaxRadiusMetersAsync(CancellationToken ct = default)
        => (int)(await _settings.GetIntAsync("shift.geofence_max_radius_meters", _fallback.MaxRadiusMeters, ct))!;

    public async Task<int> GetAutoLogoutAfterCheckoutMinutesAsync(CancellationToken ct = default)
        => (int)(await _settings.GetIntAsync("shift.auto_logout_after_checkout_minutes", 5, ct))!;
}
