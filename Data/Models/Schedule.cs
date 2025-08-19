using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class Schedule : BaseMongoDocument
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("startTime")]
        public string StartTime { get; set; } = string.Empty;

        [BsonElement("endTime")]
        public string EndTime { get; set; } = string.Empty;

        [BsonElement("rrule")]
        public string RRule { get; set; } = string.Empty;

        [BsonElement("timezone")]
        public string Timezone { get; set; } = string.Empty;

        [BsonElement("effectiveFrom")]
        public DateTime EffectiveFrom { get; set; }

        [BsonElement("effectiveTo")]
        public DateTime? EffectiveTo { get; set; }

        [BsonElement("exceptions")]
        public List<DateTime> Exceptions { get; set; } = new List<DateTime>();

        [BsonElement("scheduleType")]
        public string ScheduleType { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
