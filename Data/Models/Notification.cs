using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Data.Models.Enums;

namespace Data.Models
{
    public class Notification : BaseMongoDocument
    {
        [BsonElement("userId")]
        public Guid UserId { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("notificationType")]
        public NotificationType NotificationType { get; set; }

        [BsonElement("recipientType")]
        public RecipientType RecipientType { get; set; }

        [BsonElement("status")]
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

        [BsonElement("timeStamp")]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        [BsonElement("readAt")]
        public DateTime? ReadAt { get; set; }

        [BsonElement("acknowledgedAt")]
        public DateTime? AcknowledgedAt { get; set; }

        [BsonElement("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [BsonElement("priority")]
        public int Priority { get; set; } = 1; 

        [BsonElement("relatedEntityId")]
        public Guid? RelatedEntityId { get; set; } 

        [BsonElement("relatedEntityType")]
        public string? RelatedEntityType { get; set; } 

        [BsonElement("actionRequired")]
        public bool ActionRequired { get; set; } = false;

        [BsonElement("actionUrl")]
        public string? ActionUrl { get; set; } 

        [BsonElement("metadata")]
        public Dictionary<string, object>? Metadata { get; set; } 
    }
}
