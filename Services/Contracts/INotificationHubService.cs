using Services.Models.Notification;

namespace Services.Contracts
{
    public interface INotificationHubService
    {
        Task SendNotificationToUserAsync(Guid userId, NotificationResponse notification);
        Task SendNotificationToAdminsAsync(NotificationResponse notification);
        Task SendNotificationToGroupAsync(string groupName, NotificationResponse notification);
        Task SendNotificationToAllAsync(NotificationResponse notification);
        Task SendNotificationCountUpdateAsync(Guid userId, int unreadCount);
        Task SendNotificationCountUpdateToAdminsAsync(int unreadCount);

        Task SendEventToUserAsync(Guid userId, string eventName, object payload);
    }
}