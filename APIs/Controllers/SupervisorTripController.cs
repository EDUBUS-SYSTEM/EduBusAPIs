using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Trip;
using Constants;
using System.Security.Claims;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/supervisor/trips")]
    [Authorize(Roles = Roles.Supervisor)]
    public class SupervisorTripController : ControllerBase
    {
        private readonly ISupervisorTripService _supervisorTripService;
        private readonly ILogger<SupervisorTripController> _logger;

        public SupervisorTripController(ISupervisorTripService supervisorTripService, ILogger<SupervisorTripController> logger)
        {
            _supervisorTripService = supervisorTripService;
            _logger = logger;
        }

        /// <summary>
        /// Get the list of trips assigned to the current supervisor.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetMyTrips(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var supervisorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (supervisorIdClaim == null)
                    return Unauthorized(new { success = false, error = "SUPERVISOR_ID_NOT_FOUND" });

                Guid supervisorId = Guid.Parse(supervisorIdClaim.Value);

                var trips = await _supervisorTripService.GetSupervisorTripsAsync(supervisorId, dateFrom, dateTo, status);

                return Ok(new
                {
                    success = true,
                    data = trips,
                    count = trips.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supervisor trips");
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving trips." });
            }
        }

        /// <summary>
        /// Get trips scheduled for today for the current supervisor.
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<object>> GetTodayTrips()
        {
            try
            {
                var supervisorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (supervisorIdClaim == null)
                    return Unauthorized(new { success = false, error = "SUPERVISOR_ID_NOT_FOUND" });

                Guid supervisorId = Guid.Parse(supervisorIdClaim.Value);

                var trips = await _supervisorTripService.GetTodayTripsAsync(supervisorId);

                return Ok(new
                {
                    success = true,
                    data = trips,
                    count = trips.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's supervisor trips");
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving today's trips." });
            }
        }

        /// <summary>
        /// Get trip details for the current supervisor (with limited payload).
        /// </summary>
        [HttpGet("{tripId}")]
        public async Task<ActionResult<object>> GetTripDetail(Guid tripId)
        {
            try
            {
                var supervisorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (supervisorIdClaim == null)
                    return Unauthorized(new { success = false, error = "SUPERVISOR_ID_NOT_FOUND" });

                Guid supervisorId = Guid.Parse(supervisorIdClaim.Value);

                var trip = await _supervisorTripService.GetSupervisorTripDetailAsync(tripId, supervisorId);
                if (trip == null)
                    return Forbid();

                return Ok(new
                {
                    success = true,
                    data = trip
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trip detail");
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving trip detail." });
            }
        }

    }
}
