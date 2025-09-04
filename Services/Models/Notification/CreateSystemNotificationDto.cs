using Data.Models.Enums;

namespace Services.Models.Notification
{
    public class CreateSystemNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public RecipientType RecipientType { get; set; } = RecipientType.Admin;
        public int Priority { get; set; } = 3;
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public bool ActionRequired { get; set; } = false;
        public string? ActionUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
