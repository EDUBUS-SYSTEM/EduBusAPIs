using Services.Models.Dashboard;

namespace Services.Contracts
{

    public interface IDashboardService
    {
      
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(DateTime? from = null, DateTime? to = null);

        Task<DailyStudentsDto> GetDailyStudentsAsync(DateTime? date = null);

        Task<AttendanceRateDto> GetAttendanceRateAsync(string period = "today");

  
        Task<VehicleRuntimeDto> GetVehicleRuntimeAsync(Guid? vehicleId = null, DateTime? from = null, DateTime? to = null);

  
        Task<List<RouteStatisticsDto>> GetRouteStatisticsAsync(Guid? routeId = null, DateTime? from = null, DateTime? to = null);

        Task<RevenueStatisticsDto> GetRevenueStatisticsAsync(DateTime? from = null, DateTime? to = null);

        Task<ActiveSemesterDto?> GetCurrentSemesterAsync();

        Task<List<ActiveSemesterDto>> GetAllSemestersAsync();

        Task<List<RevenueTimelinePointDto>> GetRevenueTimelineAsync(DateTime? from = null, DateTime? to = null);
    }
}
