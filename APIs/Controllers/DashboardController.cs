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

        /// <summary>
        /// Get all dashboard statistics
        /// </summary>
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

        /// <summary>
        /// Get daily student counts
        /// </summary>
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

        /// <summary>
        /// Get attendance rate statistics
        /// </summary>
        /// <param name="period">today, week, or month</param>
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

        /// <summary>
        /// Get vehicle runtime statistics
        /// </summary>
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

        /// <summary>
        /// Get route statistics
        /// </summary>
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
    }
}
