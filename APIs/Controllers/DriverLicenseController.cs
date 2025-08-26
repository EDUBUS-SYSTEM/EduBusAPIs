using Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Driver;
using Utils;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DriverLicenseController : ControllerBase
    {
        private readonly IDriverLicenseService _driverLicenseService;
        private readonly IFileService _fileService;

        public DriverLicenseController(IDriverLicenseService driverLicenseService, IFileService fileService)
        {
            _driverLicenseService = driverLicenseService;
            _fileService = fileService;
        }

        /// <summary>
        /// Create driver license - Admin can create for any driver; Driver can create their own (only once)
        /// </summary>
        [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
        [HttpPost]
        public async Task<ActionResult<DriverLicenseResponse>> CreateDriverLicense(CreateDriverLicenseRequest request)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, request.DriverId))
                {
                    return Forbid();
                }

                var result = await _driverLicenseService.CreateDriverLicenseAsync(request);
                return CreatedAtAction(nameof(GetDriverLicenseByDriverId), new { driverId = request.DriverId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the driver license.");
            }
        }

        /// <summary>
        /// Get driver license by driver ID - Drivers can view their own license, Admin can view any license
        /// </summary>
        [HttpGet("driver/{driverId}")]
        public async Task<ActionResult<DriverLicenseResponse>> GetDriverLicenseByDriverId(Guid driverId)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverId))
                {
                    return Forbid();
                }

                var result = await _driverLicenseService.GetDriverLicenseByDriverIdAsync(driverId);
                if (result == null)
                    return NotFound("Driver license not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving driver license: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"An error occurred while retrieving the driver license: {ex.Message}");
            }
        }

        /// <summary>
        /// Update driver license - Drivers can update their own license, Admin can update any license
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<DriverLicenseResponse>> UpdateDriverLicense(Guid id, CreateDriverLicenseRequest request)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, request.DriverId))
                {
                    return Forbid();
                }

                var result = await _driverLicenseService.UpdateDriverLicenseAsync(id, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the driver license.");
            }
        }

        /// <summary>
        /// Delete driver license - Only Admin can delete driver licenses
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDriverLicense(Guid id)
        {
            try
            {
                var result = await _driverLicenseService.DeleteDriverLicenseAsync(id);
                if (!result)
                    return NotFound("Driver license not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the driver license.");
            }
        }

        /// <summary>
        /// Upload license image - Drivers can upload their own license image, Admin can upload for any driver
        /// </summary>
        [HttpPost("license-image/{driverLicenseId}")]
        public async Task<ActionResult<object>> UploadLicenseImage(Guid driverLicenseId, IFormFile file)
        {
            try
            {
                var driverLicense = await _driverLicenseService.GetDriverLicenseByIdAsync(driverLicenseId);
                if (driverLicense == null)
                {
                    return NotFound("Driver license not found.");
                }

                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, driverLicense.DriverId))
                {
                    return Forbid();
                }

                if (file == null)
                    return BadRequest("No file provided.");

                var fileId = await _fileService.UploadLicenseImageAsync(driverLicenseId, file);
                return Ok(new { FileId = fileId, Message = "License image uploaded successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while uploading the file.");
            }
        }
    }
}
