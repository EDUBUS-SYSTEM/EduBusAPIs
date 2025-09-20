using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Vehicle;
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

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        /// <summary>
        /// Get list of vehicles with pagination, filtering, and sorting
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<ActionResult<VehicleListResponse>> GetVehicles(
        [FromQuery(Name = "status")] string? status,
        [FromQuery(Name = "capacity")] int? capacity,
        [FromQuery(Name = "adminId")] Guid? adminId,
        [FromQuery(Name = "search")] string? search, 
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "perPage")] int perPage = 20,
        [FromQuery(Name = "sortBy")] string? sortBy = "createdAt",
        [FromQuery(Name = "sortOrder")] string sortOrder = "desc")
        {
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 1, 100); 

            var result = await _vehicleService.GetVehiclesAsync(
                status, capacity, adminId, search, page, perPage, sortBy, sortOrder); 

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
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, error = result.Error, message = result.Message });
            }
            
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

            if (!updated.Success)
            {
                return BadRequest(new { success = false, error = updated.Error, message = updated.Message });
            }

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

            if (!updated.Success)
            {
                return BadRequest(new { success = false, error = updated.Error, message = updated.Message });
            }

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

    }
}
