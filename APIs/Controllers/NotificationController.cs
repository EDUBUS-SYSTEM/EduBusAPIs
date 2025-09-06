using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Notification;
using Data.Models.Enums;
using System.Security.Claims;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;
        private readonly INotificationHubService _hubService;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger,
            INotificationHubService hubService)
        {
            _notificationService = notificationService;
            _logger = logger;
            _hubService = hubService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID");
            }
            return userId;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationResponse>> GetNotification(string id)
        {
            try
            {
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                return Ok(notification);
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid notification ID format");
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification {NotificationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult<NotificationResponse>> MarkAsRead(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notification = await _notificationService.MarkAsReadAsync(id, userId);
                return Ok(notification);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}/acknowledge")]
        public async Task<ActionResult<NotificationResponse>> AcknowledgeNotification(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notification = await _notificationService.AcknowledgeNotificationAsync(id, userId);
                return Ok(notification);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging notification {NotificationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("mark-all-read")]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.MarkAllAsReadAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var deleted = await _notificationService.DeleteNotificationAsync(id, userId);
                
                if (!deleted)
                {
                    return NotFound();
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-type/{type}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotificationsByType(
            NotificationType type,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsByTypeAsync(type, page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications by type {Type}", type);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetAdminNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var notifications = await _notificationService.GetAdminNotificationsAsync(page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin notifications");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("action-required")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotificationsRequiringAction()
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsRequiringActionAsync();
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications requiring action");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NotificationResponse>> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(dto);
                
                // Send real-time notification
                try
                {
                    await _hubService.SendNotificationToUserAsync(dto.UserId, notification);
                    var unreadCount = await _notificationService.GetUnreadCountAsync(dto.UserId);
                    await _hubService.SendNotificationCountUpdateAsync(dto.UserId, unreadCount);
                }
                catch (Exception hubEx)
                {
                    _logger.LogWarning(hubEx, "Failed to send real-time notification for user {UserId}", dto.UserId);
                }
                
                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NotificationResponse>> CreateAdminNotification([FromBody] CreateAdminNotificationDto dto)
        {
            try
            {
                var notification = await _notificationService.CreateAdminNotificationAsync(dto);
                
                // Send real-time notification to admins
                try
                {
                    await _hubService.SendNotificationToAdminsAsync(notification);
                    var adminUnreadCount = await _notificationService.GetAdminUnreadCountAsync();
                    await _hubService.SendNotificationCountUpdateToAdminsAsync(adminUnreadCount);
                }
                catch (Exception hubEx)
                {
                    _logger.LogWarning(hubEx, "Failed to send real-time admin notification");
                }
                
                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin notification");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
