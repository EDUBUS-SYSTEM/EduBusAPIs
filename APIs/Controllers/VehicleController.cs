using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Vehicle;
using Services.Models.DriverVehicle;
using Constants;
using System.Security.Claims;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly IDriverVehicleService _driveVveicleService;

        public VehicleController(
            IVehicleService vehicleService,
            IDriverVehicleService driverVehicleService)
        {
            _vehicleService = vehicleService;
            _driveVveicleService = driverVehicleService;
        }

        /// <summary>
        /// Get list of vehicles with pagination, filtering, and sorting
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<ActionResult<VehicleListResponse>> GetVehicles(
            [FromQuery] string? status,
            [FromQuery] int? capacity,
            [FromQuery] Guid? adminId,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string sortOrder = "desc")
        {
            var result = await _vehicleService.GetVehiclesAsync(status, capacity, adminId, page, perPage, sortBy, sortOrder);
            return Ok(result);
        }

        /// <summary>
        /// Get vehicle by ID
        /// </summary>
        [HttpGet("{vehicleId}")]
        public async Task<ActionResult<VehicleResponse>> GetVehicleById(Guid vehicleId)
        {
            var vehicle = await _vehicleService.GetByIdAsync(vehicleId);
            if (vehicle == null)
                return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

            return Ok(vehicle);
        }

        /// <summary>
        /// Create a new vehicle
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<VehicleResponse>> CreateVehicle([FromBody] VehicleCreateRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            var result = await _vehicleService.CreateAsync(request, adminId);
            return CreatedAtAction(nameof(GetVehicleById), new { vehicleId = result.Data!.Id }, result);
        }

        /// <summary>
        /// Update a vehicle (full update)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{vehicleId}")]
        public async Task<ActionResult<VehicleResponse>> UpdateVehicle(Guid vehicleId, [FromBody] VehicleUpdateRequest request)
        {
            var updated = await _vehicleService.UpdateAsync(vehicleId, request);
            if (updated == null)
                return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

            return Ok(updated);
        }

        /// <summary>
        /// Partial update a vehicle
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{vehicleId}")]
        public async Task<ActionResult<VehicleResponse>> PartialUpdateVehicle(Guid vehicleId, [FromBody] VehiclePartialUpdateRequest request)
        {
            var updated = await _vehicleService.PartialUpdateAsync(vehicleId, request);
            if (updated == null)
                return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

            return Ok(updated);
        }

        /// <summary>
        /// Delete a vehicle (soft delete)
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{vehicleId}")]
        public async Task<ActionResult<object>> DeleteVehicle(Guid vehicleId)
        {
            var deleted = await _vehicleService.DeleteAsync(vehicleId);
            if (!deleted)
                return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

            return Ok(new { success = true });
        }

        /// <summary>
        /// Get drivers assigned to a vehicle
        /// </summary>
        [HttpGet("{vehicleId}/drivers")]
        public async Task<ActionResult<VehicleDriversResponse>> GetDriversByVehicle(Guid vehicleId, [FromQuery] bool? isActive)
        {
            var result = await _driveVveicleService.GetDriversByVehicleAsync(vehicleId, isActive);
            if (result == null)
                return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

            return Ok(result);
        }

        /// <summary>
        /// Assign a driver to a vehicle
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{vehicleId}/drivers")]
        public async Task<ActionResult<DriverAssignmentResponse>> AssignDriver(Guid vehicleId, [FromBody] DriverAssignmentRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driveVveicleService.AssignDriverAsync(vehicleId, request, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

                return CreatedAtAction(nameof(GetDriversByVehicle), new { vehicleId = vehicleId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, error = ex.Message });
            }
        }
    }
}
