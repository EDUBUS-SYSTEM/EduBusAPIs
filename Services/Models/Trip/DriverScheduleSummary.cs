namespace Services.Models.Trip
{
    public class DriverScheduleSummary
    {
        public Guid DriverId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTrips { get; set; }
        public int ScheduledTrips { get; set; }
        public int InProgressTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int CancelledTrips { get; set; }
        public double TotalWorkingHours { get; set; }
        public Dictionary<DateTime, int> TripsByDate { get; set; } = new Dictionary<DateTime, int>();
    }
}
