using Microsoft.AspNetCore.SignalR;
using Services.Models.Notification;
using Services.Contracts;
using APIs.Hubs;

namespace APIs.Services
{
	public class NotificationHubService : INotificationHubService
	{
		private readonly IHubContext<NotificationHub> _hubContext;
		private readonly ILogger<NotificationHubService> _logger;

		public NotificationHubService(
			IHubContext<NotificationHub> hubContext,
			ILogger<NotificationHubService> logger)
		{
			_hubContext = hubContext;
			_logger = logger;
		}

		public async Task SendNotificationToUserAsync(Guid userId, NotificationResponse notification)
		{
			try
			{
				await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
				_logger.LogInformation("Sent real-time notification to user {UserId}: {NotificationId}", userId, notification.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending real-time notification to user {UserId}", userId);
			}
		}

		public async Task SendNotificationToAdminsAsync(NotificationResponse notification)
		{
			try
			{
				await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", notification);
				_logger.LogInformation("Sent real-time notification to admins: {NotificationId}", notification.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending real-time notification to admins");
			}
		}

		public async Task SendNotificationToGroupAsync(string groupName, NotificationResponse notification)
		{
			try
			{
				await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notification);
				_logger.LogInformation("Sent real-time notification to group {GroupName}: {NotificationId}", groupName, notification.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending real-time notification to group {GroupName}", groupName);
			}
		}

		public async Task SendNotificationToAllAsync(NotificationResponse notification)
		{
			try
			{
				await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
				_logger.LogInformation("Sent real-time notification to all clients: {NotificationId}", notification.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending real-time notification to all clients");
			}
		}

		public async Task SendNotificationCountUpdateAsync(Guid userId, int unreadCount)
		{
			try
			{
				await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateNotificationCount", unreadCount);
				_logger.LogInformation("Sent notification count update to user {UserId}: {Count}", userId, unreadCount);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending notification count update to user {UserId}", userId);
			}
		}

		public async Task SendNotificationCountUpdateToAdminsAsync(int unreadCount)
		{
			try
			{
				await _hubContext.Clients.Group("Admins").SendAsync("UpdateNotificationCount", unreadCount);
				_logger.LogInformation("Sent notification count update to admins: {Count}", unreadCount);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending notification count update to admins");
			}
		}
	}
}

