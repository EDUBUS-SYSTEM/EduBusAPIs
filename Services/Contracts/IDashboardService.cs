using Services.Models.Dashboard;

namespace Services.Contracts
{
    /// <summary>
    /// Service for dashboard statistics and analytics
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Get all dashboard statistics
        /// </summary>
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Get daily student counts
        /// </summary>
        Task<DailyStudentsDto> GetDailyStudentsAsync(DateTime? date = null);

        /// <summary>
        /// Get attendance rate statistics
        /// </summary>
        /// <param name="period">today, week, or month</param>
        Task<AttendanceRateDto> GetAttendanceRateAsync(string period = "today");

        /// <summary>
        /// Get vehicle runtime statistics
        /// </summary>
        Task<VehicleRuntimeDto> GetVehicleRuntimeAsync(Guid? vehicleId = null, DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Get route statistics
        /// </summary>
        Task<List<RouteStatisticsDto>> GetRouteStatisticsAsync(Guid? routeId = null, DateTime? from = null, DateTime? to = null);
    }
}
