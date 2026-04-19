using Core.DTO.Notification;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Notification;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — NotificationService operations.
/// Code Module : Core/Service/NotificationService.cs
/// Methods     : PublishAsync, GetNotificationsAsync, GetUnreadCountAsync, GetMissedAsync,
///               GetPreferencesAsync, UpdatePreferencesAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : System publishes notifications to real-time channels and persists them,
///               users query their notification history and unread count, retrieve missed
///               notifications after reconnect, and manage per-type sound and enable preferences.
/// </summary>
public class NotificationServiceTests
{
    // ── Mocks ──
    private readonly Mock<INotificationRepository>         _repoMock        = new();
    private readonly Mock<INotificationRealtimePublisher>  _publisherMock   = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<NotificationService>> _loggerMock = new();

    // ── Factory ──
    private NotificationService CreateService() => new(
        _repoMock.Object,
        _publisherMock.Object,
        _loggerMock.Object);

    // ── Helpers ──
    private static PublishNotificationRequest MakePublishRequest(
        List<string>? targetPermissions = null,
        List<long>?   targetUserIds     = null) => new()
    {
        Type               = nameof(NotificationType.NEW_ORDER),
        Title              = "New Order",
        Body               = "Table 5 placed a new order.",
        Priority           = nameof(NotificationPriority.Normal),
        ActionUrl          = "/dashboard/orders/1",
        EntityType         = "Order",
        EntityId           = "1",
        TargetPermissions  = targetPermissions ?? new List<string>(),
        TargetUserIds      = targetUserIds     ?? new List<long>(),
    };

