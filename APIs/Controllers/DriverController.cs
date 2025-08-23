using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Implementations;
using Services.Models.Driver;
using Services.Models.Parent;
using Services.Models.UserAccount;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        public DriverController(IDriverService driverService)
        {
            _driverService = driverService;
        }
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
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportDrivers([FromForm] IFormFile file)
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
    }
}
