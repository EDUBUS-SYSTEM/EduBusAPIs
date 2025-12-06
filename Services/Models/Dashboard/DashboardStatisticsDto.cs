namespace Services.Models.Dashboard
{
    /// <summary>
    /// Main DTO containing all dashboard statistics
    /// </summary>
    public class DashboardStatisticsDto
    {
        public DailyStudentsDto DailyStudents { get; set; } = new();
        public AttendanceRateDto AttendanceRate { get; set; } = new();
        public VehicleRuntimeDto VehicleRuntime { get; set; } = new();
        public List<RouteStatisticsDto> RouteStatistics { get; set; } = new();
    }
}
