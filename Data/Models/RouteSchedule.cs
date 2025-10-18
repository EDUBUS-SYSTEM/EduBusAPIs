using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class RouteSchedule : BaseMongoDocument
    {
        [BsonElement("routeId")]
        public Guid RouteId { get; set; }

        [BsonElement("scheduleId")]
        public Guid ScheduleId { get; set; }

        [BsonElement("effectiveFrom")]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;

        [BsonElement("effectiveTo")]
        public DateTime? EffectiveTo { get; set; } = null; // null = inherit from Schedule

        [BsonElement("priority")]
        public int Priority { get; set; } = 0;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
