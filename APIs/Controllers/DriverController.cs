using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Driver;
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
        
        public DriverController(
            IDriverService driverService, 
            IFileService fileService,
            IDriverLeaveService driverLeaveService,
            IDriverWorkingHoursService driverWorkingHoursService)
        {
            _driverService = driverService;
            _fileService = fileService;
            _driverLeaveService = driverLeaveService;
            _driverWorkingHoursService = driverWorkingHoursService;
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

        /// <summary>
        /// Get available drivers in time range - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<DriverResponse>>> GetAvailableDrivers(
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime)
        {
            try
            {
                var drivers = await _driverService.GetAvailableDriversAsync(startTime, endTime);
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving available drivers.");
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
        /// Create leave request - Driver can create own, Admin can create for any
        /// </summary>
        [HttpPost("{id}/leaves")]
        public async Task<ActionResult<DriverLeaveResponse>> CreateLeaveRequest(Guid id, [FromBody] CreateLeaveRequestDto request)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, id))
                {
                    return Forbid();
                }

                var leave = await _driverLeaveService.CreateLeaveRequestAsync(request);
                return CreatedAtAction(nameof(GetDriverLeaves), new { id = id }, leave);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating leave request.");
            }
        }

        /// <summary>
        /// Approve leave request - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("leaves/{leaveId}/approve")]
        public async Task<ActionResult<DriverLeaveResponse>> ApproveLeaveRequest(Guid leaveId, [FromBody] ApproveLeaveRequestDto request)
        {
            try
            {
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null)
                    return Unauthorized("Admin ID not found.");

                Guid adminId = Guid.Parse(adminIdClaim.Value);
                var leave = await _driverLeaveService.ApproveLeaveRequestAsync(leaveId, request, adminId);
                return Ok(leave);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
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
        [HttpPut("leaves/{leaveId}/reject")]
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
