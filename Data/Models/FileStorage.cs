using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class FileStorage : BaseMongoDocument
    {
        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;

        [BsonElement("originalFileName")]
        public string OriginalFileName { get; set; } = string.Empty;

        [BsonElement("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [BsonElement("fileSize")]
        public long FileSize { get; set; }

        [BsonElement("fileContent")]
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        [BsonElement("fileType")]
        public string FileType { get; set; } = string.Empty; // "UserPhoto", "HealthCertificate", "LicenseImage"

        [BsonElement("entityId")]
        public Guid EntityId { get; set; } // ID của entity liên quan (Driver, UserAccount, etc.)

        [BsonElement("entityType")]
        public string EntityType { get; set; } = string.Empty; // "Driver", "UserAccount", etc.

        [BsonElement("uploadedBy")]
        public Guid UploadedBy { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
