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

        public DriverLicenseController(IDriverLicenseService driverLicenseService)
        {
            _driverLicenseService = driverLicenseService;
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
                return StatusCode(500, "An error occurred while retrieving the driver license.");
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
    }
}
