namespace Services.Models.Dashboard
{

    public class RouteStatisticsDto
    {
        public Guid RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public int TotalTrips { get; set; }
        public int TotalStudents { get; set; }
        public double AttendanceRate { get; set; }
        public double AverageRuntime { get; set; } // in hours
        public int ActiveVehicles { get; set; }
    }
}
