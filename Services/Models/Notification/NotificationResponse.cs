using Data.Models.Enums;

namespace Services.Models.Notification
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public RecipientType RecipientType { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int Priority { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public bool ActionRequired { get; set; }
        public string? ActionUrl { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
        public bool IsRead => Status == NotificationStatus.Read || Status == NotificationStatus.Acknowledged;
    }
}
