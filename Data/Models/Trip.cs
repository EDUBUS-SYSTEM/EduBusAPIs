using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class Trip : BaseMongoDocument
    {
        [BsonElement("routeId")]
        public Guid RouteId { get; set; }

        [BsonElement("serviceDate")]
        public DateTime ServiceDate { get; set; }

        [BsonElement("plannedStartAt")]
        public DateTime PlannedStartAt { get; set; }

        [BsonElement("plannedEndAt")]
        public DateTime PlannedEndAt { get; set; }

        [BsonElement("startTime")]
        public DateTime? StartTime { get; set; }

        [BsonElement("endTime")]
        public DateTime? EndTime { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("scheduleSnapshot")]
        public ScheduleSnapshot ScheduleSnapshot { get; set; } = new ScheduleSnapshot();

        [BsonElement("stops")]
        public List<TripStop> Stops { get; set; } = new List<TripStop>();

        [BsonElement("overrideInfo")]
        public OverrideInfo? OverrideInfo { get; set; }

        [BsonElement("isOverride")]
        public bool IsOverride { get; set; } = false;

        [BsonElement("overrideReason")]
        public string OverrideReason { get; set; } = string.Empty;

        [BsonElement("overrideCreatedBy")]
        public string OverrideCreatedBy { get; set; } = string.Empty;

        [BsonElement("overrideCreatedAt")]
        public DateTime OverrideCreatedAt { get; set; }
    }

    public class ScheduleSnapshot
    {
        [BsonElement("scheduleId")]
        public Guid ScheduleId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("startTime")]
        public string StartTime { get; set; } = string.Empty;

        [BsonElement("endTime")]
        public string EndTime { get; set; } = string.Empty;

        [BsonElement("rrule")]
        public string RRule { get; set; } = string.Empty;
    }

    public class OverrideInfo
    {
        [BsonElement("scheduleId")]
        public string ScheduleId { get; set; } = string.Empty;

        [BsonElement("overrideType")]
        public string OverrideType { get; set; } = string.Empty; // "TIME", "CANCELLATION", "DELAY"

        [BsonElement("originalStartTime")]
        public string OriginalStartTime { get; set; } = string.Empty;

        [BsonElement("originalEndTime")]
        public string OriginalEndTime { get; set; } = string.Empty;

        [BsonElement("newStartTime")]
        public string NewStartTime { get; set; } = string.Empty;

        [BsonElement("newEndTime")]
        public string NewEndTime { get; set; } = string.Empty;

        [BsonElement("overrideReason")]
        public string OverrideReason { get; set; } = string.Empty;

        [BsonElement("overrideCreatedAt")]
        public DateTime OverrideCreatedAt { get; set; }

        [BsonElement("overrideCreatedBy")]
        public string OverrideCreatedBy { get; set; } = string.Empty;
    }
}
