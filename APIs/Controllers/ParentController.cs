using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Parent;
using Services.Models.UserAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Constants;
using System.Security.Claims;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParentController : ControllerBase
    {
        private readonly IParentService _parentService;
        private readonly ILogger<ParentController> _logger;
        public ParentController(IParentService parentService, ILogger<ParentController> logger)
        {
            _parentService = parentService;
            _logger = logger;
        }
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<CreateUserResponse>> CreateParent([FromBody] CreateParentRequest dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var response = await _parentService.CreateParentAsync(dto);
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
        public async Task<IActionResult> ImportParents(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file format. Only .xlsx files are supported.");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _parentService.ImportParentsFromExcelAsync(stream);

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
                    Message = "An error occurred while importing parents.",
                    Details = ex.Message
                });
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("export")]
        public async Task<IActionResult> ExportParents()
        {
            var fileContent = await _parentService.ExportParentsToExcelAsync();
            if (fileContent == null || fileContent.Length == 0)
            {
                return NotFound(new { message = "No data to export to Excel." });
            }

            return File(fileContent,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Parents.xlsx");
        }

        [Authorize(Roles = Roles.Parent)]
        [HttpGet("trip-report/{semesterId}")]
        public async Task<ActionResult<ParentTripReportResponse>> GetTripReportBySemester([FromRoute] string semesterId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User email not found in token." });
                }

                var report = await _parentService.GetTripReportBySemesterAsync(userEmail, semesterId);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trip report for semester {SemesterId}", semesterId);
                return StatusCode(500, new { message = "An error occurred while retrieving trip report." });
            }
        }
    }
}
