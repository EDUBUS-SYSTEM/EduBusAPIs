using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Vehicle;
using Services.Models.DriverVehicle;
using Services.Models.Driver;
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
        private readonly IDriverVehicleService _driverVehicleService;

        public VehicleController(IVehicleService vehicleService, IDriverVehicleService driverVehicleService)
        {
            _vehicleService = vehicleService;
            _driverVehicleService = driverVehicleService;
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

        /// <summary>
        /// Get drivers assigned to a vehicle
        /// </summary>
        [HttpGet("{vehicleId}/drivers")]
        public async Task<ActionResult<VehicleDriversResponse>> GetDriversByVehicle(Guid vehicleId, [FromQuery] bool? isActive)
        {
            var result = await _driverVehicleService.GetDriversByVehicleAsync(vehicleId, isActive);
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
                var result = await _driverVehicleService.AssignDriverAsync(vehicleId, request, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

                return CreatedAtAction(nameof(GetDriversByVehicle), new { vehicleId = vehicleId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, error = ex.Message });
            }
        }

		/// <summary>
		/// Get unassigned vehicles
		/// </summary>
		[Authorize(Roles = Roles.Admin)]
		[HttpGet("unassigned")]
		public async Task<ActionResult<VehicleListResponse>> GetUnassignedVehicles([FromQuery] Guid? excludeRouteId = null)
		{
			var result = await _vehicleService.GetUnassignedVehiclesAsync(excludeRouteId);
			return Ok(result);
		}

		#region Enhanced Driver-Vehicle Assignment

		/// <summary>
		/// Enhanced driver assignment with validation - Admin only
		/// </summary>
		[Authorize(Roles = Roles.Admin)]
        [HttpPost("{vehicleId}/drivers/assign-enhanced")]
        public async Task<ActionResult<DriverAssignmentResponse>> AssignDriverWithValidation(Guid vehicleId, [FromBody] DriverAssignmentRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.AssignDriverWithValidationAsync(vehicleId, request, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

                return CreatedAtAction(nameof(GetDriversByVehicle), new { vehicleId = vehicleId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Update driver assignment - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("assignments/{assignmentId}")]
        public async Task<ActionResult<DriverAssignmentResponse>> UpdateAssignment(Guid assignmentId, [FromBody] UpdateAssignmentRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.UpdateAssignmentAsync(assignmentId, request, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "ASSIGNMENT_NOT_FOUND" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Cancel driver assignment - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("assignments/{assignmentId}")]
        public async Task<ActionResult<DriverAssignmentResponse>> CancelAssignment(Guid assignmentId, [FromQuery] string reason)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.CancelAssignmentAsync(assignmentId, reason, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "ASSIGNMENT_NOT_FOUND" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Detect assignment conflicts for a vehicle - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("{vehicleId}/conflicts")]
        public async Task<ActionResult<IEnumerable<AssignmentConflictDto>>> DetectConflicts(
            Guid vehicleId, 
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime)
        {
            try
            {
                var conflicts = await _driverVehicleService.DetectAssignmentConflictsAsync(vehicleId, startTime, endTime);
                return Ok(conflicts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while detecting conflicts." });
            }
        }

        /// <summary>
        /// Suggest replacement for assignment - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("assignments/{assignmentId}/suggest")]
        public async Task<ActionResult<ReplacementSuggestionResponse>> SuggestReplacement(Guid assignmentId)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.SuggestReplacementAsync(assignmentId, adminId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while generating suggestions." });
            }
        }

        /// <summary>
        /// Accept replacement suggestion - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("assignments/{assignmentId}/accept-suggestion/{suggestionId}")]
        public async Task<ActionResult<object>> AcceptReplacementSuggestion(Guid assignmentId, Guid suggestionId)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.AcceptReplacementSuggestionAsync(assignmentId, suggestionId, adminId);
                return Ok(new { success = result, message = "Replacement suggestion accepted." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while accepting suggestion." });
            }
        }

        /// <summary>
        /// Approve assignment - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("assignments/{assignmentId}/approve")]
        public async Task<ActionResult<DriverAssignmentResponse>> ApproveAssignment(Guid assignmentId, [FromQuery] string? note)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.ApproveAssignmentAsync(assignmentId, adminId, note);
                if (result == null)
                    return NotFound(new { success = false, error = "ASSIGNMENT_NOT_FOUND" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Reject assignment - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("assignments/{assignmentId}/reject")]
        public async Task<ActionResult<DriverAssignmentResponse>> RejectAssignment(Guid assignmentId, [FromQuery] string reason)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.RejectAssignmentAsync(assignmentId, adminId, reason);
                if (result == null)
                    return NotFound(new { success = false, error = "ASSIGNMENT_NOT_FOUND" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        #endregion
    }
}
