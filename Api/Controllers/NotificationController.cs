using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Notification;
using Core.Interface.Service.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// GET /api/notifications?skip=0&take=20&type=NEW_ORDER&unreadOnly=false
        /// Lấy danh sách thông báo cho user hiện tại (lọc theo permission)
        /// </summary>
        [HttpGet]
        [HasPermission(Permissions.ViewNotification)]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] NotificationQueryDto query,
            CancellationToken ct)
        {
            var (userId, permissions) = ExtractUserContext();

            var data = await _notificationService.GetNotificationsAsync(query, permissions, userId, ct);

            return Ok(new ApiResponse<List<NotificationListItemDto>>
            {
                Success = true,
                Code = 200,
                UserMessage = "Notifications retrieved successfully.",
                Data = data,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// GET /api/notifications/unread-count
        /// Đếm số thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        [HasPermission(Permissions.ViewNotification)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var (userId, permissions) = ExtractUserContext();

            var count = await _notificationService.GetUnreadCountAsync(permissions, userId, ct);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Code = 200,
                Data = count,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// GET /api/notifications/missed?after=2024-01-01T00:00:00Z
        /// Lấy thông báo bị lỡ khi mất kết nối SignalR
        /// </summary>
        [HttpGet("missed")]
        [HasPermission(Permissions.ViewNotification)]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMissed(
            [FromQuery] DateTime? after,
            CancellationToken ct)
        {
            var (userId, permissions) = ExtractUserContext();

            var data = await _notificationService.GetMissedAsync(permissions, userId, after, ct);

            return Ok(new ApiResponse<List<NotificationListItemDto>>
            {
                Success = true,
                Code = 200,
                Data = data,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// POST /api/notifications/{id}/read
        /// Đánh dấu đã đọc 1 thông báo
        /// </summary>
        [HttpPost("{id:long}/read")]
        [HasPermission(Permissions.ViewNotification)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAsRead(long id, CancellationToken ct)
        {
            var (userId, _) = ExtractUserContext();

            await _notificationService.MarkAsReadAsync(id, userId, ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Notification marked as read.",
                Data = null!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// POST /api/notifications/mark-all-read
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        [HttpPost("mark-all-read")]
        [HasPermission(Permissions.ViewNotification)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            var (userId, permissions) = ExtractUserContext();

            await _notificationService.MarkAllReadAsync(permissions, userId, ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "All notifications marked as read.",
                Data = null!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// POST /api/notifications/{id}/ack
        /// Xác nhận đã nhận thông báo quan trọng
        /// </summary>
        [HttpPost("{id:long}/ack")]
        [HasPermission(Permissions.AcknowledgeNotification)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Acknowledge(long id, CancellationToken ct)
        {
            var (userId, _) = ExtractUserContext();

            await _notificationService.AcknowledgeAsync(id, userId, ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Notification acknowledged.",
                Data = null!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Trích xuất userId và permissions từ JWT claims
        /// </summary>
        private (long userId, List<string> permissions) ExtractUserContext()
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user_id claim.");
            }

            var permissions = User.FindAll("permission")
                .Select(c => c.Value)
                .ToList();

            return (userId, permissions);
        }
    }
}
