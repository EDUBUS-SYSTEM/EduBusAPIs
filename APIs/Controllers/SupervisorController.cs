using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Supervisor;
using Services.Models.UserAccount;
using Constants;
using Microsoft.AspNetCore.Authorization;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SupervisorController : ControllerBase
    {
        private readonly ISupervisorService _supervisorService;

        public SupervisorController(ISupervisorService supervisorService)
        {
            _supervisorService = supervisorService;
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<CreateUserResponse>> CreateSupervisor([FromBody] CreateSupervisorRequest dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var response = await _supervisorService.CreateSupervisorAsync(dto);
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
        public async Task<IActionResult> ImportSupervisors(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required. Please select an Excel (.xlsx) file to import.");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest($"Only Excel files with .xlsx extension are supported. Provided file: {file.FileName}");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _supervisorService.ImportSupervisorsFromExcelAsync(stream);

                return Ok(new
                {
                    TotalProcessed = result.TotalProcessed,
                    TotalSuccess = result.SuccessfulUsers.Count,
                    TotalFailed = result.FailedUsers.Count,
                    SuccessUsers = result.SuccessfulUsers,
                    FailedUsers = result.FailedUsers
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error while importing supervisors: {ex.Message}");
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("export")]
        public async Task<IActionResult> ExportSupervisors()
        {
            var fileContent = await _supervisorService.ExportSupervisorsToExcelAsync();
            if (fileContent == null || fileContent.Length == 0)
            {
                return NotFound(new { message = "No data to export to Excel." });
            }

            return File(fileContent,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Supervisors.xlsx");
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupervisorResponse>>> GetAllSupervisors()
        {
            try
            {
                var supervisors = await _supervisorService.GetAllSupervisorsAsync();
                return Ok(supervisors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving supervisors.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupervisorResponse>> GetSupervisorById(Guid id)
        {
            try
            {
                var supervisor = await _supervisorService.GetSupervisorResponseByIdAsync(id);
                if (supervisor == null)
                {
                    return NotFound("Supervisor not found.");
                }

                return Ok(supervisor);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the supervisor.");
            }
        }
    }
}

