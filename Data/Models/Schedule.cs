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

        [BsonElement("academicYear")]
        public string AcademicYear { get; set; } = string.Empty;

        [BsonElement("semesterCode")]
        public string? SemesterCode { get; set; }

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

        [BsonElement("timeOverrides")]
        public List<ScheduleTimeOverride> TimeOverrides { get; set; } = new List<ScheduleTimeOverride>();
    }

    public class ScheduleTimeOverride
    {
        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("startTime")]
        public string StartTime { get; set; } = string.Empty;

        [BsonElement("endTime")]
        public string EndTime { get; set; } = string.Empty;

        [BsonElement("reason")]
        public string Reason { get; set; } = string.Empty;

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("isCancelled")]
        public bool IsCancelled { get; set; } = false;
    }
}
