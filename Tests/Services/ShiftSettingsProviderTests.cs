using Core.Data;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Shift;
using Core.Service;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — ShiftSettingsProvider
/// Code Module : Core/Service/ShiftSettingsProvider.cs
/// Method      : GetAllowedEarlyCheckInMinutesAsync, GetLateGraceMinutesAsync,
///               GetAbsenceThresholdMinutesAsync, GetEarlyLeaveBufferMinutesAsync,
///               GetNoShowThresholdMinutesAsync, GetMaxWeeklyHoursAsync,
///               GetGeofenceBaseLatitudeAsync, GetGeofenceBaseLongitudeAsync,
///               GetGeofenceMaxRadiusMetersAsync, GetAutoLogoutAfterCheckoutMinutesAsync
/// Created By  : Automation
/// Executed By : Test Runner
/// Test Req.   : Staff operations manager configures shift/attendance thresholds that are
///               read from the system settings table at runtime, falling back to
///               appsettings.json defaults when no database override exists.
/// </summary>
public class ShiftSettingsProviderTests
{
    // ── Mocks ──
    private readonly Mock<ISystemSettingService> _settingsMock = new();
    private readonly AttendanceOptions _fallback = new()
    {
        AllowedEarlyCheckInMinutes = 120,
        LateGraceMinutes = 5,
        AbsenceThresholdMinutes = 30,
        EarlyLeaveBufferMinutes = 5,
        NoShowThresholdMinutes = 15,
        MaxWeeklyHours = 48,
        BaseLatitude = 10.762622,
        BaseLongitude = 106.660172,
        MaxRadiusMeters = 200
    };

    // ── Factory ──
    private ShiftSettingsProvider CreateService() => new(
        _settingsMock.Object,
        Options.Create(_fallback));

    // ══════════════════════════════════════════════════════════════
    // GetAllowedEarlyCheckInMinutesAsync
    // ══════════════════════════════════════════════════════════════

    #region GetAllowedEarlyCheckInMinutesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllowedEarlyCheckInMinutesAsync")]
    public async Task GetAllowedEarlyCheckInMinutesAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.allowed_early_check_in_minutes", 120, It.IsAny<CancellationToken>()))
            .ReturnsAsync(60L);

        var service = CreateService();

        // Act
        var result = await service.GetAllowedEarlyCheckInMinutesAsync();

        // Assert
        result.Should().Be(60);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllowedEarlyCheckInMinutesAsync")]
    public async Task GetAllowedEarlyCheckInMinutesAsync_WhenNoDbOverride_ReturnsFallback()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.allowed_early_check_in_minutes", 120, It.IsAny<CancellationToken>()))
            .ReturnsAsync(120L);

        var service = CreateService();

        // Act
        var result = await service.GetAllowedEarlyCheckInMinutesAsync();

        // Assert
        result.Should().Be(120);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllowedEarlyCheckInMinutesAsync")]
    public async Task GetAllowedEarlyCheckInMinutesAsync_WhenDbReturnsZero_ReturnsZero()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.allowed_early_check_in_minutes", 120, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var service = CreateService();

        // Act
        var result = await service.GetAllowedEarlyCheckInMinutesAsync();

        // Assert
        result.Should().Be(0);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetLateGraceMinutesAsync
    // ══════════════════════════════════════════════════════════════

    #region GetLateGraceMinutesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetLateGraceMinutesAsync")]
    public async Task GetLateGraceMinutesAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.late_grace_minutes", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10L);

        var service = CreateService();

        // Act
        var result = await service.GetLateGraceMinutesAsync();

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetLateGraceMinutesAsync")]
    public async Task GetLateGraceMinutesAsync_WhenNoDbOverride_ReturnsFallback()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.late_grace_minutes", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5L);

        var service = CreateService();

        // Act
        var result = await service.GetLateGraceMinutesAsync();

        // Assert
        result.Should().Be(5);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetAbsenceThresholdMinutesAsync
    // ══════════════════════════════════════════════════════════════

    #region GetAbsenceThresholdMinutesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAbsenceThresholdMinutesAsync")]
    public async Task GetAbsenceThresholdMinutesAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.absence_threshold_minutes", 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(45L);

        var service = CreateService();

        // Act
        var result = await service.GetAbsenceThresholdMinutesAsync();

        // Assert
        result.Should().Be(45);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetEarlyLeaveBufferMinutesAsync
    // ══════════════════════════════════════════════════════════════

    #region GetEarlyLeaveBufferMinutesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetEarlyLeaveBufferMinutesAsync")]
    public async Task GetEarlyLeaveBufferMinutesAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.early_leave_buffer_minutes", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15L);

        var service = CreateService();

        // Act
        var result = await service.GetEarlyLeaveBufferMinutesAsync();

        // Assert
        result.Should().Be(15);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetNoShowThresholdMinutesAsync
    // ══════════════════════════════════════════════════════════════

    #region GetNoShowThresholdMinutesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetNoShowThresholdMinutesAsync")]
    public async Task GetNoShowThresholdMinutesAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.no_show_threshold_minutes", 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20L);

