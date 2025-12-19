using Data.Repos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Contracts;
using Services.Models.Notification;
using System.Text;
using System.Text.Json;

namespace Services.Implementations
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly HttpClient _httpClient;
        private const string ExpoPushApiUrl = "https://exp.host/--/api/v2/push/send";

        public PushNotificationService(
            IServiceScopeFactory scopeFactory,
            ILogger<PushNotificationService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> SendPushNotificationAsync(Guid userId, NotificationResponse notification)
        {
            using var scope = _scopeFactory.CreateScope();
            var deviceTokenRepository = scope.ServiceProvider.GetRequiredService<IDeviceTokenRepository>();

            try
            {
                var deviceTokens = await deviceTokenRepository.GetByUserIdAsync(userId);
                var activeTokens = deviceTokens.Where(t => t.IsActive).ToList();

                if (!activeTokens.Any())
                {
                    _logger.LogWarning("No active device tokens found for user {UserId}", userId);
                    return false;
                }

                var tasks = activeTokens.Select(token =>
                    SendPushNotificationToTokenAsync(token.Token, notification));

                var results = await Task.WhenAll(tasks);
                return results.Any(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SendPushNotificationToTokenAsync(string deviceToken, NotificationResponse notification)
        {
            try
            {
                var pushMessage = new
                {
                    to = deviceToken,
                    sound = "default",
                    title = notification.Title,
                    body = notification.Message,
                    data = new
                    {
                        notificationId = notification.Id,
                        type = notification.NotificationType.ToString(),
                        metadata = notification.Metadata ?? new Dictionary<string, object>()
                    },
                    priority = "high",
                    channelId = "default"
                };

                var messages = new[] { pushMessage };
                var json = JsonSerializer.Serialize(messages);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ExpoPushApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Push notification sent successfully to token {Token}", deviceToken);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send push notification. Status: {Status}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to token {Token}", deviceToken);
                return false;
            }
        }

        public async Task<bool> SendPushNotificationToMultipleUsersAsync(
            IEnumerable<Guid> userIds,
            NotificationResponse notification)
        {
            // Each call to SendPushNotificationAsync will create its own scope,
            // so multiple users can be processed concurrently without DbContext conflicts
            var tasks = userIds.Select(userId => SendPushNotificationAsync(userId, notification));
            var results = await Task.WhenAll(tasks);
            return results.Any(r => r);
        }
    }
}
