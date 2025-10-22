using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Driver;
using Services.Models.DriverVehicle;
using Services.Models.UserAccount;
using Data.Models;
using Data.Models.Enums;
using Constants;
using Microsoft.AspNetCore.Authorization;
using Utils;
using System.Security.Claims;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly IFileService _fileService;
        private readonly IDriverLeaveService _driverLeaveService;
        private readonly IDriverWorkingHoursService _driverWorkingHoursService;
        private readonly IDriverVehicleService _driverVehicleService;
        
        public DriverController(
            IDriverService driverService, 
            IFileService fileService,
            IDriverLeaveService driverLeaveService,
            IDriverWorkingHoursService driverWorkingHoursService,
            IDriverVehicleService driverVehicleService)
        {
            _driverService = driverService;
            _fileService = fileService;
            _driverLeaveService = driverLeaveService;
            _driverWorkingHoursService = driverWorkingHoursService;
            _driverVehicleService = driverVehicleService;
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<CreateUserResponse>> CreateDriver([FromBody] CreateDriverRequest dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var response = await _driverService.CreateDriverAsync(dto);
                return Ok(response);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An internal error occurred.");
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportDrivers(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file format. Only .xlsx files are supported.");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _driverService.ImportDriversFromExcelAsync(stream);

                return Ok(new
                {
                    TotalProcessed = result.TotalProcessed,
                    SuccessUsers = result.SuccessfulUsers,
                    FailedUsers = result.FailedUsers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while importing drivers.",
                    Details = ex.Message
                });
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("export")]
        public async Task<IActionResult> ExportDrivers()
        {
            var fileContent = await _driverService.ExportDriversToExcelAsync();
            if (fileContent == null || fileContent.Length == 0)
            {
                return NotFound(new { message = "No data to export to Excel." });
            }

            return File(fileContent,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Drivers.xlsx");
        }

        /// <summary>
        /// Upload health certificate - Drivers can upload their own certificate, Admin can upload for any driver
        /// </summary>
        [HttpPost("{driverId}/upload-health-certificate")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> UploadHealthCertificate(Guid driverId, IFormFile file)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverId))
                {
                    return Forbid();
                }

                if (file == null)
                    return BadRequest("No file provided.");

                var fileId = await _fileService.UploadHealthCertificateAsync(driverId, file);
                return Ok(new { FileId = fileId, Message = "Health certificate uploaded successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while uploading the health certificate.");
            }
        }

        /// <summary>
        /// Get health certificate - Drivers can view their own certificate, Admin can view any driver's certificate
        /// </summary>
        [HttpGet("{driverId}/health-certificate")]
        public async Task<IActionResult> GetHealthCertificate(Guid driverId)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverId))
                {
                    return Forbid();
                }

                var fileId = await _driverService.GetHealthCertificateFileIdAsync(driverId);
                if (!fileId.HasValue)
                    return NotFound("Health certificate not found.");

                var fileContent = await _fileService.GetFileAsync(fileId.Value);
                var contentType = await _fileService.GetFileContentTypeAsync(fileId.Value);

                return File(fileContent, contentType);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the health certificate.");
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DriverResponse>>> GetAllDrivers()
        {
            try
            {
                var drivers = await _driverService.GetAllDriversAsync();
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving drivers.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DriverResponse>> GetDriverById(Guid id)
        {
            try
            {
                var driver = await _driverService.GetDriverResponseByIdAsync(id);
                if (driver == null)
                {
                    return NotFound("Driver not found.");
                }

                return Ok(driver);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the driver.");
            }
        }

        #region Driver Status Management

        /// <summary>
        /// Get drivers by status - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<DriverResponse>>> GetDriversByStatus(DriverStatus status)
        {
            try
            {
                var drivers = await _driverService.GetDriversByStatusAsync(status);
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving drivers by status.");
            }
        }

        /// <summary>
        /// Update driver status - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}/status")]
        public async Task<ActionResult<DriverResponse>> UpdateDriverStatus(Guid id, [FromBody] UpdateDriverStatusRequest request)
        {
            try
            {
                var driver = await _driverService.UpdateDriverStatusAsync(id, request.Status, request.Note);
                return Ok(driver);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating driver status.");
            }
        }

        /// <summary>
        /// Suspend driver - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}/suspend")]
        public async Task<ActionResult<DriverResponse>> SuspendDriver(Guid id, [FromBody] SuspendDriverRequest request)
        {
            try
            {
                var driver = await _driverService.SuspendDriverAsync(id, request.Reason, request.UntilDate);
                return Ok(driver);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while suspending driver.");
            }
        }

        /// <summary>
        /// Reactivate driver - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}/reactivate")]
        public async Task<ActionResult<DriverResponse>> ReactivateDriver(Guid id)
        {
            try
            {
                var driver = await _driverService.ReactivateDriverAsync(id);
                return Ok(driver);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while reactivating driver.");
            }
        }


        #endregion

        #region Driver Leave Management

        /// <summary>
        /// Get driver leave requests - Driver can view own, Admin can view any
        /// </summary>
        [HttpGet("{id}/leaves")]
        public async Task<ActionResult<IEnumerable<DriverLeaveResponse>>> GetDriverLeaves(Guid id)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, id))
                {
                    return Forbid();
                }

                var leaves = await _driverLeaveService.GetDriverLeavesAsync(id, null, null);
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving driver leaves.");
            }
        }

        /// <summary>
        /// Get all driver leave requests with pagination, search and filters - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("leaves")]
        public async Task<ActionResult<DriverLeaveListResponse>> GetAllLeaveRequests(
            [FromQuery] DriverLeaveListRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(new DriverLeaveListResponse
                    {
                        Success = false,
                        Error = ModelState
                    });
                }

                var result = await _driverLeaveService.GetLeaveRequestsAsync(request);
                
                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DriverLeaveListResponse
                {
                    Success = false,
                    Error = new { message = "An error occurred while retrieving leave requests." }
                });
            }
        }
        /// <summary>
        /// Get leave requests for the currently logged-in driver with pagination
        /// </summary>
        [Authorize(Roles = Roles.Driver)]
        [HttpGet("my-leaves")]
        public async Task<ActionResult<DriverLeaveListResponse>> GetMyLeaveRequests(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] LeaveStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                var driverIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(driverIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User identification not found in token"
                    });
                }

                var driverId = Guid.Parse(driverIdClaim);
                
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (perPage < 1 || perPage > 100) perPage = 20;
                
                var result = await _driverLeaveService.GetDriverLeavesPaginatedAsync(
                    driverId, fromDate, toDate, status, page, perPage);
                
                if (!result.Success)
                {
                    return StatusCode(500, result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DriverLeaveListResponse
                {
                    Success = false,
                    Error = new { message = "An error occurred while retrieving leave requests.", details = ex.Message }
                });
            }
        }

        /// <summary>
        /// Get a specific leave request by ID - Driver can view own, Admin can view any
        /// </summary>
        [HttpGet("leaves/{leaveId}")]
        public async Task<ActionResult<DriverLeaveResponse>> GetLeaveById(Guid leaveId)
        {
            try
            {
                var leave = await _driverLeaveService.GetLeaveByIdAsync(leaveId);
                if (leave == null)
                    return NotFound(new { message = "Leave request not found." });

                // Check authorization: Driver can only see their own leaves
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole(Roles.Admin);
                
                if (!isAdmin && leave.DriverId.ToString() != userIdClaim)
                    return Forbid();

                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving leave request.", details = ex.Message });
            }
        }

       

        /// <summary>
        /// Create leave request - Driver can create own, Admin can create for any
        /// </summary>
        [Authorize(Roles = Roles.Driver)]
        [HttpPost("send-leave-request")]
        public async Task<ActionResult<DriverLeaveResponse>> SendLeaveRequest([FromBody] CreateLeaveRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList(),
                        requestId = HttpContext.TraceIdentifier
                    });
                }

                var driverIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(driverIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User identification not found in token",
                    });
                }
                var driverId = Guid.Parse(driverIdClaim);
                request.DriverId = driverId;
                
                var leave = await _driverLeaveService.CreateLeaveRequestAsync(request);
                return CreatedAtAction(nameof(GetDriverLeaves), new { id = driverIdClaim }, leave);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message,  
                });
            }
            catch (InvalidOperationException ex)
            {
                // Check if it's an overlapping leave request error
                if (ex.Message.Contains("already have") && 
                    (ex.Message.Contains("pending") || ex.Message.Contains("approved")))
                {
                    return Conflict(new
                    {
                        message = ex.Message,
                        errorType = "OverlappingLeaveRequest",
                        statusCode = 409
                    });
                }
                
                // For other InvalidOperationException (validation errors, etc.)
                return BadRequest(new
                {
                    message = ex.Message,
                    errorType = "ValidationError",
                    statusCode = 400
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating leave request.");
            }
        }

        /// <summary>
        /// Approve leave request - Admin only
        /// Optional: Assign replacement driver during approval
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("leaves/{leaveId}/approve")]
        public async Task<ActionResult<DriverLeaveResponse>> ApproveLeaveRequest(Guid leaveId, [FromBody] ApproveLeaveRequestDto request)
        {
            try
            {
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null)
                    return Unauthorized("Admin ID not found.");

                Guid adminId = Guid.Parse(adminIdClaim.Value);
                
                // Approve leave request
                var leave = await _driverLeaveService.ApproveLeaveRequestAsync(leaveId, request, adminId);
                
                // Tạo assignment cho replacement driver (nếu có)
                if (request.ReplacementDriverId.HasValue)
                {
                    try
                    {
                        // Validate replacement driver exists
                        var replacementDriver = await _driverService.GetDriverByIdAsync(request.ReplacementDriverId.Value);
                        if (replacementDriver == null)
                            return BadRequest("Replacement driver not found.");
                        
                        // Check if replacement driver is available during leave period
                        var isAvailable = await _driverService.IsDriverAvailableAsync(
                            request.ReplacementDriverId.Value, 
                            leave.StartDate, 
                            leave.EndDate);
                            
                        if (!isAvailable)
                            return BadRequest("Replacement driver is not available during the leave period.");
                        
                        // Determine vehicle ID - always use current driver's vehicle
                        var vehicleId = await _driverVehicleService.GetVehicleForDriverReplacementAsync(leave.DriverId);
                        if (!vehicleId.HasValue)
                            return BadRequest("Current driver has no active vehicle assignment.");
                        
                        // Create assignment for replacement driver
                        var assignmentRequest = new DriverAssignmentRequest
                        {
                            DriverId = request.ReplacementDriverId.Value,
                            StartTimeUtc = leave.StartDate,
                            EndTimeUtc = leave.EndDate,
                            IsPrimaryDriver = false
                        };
                        
                        var assignment = await _driverVehicleService.AssignDriverAsync(
                            vehicleId.Value, 
                            assignmentRequest, 
                            adminId);
                        
                        if (assignment?.Success != true)
                            return BadRequest("Failed to create replacement assignment.");
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"Leave approved but failed to create replacement assignment: {ex.Message}");
                    }
                }
                
                return Ok(leave);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while approving leave request.");
            }
        }

        /// <summary>
        /// Reject leave request - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("leaves/{leaveId}/reject")]
        public async Task<ActionResult<DriverLeaveResponse>> RejectLeaveRequest(Guid leaveId, [FromBody] RejectLeaveRequestDto request)
        {
            try
            {
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null)
                    return Unauthorized("Admin ID not found.");

                Guid adminId = Guid.Parse(adminIdClaim.Value);
                var leave = await _driverLeaveService.RejectLeaveRequestAsync(leaveId, request, adminId);
                return Ok(leave);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while rejecting leave request.");
            }
        }

        #endregion

        #region Driver Working Hours Management

        /// <summary>
        /// Get driver working hours - Driver can view own, Admin can view any
        /// </summary>
        [HttpGet("{id}/working-hours")]
        public async Task<ActionResult<IEnumerable<DriverWorkingHoursResponse>>> GetDriverWorkingHours(Guid id)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, id))
                {
                    return Forbid();
                }

                var workingHours = await _driverWorkingHoursService.GetWorkingHoursByDriverAsync(id);
                return Ok(workingHours);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving driver working hours.");
            }
        }

        /// <summary>
        /// Set driver working hours - Driver can set own, Admin can set for any
        /// </summary>
        [HttpPost("{id}/working-hours")]
        public async Task<ActionResult<DriverWorkingHoursResponse>> SetDriverWorkingHours(Guid id, [FromBody] CreateWorkingHoursDto request)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, id))
                {
                    return Forbid();
                }

                request.DriverId = id;
                var workingHours = await _driverWorkingHoursService.CreateWorkingHoursAsync(request);
                return CreatedAtAction(nameof(GetDriverWorkingHours), new { id = id }, workingHours);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while setting driver working hours.");
            }
        }

        /// <summary>
        /// Update driver working hours - Driver can update own, Admin can update any
        /// </summary>
        [HttpPut("working-hours/{workingHoursId}")]
        public async Task<ActionResult<DriverWorkingHoursResponse>> UpdateDriverWorkingHours(Guid workingHoursId, [FromBody] UpdateWorkingHoursDto request)
        {
            try
            {
                var workingHours = await _driverWorkingHoursService.UpdateWorkingHoursAsync(workingHoursId, request);
                if (workingHours == null)
                    return NotFound("Working hours not found.");

                return Ok(workingHours);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating driver working hours.");
            }
        }

        #endregion

    }
}
