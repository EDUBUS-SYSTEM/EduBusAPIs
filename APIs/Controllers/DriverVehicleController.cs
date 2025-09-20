using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.DriverVehicle;
using Services.Models.Driver;
using Data.Models.Enums;
using Constants;
using System.Security.Claims;
using Utils;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DriverVehicleController : ControllerBase
    {
        private readonly IDriverVehicleService _driverVehicleService;

        public DriverVehicleController(IDriverVehicleService driverVehicleService)
        {
            _driverVehicleService = driverVehicleService;
        }

        #region Driver Assignment Management (Driver can view own, Admin can view any)

        /// <summary>
        /// Get driver assignments with filtering and pagination - Driver can view own, Admin can view any
        /// </summary>
        [HttpGet("driver/{driverId}/assignments")]
        public async Task<ActionResult<AssignmentListResponse>> GetDriverAssignments(
            Guid driverId,
            [FromQuery] bool? isActive = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverId))
                {
                    return Forbid();
                }

                var result = await _driverVehicleService.GetDriverAssignmentsAsync(driverId, isActive, startDate, endDate, page, perPage);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving driver assignments." });
            }
        }

        /// <summary>
        /// Get driver assignment summary - Driver can view own, Admin can view any
        /// </summary>
        [HttpGet("driver/{driverId}/assignments/summary")]
        public async Task<ActionResult<DriverAssignmentSummaryResponse>> GetDriverAssignmentSummary(Guid driverId)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverId))
                {
                    return Forbid();
                }

                var result = await _driverVehicleService.GetDriverAssignmentSummaryAsync(driverId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving driver assignment summary." });
            }
        }

        /// <summary>
        /// Get driver current assignments - Driver can view own, Admin can view any
        /// </summary>
        [HttpGet("driver/{driverId}/assignments/current")]
        public async Task<ActionResult<AssignmentListResponse>> GetDriverCurrentAssignments(Guid driverId)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverId))
                {
                    return Forbid();
                }

                var result = await _driverVehicleService.GetDriverAssignmentsAsync(driverId, isActive: true, page: 1, perPage: 50);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving current assignments." });
            }
        }

        /// <summary>
        /// Get driver upcoming assignments - Driver can view own, Admin can view any
        /// </summary>
        [HttpGet("driver/{driverId}/assignments/upcoming")]
        public async Task<ActionResult<AssignmentListResponse>> GetDriverUpcomingAssignments(Guid driverId)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverId))
                {
                    return Forbid();
                }

                var result = await _driverVehicleService.GetDriverAssignmentsAsync(driverId, startDate: DateTime.UtcNow, page: 1, perPage: 50);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving upcoming assignments." });
            }
        }

        #endregion

        #region Vehicle Assignment Management (Admin only)

        /// <summary>
        /// Get drivers assigned to a vehicle
        /// </summary>
        [HttpGet("vehicle/{vehicleId}/drivers")]
        public async Task<ActionResult<object>> GetDriversByVehicle(Guid vehicleId,
        [FromQuery] bool? isActive,
        [FromQuery] bool availableOnly = false,
        [FromQuery] DateTime? startTimeUtc = null,
        [FromQuery] DateTime? endTimeUtc = null)
        {
            if (availableOnly)
            {
                var start = startTimeUtc ?? DateTime.UtcNow;
                var end = endTimeUtc ?? start.AddHours(4);

                var availableDrivers = await _driverVehicleService
                    .GetDriversNotAssignedToVehicleAsync(vehicleId, start, end);
                return Ok(new { success = true, data = availableDrivers });
            }
            else
            {
                var result = await _driverVehicleService.GetDriversByVehicleAsync(vehicleId, isActive);
                if (result == null)
                    return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

                return Ok(result);
            }
        }

        /// <summary>
        /// Assign a driver to a vehicle - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("vehicle/{vehicleId}/drivers")]
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

        #endregion

        #region Enhanced Driver-Vehicle Assignment (Admin only)

        /// <summary>
        /// Enhanced driver assignment with validation - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("vehicle/{vehicleId}/drivers/assign-enhanced")]
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
        [HttpGet("vehicle/{vehicleId}/conflicts")]
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

        #region Advanced Assignment Management (Admin only)

        /// <summary>
        /// Get all assignments with advanced filtering, sorting, and pagination - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("assignments")]
        public async Task<ActionResult<AssignmentListResponse>> GetAllAssignments(
            [FromQuery] Guid? driverId = null,
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] DriverVehicleStatus? status = null,
            [FromQuery] bool? isPrimaryDriver = null,
            [FromQuery] DateTime? startDateFrom = null,
            [FromQuery] DateTime? startDateTo = null,
            [FromQuery] DateTime? endDateFrom = null,
            [FromQuery] DateTime? endDateTo = null,
            [FromQuery] Guid? assignedByAdminId = null,
            [FromQuery] Guid? approvedByAdminId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isUpcoming = null,
            [FromQuery] bool? isCompleted = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                var request = new AssignmentListRequest
                {
                    DriverId = driverId,
                    VehicleId = vehicleId,
                    Status = status,
                    IsPrimaryDriver = isPrimaryDriver,
                    StartDateFrom = startDateFrom,
                    StartDateTo = startDateTo,
                    EndDateFrom = endDateFrom,
                    EndDateTo = endDateTo,
                    AssignedByAdminId = assignedByAdminId,
                    ApprovedByAdminId = approvedByAdminId,
                    IsActive = isActive,
                    IsUpcoming = isUpcoming,
                    IsCompleted = isCompleted,
                    SearchTerm = searchTerm,
                    Page = page,
                    PerPage = perPage,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var result = await _driverVehicleService.GetAssignmentsWithFiltersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving assignments." });
            }
        }

        /// <summary>
        /// Get assignments by status - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("assignments/status/{status}")]
        public async Task<ActionResult<AssignmentListResponse>> GetAssignmentsByStatus(
            DriverVehicleStatus status,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                var request = new AssignmentListRequest
                {
                    Status = status,
                    Page = page,
                    PerPage = perPage,
                    SortBy = "createdAt",
                    SortOrder = "desc"
                };

                var result = await _driverVehicleService.GetAssignmentsWithFiltersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving assignments by status." });
            }
        }

        /// <summary>
        /// Get assignments in date range - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("assignments/date-range")]
        public async Task<ActionResult<AssignmentListResponse>> GetAssignmentsInDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                var request = new AssignmentListRequest
                {
                    StartDateFrom = startDate,
                    StartDateTo = endDate,
                    Page = page,
                    PerPage = perPage,
                    SortBy = "startTime",
                    SortOrder = "asc"
                };

                var result = await _driverVehicleService.GetAssignmentsWithFiltersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving assignments in date range." });
            }
        }

        /// <summary>
        /// Update assignment status - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("assignments/{assignmentId}/status")]
        public async Task<ActionResult<DriverAssignmentResponse>> UpdateAssignmentStatus(
            Guid assignmentId, 
            [FromBody] UpdateAssignmentStatusRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.UpdateAssignmentStatusAsync(assignmentId, request.Status, adminId, request.Note);
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
        /// Get all assignment conflicts - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("assignments/conflicts")]
        public async Task<ActionResult<IEnumerable<AssignmentConflictDto>>> GetAllAssignmentConflicts()
        {
            try
            {
                // This would need to be implemented in the service to get all conflicts
                // For now, return empty list as placeholder
                return Ok(new List<AssignmentConflictDto>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving assignment conflicts." });
            }
        }

        #endregion
    }
}
