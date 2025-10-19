namespace Services.Models.Trip
{
    public class UpdateTripStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class UpdateAttendanceRequest
    {
        public Guid StopId { get; set; }
        public Guid StudentId { get; set; }
        public string State { get; set; } = string.Empty;
    }

    public class AttendanceDto
    {
        public Guid StudentId { get; set; }
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
}