    // ═══════════════════════════════════════════════════════════════════════
    // PublishAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region PublishAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "PublishAsync")]
    public async Task PublishAsync_WhenTargetPermissions_PersistsNotificationAndPushesToPermissionGroup()
    {
        // Arrange
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishToPermissionsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = MakePublishRequest(targetPermissions: new List<string> { "permission.view_orders" });
        var service  = CreateService();

        // Act
        await service.PublishAsync(request);

        // Assert
        _repoMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n => n.Type == nameof(NotificationType.NEW_ORDER)),
            It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishToPermissionsAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<NotificationDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "PublishAsync")]
    public async Task PublishAsync_WhenTargetUserIds_PushesToEachSpecificUser()
    {
        // Arrange
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishToUserAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = MakePublishRequest(targetUserIds: new List<long> { 101L, 102L });
        var service  = CreateService();

        // Act
        await service.PublishAsync(request);

        // Assert
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishToUserAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "PublishAsync")]
    public async Task PublishAsync_WhenRealTimePushFails_StillPersistsNotificationAndDoesNotThrow()
    {
        // Arrange
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishToPermissionsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SignalR hub unavailable"));

        var request = MakePublishRequest(targetPermissions: new List<string> { "permission.view_orders" });
        var service  = CreateService();

        // Act
        var act = () => service.PublishAsync(request);

        // Assert — real-time failure is swallowed; notification DB record still saved
        await act.Should().NotThrowAsync();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "PublishAsync")]
    public async Task PublishAsync_WhenNoTargets_BroadcastsToAllConnectedClients()
    {
        // Arrange
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishToAllAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = MakePublishRequest(); // no permissions, no user IDs
        var service  = CreateService();

        // Act
        await service.PublishAsync(request);

        // Assert
        _publisherMock.Verify(p => p.PublishToAllAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishToPermissionsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.PublishToUserAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetNotificationsAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetNotificationsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetNotificationsAsync")]
    public async Task GetNotificationsAsync_WhenCalled_DelegatesQueryToRepositoryAndReturnsItems()
    {
        // Arrange
        var query       = new NotificationQueryDto { Skip = 0, Take = 20 };
        var permissions = new List<string> { "permission.view_orders" };
        var items = new List<NotificationListItemDto>
        {
            new() { Id = 1, Type = "NEW_ORDER",  Title = "New Order",  IsRead = false },
            new() { Id = 2, Type = "LOW_STOCK_ALERT", Title = "Low Stock", IsRead = true },
        };
        _repoMock.Setup(r => r.GetByUserAsync(permissions, 100L, query, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var service = CreateService();

        // Act
        var result = await service.GetNotificationsAsync(query, permissions, userId: 100L);

        // Assert
        result.Should().HaveCount(2);
        result[0].Type.Should().Be("NEW_ORDER");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetNotificationsAsync")]
    public async Task GetNotificationsAsync_WhenNoNotifications_ReturnsEmptyList()
    {
        // Arrange
        var query       = new NotificationQueryDto { Skip = 0, Take = 20 };
        var permissions = new List<string>();
        _repoMock.Setup(r => r.GetByUserAsync(permissions, 200L, query, It.IsAny<CancellationToken>())).ReturnsAsync(new List<NotificationListItemDto>());

        var service = CreateService();

        // Act
        var result = await service.GetNotificationsAsync(query, permissions, userId: 200L);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetUnreadCountAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetUnreadCountAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetUnreadCountAsync")]
    public async Task GetUnreadCountAsync_WhenUserHasUnreadNotifications_ReturnsCount()
    {
        // Arrange
        var permissions = new List<string> { "permission.view_orders" };
        _repoMock.Setup(r => r.GetUnreadCountAsync(permissions, 100L, It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var service = CreateService();

        // Act
        var result = await service.GetUnreadCountAsync(permissions, userId: 100L);

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetUnreadCountAsync")]
    public async Task GetUnreadCountAsync_WhenNoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        var permissions = new List<string>();
        _repoMock.Setup(r => r.GetUnreadCountAsync(permissions, 200L, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var service = CreateService();

        // Act
        var result = await service.GetUnreadCountAsync(permissions, userId: 200L);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetMissedAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetMissedAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMissedAsync")]
    public async Task GetMissedAsync_WhenAfterUtcProvided_ReturnsMissedNotificationsAfterDate()
    {
        // Arrange
        var permissions = new List<string> { "permission.view_orders" };
        var afterUtc    = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc);
        var missed = new List<NotificationListItemDto>
        {
            new() { Id = 5, Type = "SHIFT_ASSIGNED", Title = "Shift Assigned" }
        };
        _repoMock.Setup(r => r.GetMissedAsync(permissions, 100L, afterUtc, It.IsAny<CancellationToken>())).ReturnsAsync(missed);

        var service = CreateService();

        // Act
        var result = await service.GetMissedAsync(permissions, userId: 100L, afterUtc: afterUtc);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be("SHIFT_ASSIGNED");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetMissedAsync")]
    public async Task GetMissedAsync_WhenAfterUtcIsNull_DelegatesNullAndReturnsAll()
    {
        // Arrange
        var permissions = new List<string> { "permission.view_orders" };
        var missed = new List<NotificationListItemDto>
        {
            new() { Id = 1, Type = "NEW_ORDER" },
            new() { Id = 2, Type = "LOW_STOCK_ALERT" }
        };
        _repoMock.Setup(r => r.GetMissedAsync(permissions, 100L, null, It.IsAny<CancellationToken>())).ReturnsAsync(missed);

        var service = CreateService();

        // Act
        var result = await service.GetMissedAsync(permissions, userId: 100L, afterUtc: null);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetPreferencesAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetPreferencesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPreferencesAsync")]
    public async Task GetPreferencesAsync_WhenUserHasSomePreferences_MergesWithDefaults()
    {
        // Arrange
        var stored = new List<NotificationPreference>
        {
            new() { UserId = 100L, NotificationType = "NEW_ORDER", IsEnabled = false, SoundEnabled = false }
        };
        _repoMock.Setup(r => r.GetPreferencesAsync(100L, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var service = CreateService();

        // Act
        var result = await service.GetPreferencesAsync(userId: 100L);

        // Assert — all enum types are returned; the stored one is overridden
        var total = System.Enum.GetNames(typeof(NotificationType)).Length;
        result.Should().HaveCount(total);
        var newOrderPref = result.Single(p => p.NotificationType == "NEW_ORDER");
        newOrderPref.IsEnabled.Should().BeFalse();
        newOrderPref.SoundEnabled.Should().BeFalse();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPreferencesAsync")]
    public async Task GetPreferencesAsync_WhenNoPreferencesStored_ReturnsAllTypesWithDefaultEnabled()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPreferencesAsync(200L, It.IsAny<CancellationToken>())).ReturnsAsync(new List<NotificationPreference>());

        var service = CreateService();

        // Act
        var result = await service.GetPreferencesAsync(userId: 200L);

        // Assert — every type defaults to IsEnabled=true, SoundEnabled=true
        var total = System.Enum.GetNames(typeof(NotificationType)).Length;
        result.Should().HaveCount(total);
        result.Should().AllSatisfy(p =>
        {
            p.IsEnabled.Should().BeTrue();
            p.SoundEnabled.Should().BeTrue();
        });
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // UpdatePreferencesAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region UpdatePreferencesAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdatePreferencesAsync")]
    public async Task UpdatePreferencesAsync_WhenCalled_UpsertsPreferencesForUser()
    {
        // Arrange
        _repoMock.Setup(r => r.UpsertPreferencesAsync(100L, It.IsAny<List<NotificationPreference>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = new UpdateNotificationPreferencesRequest
        {
            Preferences = new List<NotificationPreferenceItemRequest>
            {
                new() { NotificationType = "NEW_ORDER",      IsEnabled = true,  SoundEnabled = true  },
                new() { NotificationType = "LOW_STOCK_ALERT", IsEnabled = false, SoundEnabled = false },
            }
        };
        var service = CreateService();

        // Act
        await service.UpdatePreferencesAsync(userId: 100L, request);

        // Assert
        _repoMock.Verify(r => r.UpsertPreferencesAsync(
            100L,
            It.Is<List<NotificationPreference>>(prefs => prefs.Count == 2 && prefs[0].UserId == 100L),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdatePreferencesAsync")]
    public async Task UpdatePreferencesAsync_WhenEmptyList_StillCallsUpsertWithEmptyCollection()
    {
        // Arrange
        _repoMock.Setup(r => r.UpsertPreferencesAsync(200L, It.IsAny<List<NotificationPreference>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = new UpdateNotificationPreferencesRequest
        {
            Preferences = new List<NotificationPreferenceItemRequest>() // empty
        };
        var service = CreateService();

        // Act
        await service.UpdatePreferencesAsync(userId: 200L, request);

        // Assert
        _repoMock.Verify(r => r.UpsertPreferencesAsync(
            200L,
            It.Is<List<NotificationPreference>>(prefs => prefs.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