        var service = CreateService();

        // Act
        var result = await service.GetNoShowThresholdMinutesAsync();

        // Assert
        result.Should().Be(20);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetMaxWeeklyHoursAsync
    // ══════════════════════════════════════════════════════════════

    #region GetMaxWeeklyHoursAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMaxWeeklyHoursAsync")]
    public async Task GetMaxWeeklyHoursAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.max_weekly_hours", 48m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(40m);

        var service = CreateService();

        // Act
        var result = await service.GetMaxWeeklyHoursAsync();

        // Assert
        result.Should().Be(40.0);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMaxWeeklyHoursAsync")]
    public async Task GetMaxWeeklyHoursAsync_WhenNoDbOverride_ReturnsFallback()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.max_weekly_hours", 48m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(48m);

        var service = CreateService();

        // Act
        var result = await service.GetMaxWeeklyHoursAsync();

        // Assert
        result.Should().Be(48.0);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetMaxWeeklyHoursAsync")]
    public async Task GetMaxWeeklyHoursAsync_WhenDbReturnsDecimalFraction_PreservesDecimal()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.max_weekly_hours", 48m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(36.5m);

        var service = CreateService();

        // Act
        var result = await service.GetMaxWeeklyHoursAsync();

        // Assert
        result.Should().Be(36.5);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetGeofenceBaseLatitudeAsync
    // ══════════════════════════════════════════════════════════════

    #region GetGeofenceBaseLatitudeAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGeofenceBaseLatitudeAsync")]
    public async Task GetGeofenceBaseLatitudeAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.geofence_base_latitude", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(21.028511m);

        var service = CreateService();

        // Act
        var result = await service.GetGeofenceBaseLatitudeAsync();

        // Assert
        result.Should().BeApproximately(21.028511, 0.000001);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGeofenceBaseLatitudeAsync")]
    public async Task GetGeofenceBaseLatitudeAsync_WhenNoDbOverride_ReturnsFallback()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.geofence_base_latitude", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        var service = CreateService();

        // Act
        var result = await service.GetGeofenceBaseLatitudeAsync();

        // Assert
        result.Should().Be(_fallback.BaseLatitude);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetGeofenceBaseLatitudeAsync")]
    public async Task GetGeofenceBaseLatitudeAsync_WhenNoDbOverrideAndNoFallback_ReturnsNull()
    {
        // Arrange
        _fallback.BaseLatitude = null;

        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.geofence_base_latitude", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        var service = CreateService();

        // Act
        var result = await service.GetGeofenceBaseLatitudeAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetGeofenceBaseLongitudeAsync
    // ══════════════════════════════════════════════════════════════

    #region GetGeofenceBaseLongitudeAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGeofenceBaseLongitudeAsync")]
    public async Task GetGeofenceBaseLongitudeAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.geofence_base_longitude", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(105.834160m);

        var service = CreateService();

        // Act
        var result = await service.GetGeofenceBaseLongitudeAsync();

        // Assert
        result.Should().BeApproximately(105.834160, 0.000001);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGeofenceBaseLongitudeAsync")]
    public async Task GetGeofenceBaseLongitudeAsync_WhenNoDbOverride_ReturnsFallback()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetDecimalAsync("shift.geofence_base_longitude", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        var service = CreateService();

        // Act
        var result = await service.GetGeofenceBaseLongitudeAsync();

        // Assert
        result.Should().Be(_fallback.BaseLongitude);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetGeofenceMaxRadiusMetersAsync
    // ══════════════════════════════════════════════════════════════

    #region GetGeofenceMaxRadiusMetersAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGeofenceMaxRadiusMetersAsync")]
    public async Task GetGeofenceMaxRadiusMetersAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.geofence_max_radius_meters", 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500L);

        var service = CreateService();

        // Act
        var result = await service.GetGeofenceMaxRadiusMetersAsync();

        // Assert
        result.Should().Be(500);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetAutoLogoutAfterCheckoutMinutesAsync
    // ══════════════════════════════════════════════════════════════

    #region GetAutoLogoutAfterCheckoutMinutesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAutoLogoutAfterCheckoutMinutesAsync")]
    public async Task GetAutoLogoutAfterCheckoutMinutesAsync_WhenDbHasOverride_ReturnsDbValue()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.auto_logout_after_checkout_minutes", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10L);

        var service = CreateService();

        // Act
        var result = await service.GetAutoLogoutAfterCheckoutMinutesAsync();

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAutoLogoutAfterCheckoutMinutesAsync")]
    public async Task GetAutoLogoutAfterCheckoutMinutesAsync_WhenNoDbOverride_ReturnsHardcodedDefault()
    {
        // Arrange — note: fallback default is hardcoded as 5 in ShiftSettingsProvider
        _settingsMock
            .Setup(s => s.GetIntAsync("shift.auto_logout_after_checkout_minutes", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5L);

        var service = CreateService();

        // Act
        var result = await service.GetAutoLogoutAfterCheckoutMinutesAsync();

        // Assert
        result.Should().Be(5);
    }

    #endregion
}
