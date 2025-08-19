using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class Notification : BaseMongoDocument
    {
        [BsonElement("userId")]
        public Guid UserId { get; set; }

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("notificationType")]
        public string NotificationType { get; set; } = string.Empty;

        [BsonElement("recipientType")]
        public string RecipientType { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("timeStamp")]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
