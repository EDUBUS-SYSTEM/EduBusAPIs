using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Driver;
using Services.Models.UserAccount;
using Data.Models;
using Constants;
using Microsoft.AspNetCore.Authorization;
using Utils;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly IFileService _fileService;
        
        public DriverController(IDriverService driverService, IFileService fileService)
        {
            _driverService = driverService;
            _fileService = fileService;
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
        public async Task<ActionResult<IEnumerable<Services.Models.Driver.DriverResponse>>> GetAllDrivers()
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
        public async Task<ActionResult<Services.Models.Driver.DriverResponse>> GetDriverById(Guid id)
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
    }
}
