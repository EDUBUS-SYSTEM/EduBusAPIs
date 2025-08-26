using Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Driver;

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

        [HttpPost]
        public async Task<ActionResult<DriverLicenseResponse>> CreateDriverLicense(CreateDriverLicenseRequest request)
        {
            try
            {
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

        [HttpGet("driver/{driverId}")]
        public async Task<ActionResult<DriverLicenseResponse>> GetDriverLicenseByDriverId(Guid driverId)
        {
            try
            {
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

        [HttpPut("{id}")]
        public async Task<ActionResult<DriverLicenseResponse>> UpdateDriverLicense(Guid id, CreateDriverLicenseRequest request)
        {
            try
            {
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

        [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
        [HttpPost("license-image/{driverLicenseId}")]
        public async Task<ActionResult<object>> UploadLicenseImage(Guid driverLicenseId, IFormFile file)
        {
            try
            {
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
