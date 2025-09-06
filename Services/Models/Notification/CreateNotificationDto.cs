using Data.Models.Enums;

namespace Services.Models.Notification
{
    public class CreateNotificationDto
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public RecipientType RecipientType { get; set; }
        public int Priority { get; set; } = 1;
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public bool ActionRequired { get; set; } = false;
        public string? ActionUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
