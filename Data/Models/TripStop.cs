using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class TripStop
    {
        [BsonElement("sequenceOrder")]
        public int SequenceOrder { get; set; }

        [BsonElement("pickupPointId")]
        public Guid PickupPointId { get; set; }

        [BsonElement("plannedAt")]
        public DateTime PlannedAt { get; set; }

        [BsonElement("arrivedAt")]
        public DateTime? ArrivedAt { get; set; }

        [BsonElement("departedAt")]
        public DateTime? DepartedAt { get; set; }

        [BsonElement("location")]
        public LocationInfo Location { get; set; } = new LocationInfo();

        [BsonElement("attendance")]
        public List<Attendance> Attendance { get; set; } = new List<Attendance>();
    }

    public class Attendance
    {
        [BsonElement("studentId")]
        public Guid StudentId { get; set; }

        [BsonElement("boardedAt")]
        public DateTime? BoardedAt { get; set; }

        [BsonElement("state")]
        public string State { get; set; } = string.Empty;
    }
}
