using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.DriverVehicle;
using Services.Models.SupervisorVehicle;
using Data.Models.Enums;
using Constants;
using System.Security.Claims;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class VehicleAssignmentController : ControllerBase
    {
        private readonly IDriverVehicleService _driverVehicleService;
        private readonly ISupervisorVehicleService _supervisorVehicleService;

        public VehicleAssignmentController(
            IDriverVehicleService driverVehicleService,
            ISupervisorVehicleService supervisorVehicleService)
        {
            _driverVehicleService = driverVehicleService;
            _supervisorVehicleService = supervisorVehicleService;
        }

        #region Get Assignments

        /// <summary>
        /// Get all assignments with filtering, sorting, and pagination
        /// Supports both Driver and Supervisor assignments
        /// </summary>
        [HttpGet("assignments")]
        public async Task<ActionResult<object>> GetAssignments(
            [FromQuery] string? assignmentType = null, // "Driver" or "Supervisor" or null for all
            [FromQuery] Guid? driverId = null,
            [FromQuery] Guid? supervisorId = null,
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] DriverVehicleStatus? status = null,
            [FromQuery] bool? isPrimaryDriver = null,
            [FromQuery] DateTime? startDateFrom = null,
            [FromQuery] DateTime? startDateTo = null,
            [FromQuery] DateTime? endDateFrom = null,
            [FromQuery] DateTime? endDateTo = null,
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
                // If assignmentType is specified, route to appropriate service
                if (!string.IsNullOrEmpty(assignmentType))
                {
                    if (assignmentType.Equals("Driver", StringComparison.OrdinalIgnoreCase))
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
                        return Ok(new { success = true, data = result, assignmentType = "Driver" });
                    }
                    else if (assignmentType.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
                    {
                        // For Supervisor, we need to get assignments by supervisor or vehicle
                        if (supervisorId.HasValue)
                        {
                            var result = await _supervisorVehicleService.GetSupervisorAssignmentsAsync(
                                supervisorId.Value, 
                                isActive, 
                                startDateFrom, 
                                endDateTo, 
                                page, 
                                perPage);
                            return Ok(new { success = true, data = result, assignmentType = "Supervisor" });
                        }
                        else if (vehicleId.HasValue)
                        {
                            var result = await _supervisorVehicleService.GetSupervisorsByVehicleAsync(vehicleId.Value, isActive);
                            return Ok(new { success = true, data = result, assignmentType = "Supervisor" });
                        }
                        else
                        {
                            return BadRequest(new { success = false, error = "supervisorId or vehicleId is required for Supervisor assignments" });
                        }
                    }
                    else
                    {
                        return BadRequest(new { success = false, error = "assignmentType must be 'Driver' or 'Supervisor'" });
                    }
                }
                else
                {
                    // Return both Driver and Supervisor assignments
                    var driverRequest = new AssignmentListRequest
                    {
                        DriverId = driverId,
                        VehicleId = vehicleId,
                        Status = status,
                        IsPrimaryDriver = isPrimaryDriver,
                        StartDateFrom = startDateFrom,
                        StartDateTo = startDateTo,
                        EndDateFrom = endDateFrom,
                        EndDateTo = endDateTo,
                        IsActive = isActive,
                        IsUpcoming = isUpcoming,
                        IsCompleted = isCompleted,
                        SearchTerm = searchTerm,
                        Page = page,
                        PerPage = perPage,
                        SortBy = sortBy,
                        SortOrder = sortOrder
                    };

                    var driverResult = await _driverVehicleService.GetAssignmentsWithFiltersAsync(driverRequest);
                    
                    // For Supervisor, we can only get by vehicle or supervisor
                    object? supervisorResult = null;
                    if (vehicleId.HasValue)
                    {
                        supervisorResult = await _supervisorVehicleService.GetSupervisorsByVehicleAsync(vehicleId.Value, isActive);
                    }

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            drivers = driverResult,
                            supervisors = supervisorResult
                        },
                        assignmentType = "All"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving assignments." });
            }
        }

        /// <summary>
        /// Get assignments for a specific vehicle
        /// </summary>
        [HttpGet("vehicle/{vehicleId}/assignments")]
        public async Task<ActionResult<object>> GetVehicleAssignments(
            Guid vehicleId,
            [FromQuery] string? assignmentType = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                object? drivers = null;
                object? supervisors = null;

                if (string.IsNullOrEmpty(assignmentType) || assignmentType.Equals("Driver", StringComparison.OrdinalIgnoreCase))
                {
                    var driverResult = await _driverVehicleService.GetDriversByVehicleAsync(vehicleId, isActive);
                    drivers = driverResult;
                }

                if (string.IsNullOrEmpty(assignmentType) || assignmentType.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
                {
                    var supervisorResult = await _supervisorVehicleService.GetSupervisorsByVehicleAsync(vehicleId, isActive);
                    supervisors = supervisorResult;
                }

                var result = new
                {
                    success = true,
                    vehicleId = vehicleId,
                    drivers = drivers,
                    supervisors = supervisors
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving vehicle assignments." });
            }
        }

        /// <summary>
        /// Get available supervisors for a vehicle during a specific time period
        /// Returns supervisors who are NOT assigned to ANY vehicle during the specified time
        /// </summary>
        [HttpGet("vehicle/{vehicleId}/supervisors")]
        public async Task<ActionResult<object>> GetAvailableSupervisorsForVehicle(
            Guid vehicleId,
            [FromQuery] bool availableOnly = false,
            [FromQuery] DateTime? startTimeUtc = null,
            [FromQuery] DateTime? endTimeUtc = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                // If availableOnly is true and time range is provided, get available supervisors
                if (availableOnly && startTimeUtc.HasValue)
                {
                    var availableSupervisors = await _supervisorVehicleService.GetAvailableSupervisorsForVehicleAsync(
                        vehicleId,
                        startTimeUtc.Value,
                        endTimeUtc);

                    return Ok(new { success = true, data = availableSupervisors });
                }
                
                // Otherwise, get assigned supervisors (existing behavior)
                var result = await _supervisorVehicleService.GetSupervisorsByVehicleAsync(vehicleId, isActive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while retrieving supervisors." });
            }
        }

        #endregion

        #region Create Assignment

        /// <summary>
        /// Create Driver assignment (convenience endpoint)
        /// </summary>
        [HttpPost("vehicle/{vehicleId}/drivers")]
        public async Task<ActionResult<object>> CreateDriverAssignment(
            Guid vehicleId,
            [FromBody] DriverAssignmentRequest request)
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

                return CreatedAtAction(nameof(GetVehicleAssignments), new { vehicleId = vehicleId }, new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Create Supervisor assignment (convenience endpoint)
        /// </summary>
        [HttpPost("vehicle/{vehicleId}/supervisors")]
        public async Task<ActionResult<object>> CreateSupervisorAssignment(
            Guid vehicleId,
            [FromBody] SupervisorAssignmentRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _supervisorVehicleService.AssignSupervisorWithValidationAsync(vehicleId, request, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "VEHICLE_NOT_FOUND" });

                return CreatedAtAction(nameof(GetVehicleAssignments), new { vehicleId = vehicleId }, new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, error = ex.Message });
            }
        }

        #endregion

        #region Update Assignment

        /// <summary>
        /// Update a Driver assignment
        /// </summary>
        [HttpPut("assignments/{assignmentId}/driver")]
        public async Task<ActionResult<object>> UpdateDriverAssignment(
            Guid assignmentId,
            [FromBody] UpdateAssignmentRequest request)
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

                return Ok(new { success = true, data = result, assignmentType = "Driver" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Update a Supervisor assignment
        /// </summary>
        [HttpPut("assignments/{assignmentId}/supervisor")]
        public async Task<ActionResult<object>> UpdateSupervisorAssignment(
            Guid assignmentId,
            [FromBody] UpdateSupervisorAssignmentRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _supervisorVehicleService.UpdateAssignmentAsync(assignmentId, request, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "ASSIGNMENT_NOT_FOUND" });

                return Ok(new { success = true, data = result, assignmentType = "Supervisor" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        #endregion

        #region Delete Assignment

        /// <summary>
        /// Delete a Driver assignment
        /// </summary>
        [HttpDelete("assignments/{assignmentId}/driver")]
        public async Task<ActionResult<object>> DeleteDriverAssignment(Guid assignmentId)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _driverVehicleService.DeleteAssignmentAsync(assignmentId, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "ASSIGNMENT_NOT_FOUND" });

                return Ok(new { success = true, data = result, assignmentType = "Driver" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while deleting assignment." });
            }
        }

        /// <summary>
        /// Delete a Supervisor assignment (soft delete)
        /// </summary>
        [HttpDelete("assignments/{assignmentId}/supervisor")]
        public async Task<ActionResult<object>> DeleteSupervisorAssignment(Guid assignmentId)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null)
                return Unauthorized(new { success = false, error = "ADMIN_ID_NOT_FOUND" });

            Guid adminId = Guid.Parse(adminIdClaim.Value);

            try
            {
                var result = await _supervisorVehicleService.DeleteAssignmentAsync(assignmentId, adminId);
                if (result == null)
                    return NotFound(new { success = false, error = "ASSIGNMENT_NOT_FOUND" });

                return Ok(new { success = true, data = result, assignmentType = "Supervisor" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while deleting assignment." });
            }
        }

        #endregion

    }
}

