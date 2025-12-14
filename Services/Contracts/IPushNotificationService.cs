using Services.Models.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
    public interface IPushNotificationService
    {
        Task<bool> SendPushNotificationAsync(Guid userId, NotificationResponse notification);
        Task<bool> SendPushNotificationToTokenAsync(string deviceToken, NotificationResponse notification);
        Task<bool> SendPushNotificationToMultipleUsersAsync(IEnumerable<Guid> userIds, NotificationResponse notification);
    }
}
