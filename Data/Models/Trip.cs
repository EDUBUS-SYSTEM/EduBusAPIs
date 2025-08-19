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
}
