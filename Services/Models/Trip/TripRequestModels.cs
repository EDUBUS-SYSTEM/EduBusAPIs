using System.ComponentModel.DataAnnotations;

namespace Services.Models.Trip
{
    public class UpdateTripStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class UpdateAttendanceRequest
    {
        public Guid? StopId { get; set; }
        public Guid StudentId { get; set; }
        public string State { get; set; } = string.Empty;
    }

    public class AttendanceDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime? BoardedAt { get; set; }
        public string State { get; set; } = string.Empty;
    }

    public class TripStatusSummaryDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class TripAnalyticsDto
    {
        public int TotalTrips { get; set; }
        public int ScheduledTrips { get; set; }
        public int InProgressTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int CancelledTrips { get; set; }
        public int DelayedTrips { get; set; }
        public double OnTimePercentage { get; set; }
        public double CompletionRate { get; set; }
    }

    public class UpdateTripLocationRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public double? Accuracy { get; set; }
        public bool IsMoving { get; set; }
    }

	public class ArrangeStopRequest
	{
        [Required]
		public Guid PickupPointId { get; set; }
        [Required]
		public int NewSequenceOrder { get; set; }
	}

	public class UpdateStopSequenceItem
	{
        [Required]
		public Guid PickupPointId { get; set; }
        [Required]
		public int SequenceOrder { get; set; }
	}

	public class UpdateMultipleStopsSequenceRequest
	{
		public List<UpdateStopSequenceItem> Stops { get; set; } = new List<UpdateStopSequenceItem>();
	}

	public class ManualAttendanceRequest
	{
		[Required]
		public int StopId { get; set; }
		[Required]
		public Guid StudentId { get; set; }
		public string? BoardStatus { get; set; }
		public string? AlightStatus { get; set; }
		public DateTime? Timestamp { get; set; }
	}

}

