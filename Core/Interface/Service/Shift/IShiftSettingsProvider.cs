using Core.Data;

namespace Core.Interface.Service.Shift;

/// <summary>
/// Provides attendance / shift configuration values at runtime.
/// Reads from <c>systemsetting</c> table with <see cref="AttendanceOptions"/> fallback.
/// </summary>
public interface IShiftSettingsProvider
{
    Task<int> GetAllowedEarlyCheckInMinutesAsync(CancellationToken ct = default);
    Task<int> GetLateGraceMinutesAsync(CancellationToken ct = default);
    Task<int> GetAbsenceThresholdMinutesAsync(CancellationToken ct = default);
    Task<int> GetEarlyLeaveBufferMinutesAsync(CancellationToken ct = default);
    Task<int> GetNoShowThresholdMinutesAsync(CancellationToken ct = default);
    Task<double> GetMaxWeeklyHoursAsync(CancellationToken ct = default);
    Task<double?> GetGeofenceBaseLatitudeAsync(CancellationToken ct = default);
    Task<double?> GetGeofenceBaseLongitudeAsync(CancellationToken ct = default);
    Task<int> GetGeofenceMaxRadiusMetersAsync(CancellationToken ct = default);
    Task<int> GetAutoLogoutAfterCheckoutMinutesAsync(CancellationToken ct = default);
}
