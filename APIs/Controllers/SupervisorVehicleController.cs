using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.SupervisorVehicle;
using Constants;
using System.Security.Claims;
using Utils;

namespace APIs.Controllers
{
    /// <summary>
    /// Controller for Supervisor-specific vehicle assignment operations
    /// Note: Admin operations are handled by VehicleAssignmentController
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SupervisorVehicleController : ControllerBase
    {
        private readonly ISupervisorVehicleService _supervisorVehicleService;

        public SupervisorVehicleController(ISupervisorVehicleService supervisorVehicleService)
        {
            _supervisorVehicleService = supervisorVehicleService;
        }

        /// <summary>
        /// Get supervisor assignments with filtering and pagination
        /// Supervisor can view own assignments, Admin can view any supervisor's assignments
        /// </summary>
        [HttpGet("supervisor/{supervisorId}/assignments")]
        public async Task<ActionResult<SupervisorAssignmentListResponse>> GetSupervisorAssignments(
            Guid supervisorId,
            [FromQuery] bool? isActive = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, supervisorId))
                {
                    return Forbid();
                }

                var result = await _supervisorVehicleService.GetSupervisorAssignmentsAsync(supervisorId, isActive, startDate, endDate, page, perPage);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving supervisor assignments." });
            }
        }

        /// <summary>
        /// Get current supervisor's own vehicle from token - Supervisor only
        /// </summary>
        [Authorize(Roles = Roles.Supervisor)]
        [HttpGet("current-vehicle")]
        public async Task<ActionResult<object>> GetCurrentVehicle()
        {
            try
            {
                var supervisorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (supervisorIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        error = "SUPERVISOR_ID_NOT_FOUND",
                        message = "Supervisor ID not found in token."
                    });
            }

                Guid supervisorId = Guid.Parse(supervisorIdClaim.Value);

                var result = await _supervisorVehicleService.GetSupervisorCurrentVehicleAsync(supervisorId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "NO_VEHICLE_ASSIGNED",
                        message = "You have no vehicle assigned."
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "INTERNAL_SERVER_ERROR",
                    message = "An error occurred while retrieving your vehicle information."
                });
            }
        }
    }
}
