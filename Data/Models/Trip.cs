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
        public string Status { get; set; } = Constants.TripStatus.Scheduled; 
 
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

        [BsonElement("currentLocation")]
        public VehicleLocation? CurrentLocation { get; set; }

        [BsonElement("summary")]
        public TripSummary? Summary { get; set; }

        [BsonElement("vehicleId")]
        public Guid VehicleId { get; set; }

        [BsonElement("driverVehicleId")]
        public Guid? DriverVehicleId { get; set; }

        [BsonElement("vehicle")]
        public VehicleSnapshot? Vehicle { get; set; }

        [BsonElement("driver")]
        public DriverSnapshot? Driver { get; set; }

        public class VehicleLocation
        {
            [BsonElement("latitude")]
            public double Latitude { get; set; }

            [BsonElement("longitude")]
            public double Longitude { get; set; }

            [BsonElement("recordedAt")]
            public DateTime RecordedAt { get; set; }

            [BsonElement("speed")]
            public double? Speed { get; set; }

            [BsonElement("accuracy")]
            public double? Accuracy { get; set; }

            [BsonElement("isMoving")]
            public bool IsMoving { get; set; }
        }

        public class TripSummary
        {
            [BsonElement("totalDistance")]
            public double TotalDistance { get; set; }

            [BsonElement("totalDuration")]
            public string TotalDurationIso { get; set; } = string.Empty;

            [BsonElement("averageSpeed")]
            public double AverageSpeed { get; set; }

            [BsonElement("maxSpeed")]
            public double? MaxSpeed { get; set; }

            [BsonElement("minSpeed")]
            public double? MinSpeed { get; set; }

            [BsonElement("plannedDistance")]
            public double PlannedDistance { get; set; }

            [BsonElement("actualDistance")]
            public double ActualDistance { get; set; }

            [BsonElement("distanceDeviation")]
            public double DistanceDeviation { get; set; }

            [BsonElement("stopsCompleted")]
            public int StopsCompleted { get; set; }

            [BsonElement("totalStops")]
            public int TotalStops { get; set; }

            [BsonElement("idleTime")]
            public string IdleTimeIso { get; set; } = string.Empty;

            [BsonElement("movingTime")]
            public string MovingTimeIso { get; set; } = string.Empty;

            [BsonElement("onTimePercentage")]
            public double OnTimePercentage { get; set; }

            [BsonElement("calculatedAt")]
            public DateTime CalculatedAt { get; set; }
        }

        public class VehicleSnapshot
        {
            [BsonElement("id")]
            public Guid Id { get; set; }

            [BsonElement("maskedPlate")]
            public string MaskedPlate { get; set; } = string.Empty;

            [BsonElement("capacity")]
            public int Capacity { get; set; }

            [BsonElement("status")]
            public string Status { get; set; } = string.Empty;
        }

        public class DriverSnapshot
        {
            [BsonElement("id")]
            public Guid Id { get; set; }

            [BsonElement("fullName")]
            public string FullName { get; set; } = string.Empty;

            [BsonElement("phone")]
            public string Phone { get; set; } = string.Empty;

            [BsonElement("isPrimary")]
            public bool IsPrimary { get; set; }

            [BsonElement("snapshottedAtUtc")]
            public DateTime SnapshottedAtUtc { get; set; }
        }
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
