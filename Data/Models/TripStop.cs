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

        [BsonElement("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("boardStatus")]
        public string? BoardStatus { get; set; }

        [BsonElement("boardedAt")]
        public DateTime? BoardedAt { get; set; }

        [BsonElement("alightStatus")]
        public string? AlightStatus { get; set; }

        [BsonElement("alightedAt")]
        public DateTime? AlightedAt { get; set; }

        [BsonElement("state")]
        public string State { get; set; } = null!;

        [BsonElement("recognitionMethod")]
        public string? RecognitionMethod { get; set; }

        [BsonElement("faceRecognitionData")]
        public FaceRecognitionData? FaceRecognitionData { get; set; }
    }

    public class FaceRecognitionData
    {
        [BsonElement("similarity")]
        public double Similarity { get; set; }

        [BsonElement("livenessScore")]
        public double LivenessScore { get; set; }

        [BsonElement("framesConfirmed")]
        public int FramesConfirmed { get; set; }

        [BsonElement("deviceId")]
        public string DeviceId { get; set; } = null!;

        [BsonElement("modelVersion")]
        public string ModelVersion { get; set; } = Constants.TripConstants.FaceRecognitionConstants.ModelVersions.MobileFaceNet_V1;

        [BsonElement("recognizedAt")]
        public DateTime RecognizedAt { get; set; } = DateTime.UtcNow;
    }
}
