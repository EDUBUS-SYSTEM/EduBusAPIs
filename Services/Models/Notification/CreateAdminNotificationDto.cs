using Data.Models.Enums;

namespace Services.Models.Notification
{
    public class CreateAdminNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public int Priority { get; set; } = 2;
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public bool ActionRequired { get; set; } = true;
        public string? ActionUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
