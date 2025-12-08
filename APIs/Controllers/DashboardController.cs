using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Constants;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetDashboardStatistics(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var statistics = await _dashboardService.GetDashboardStatisticsAsync(from, to);
                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving dashboard statistics." });
            }
        }

      
        [HttpGet("daily-students")]
        public async Task<ActionResult<object>> GetDailyStudents([FromQuery] DateTime? date = null)
        {
            try
            {
                var dailyStudents = await _dashboardService.GetDailyStudentsAsync(date);
                return Ok(new { success = true, data = dailyStudents });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving daily students." });
            }
        }

      
        [HttpGet("attendance-rate")]
        public async Task<ActionResult<object>> GetAttendanceRate([FromQuery] string period = "today")
        {
            try
            {
                var attendanceRate = await _dashboardService.GetAttendanceRateAsync(period);
                return Ok(new { success = true, data = attendanceRate });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving attendance rate." });
            }
        }

        [HttpGet("vehicle-runtime")]
        public async Task<ActionResult<object>> GetVehicleRuntime(
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var vehicleRuntime = await _dashboardService.GetVehicleRuntimeAsync(vehicleId, from, to);
                return Ok(new { success = true, data = vehicleRuntime });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving vehicle runtime." });
            }
        }


        [HttpGet("route-statistics")]
        public async Task<ActionResult<object>> GetRouteStatistics(
            [FromQuery] Guid? routeId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var routeStatistics = await _dashboardService.GetRouteStatisticsAsync(routeId, from, to);
                return Ok(new { success = true, data = routeStatistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving route statistics." });
            }
        }

        [HttpGet("revenue")]
        public async Task<ActionResult<object>> GetRevenueStatistics(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var revenueStatistics = await _dashboardService.GetRevenueStatisticsAsync(from, to);
                return Ok(new { success = true, data = revenueStatistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving revenue statistics." });
            }
        }

        [HttpGet("revenue/timeline")]
        public async Task<ActionResult<object>> GetRevenueTimeline(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var timeline = await _dashboardService.GetRevenueTimelineAsync(from, to);
                return Ok(new { success = true, data = timeline });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving revenue timeline." });
            }
        }

        [HttpGet("current-semester")]
        public async Task<ActionResult<object>> GetCurrentSemester()
        {
            try
            {
                var semester = await _dashboardService.GetCurrentSemesterAsync();
                if (semester == null)
                {
                    return NotFound(new { success = false, error = "No active semester found" });
                }

                return Ok(new { success = true, data = semester });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving current semester." });
            }
        }
    }
}
