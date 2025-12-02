using AutoMapper;
using Constants;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.EnrollmentSemesterSettings;

namespace APIs.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentSemesterSettingsController : ControllerBase
    {
        private readonly IEnrollmentSemesterSettingsService _service;
        private readonly ILogger<EnrollmentSemesterSettingsController> _logger;
        private readonly IMapper _mapper;

        public EnrollmentSemesterSettingsController(
            IEnrollmentSemesterSettingsService service,
            ILogger<EnrollmentSemesterSettingsController> logger,
            IMapper mapper)
        {
            _service = service;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<EnrollmentSemesterSettingsQueryResultDto>> GetEnrollmentSemesterSettings(
            [FromQuery] string? semesterCode = null,
            [FromQuery] string? academicYear = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                var result = await _service.QueryAsync(
                    semesterCode,
                    academicYear,
                    isActive,
                    search,
                    page,
                    perPage,
                    sortBy,
                    sortOrder);

                var dto = new EnrollmentSemesterSettingsQueryResultDto
                {
                    Items = _mapper.Map<List<EnrollmentSemesterSettingsDto>>(result.Items),
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PerPage = result.PerPage,
                    TotalPages = result.TotalPages
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment semester settings");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<EnrollmentSemesterSettingsDto>> GetEnrollmentSemesterSetting(Guid id)
        {
            try
            {
                var settings = await _service.GetByIdAsync(id);
                if (settings == null)
                {
                    return NotFound(new { message = "Enrollment semester settings not found" });
                }

                return Ok(_mapper.Map<EnrollmentSemesterSettingsDto>(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment semester settings with id: {Id}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<EnrollmentSemesterSettingsDto>> CreateEnrollmentSemesterSetting(
            [FromBody] EnrollmentSemesterSettingsCreateDto createDto)
        {
            try
            {
                var settings = await _service.CreateAsync(createDto);
                return CreatedAtAction(
                    nameof(GetEnrollmentSemesterSetting),
                    new { id = settings.Id },
                    _mapper.Map<EnrollmentSemesterSettingsDto>(settings));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating enrollment semester settings");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateEnrollmentSemesterSetting(
            Guid id,
            [FromBody] EnrollmentSemesterSettingsUpdateDto updateDto)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, updateDto);
                if (updated == null)
                {
                    return NotFound(new { message = "Enrollment semester settings not found" });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enrollment semester settings with id: {Id}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteEnrollmentSemesterSetting(Guid id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "Enrollment semester settings not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting enrollment semester settings with id: {Id}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<EnrollmentSemesterSettingsDto>> GetActiveSettings()
        {
            try
            {
                var settings = await _service.GetActiveSettingsAsync();
                if (settings == null)
                {
                    return NotFound(new { message = "No active enrollment semester settings found" });
                }

                return Ok(_mapper.Map<EnrollmentSemesterSettingsDto>(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active enrollment semester settings");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("current-open")]
        public async Task<ActionResult<EnrollmentSemesterSettingsDto>> GetCurrentOpenRegistration()
        {
            try
            {
                var settings = await _service.GetCurrentOpenRegistrationAsync();
                if (settings == null)
                {
                    return NotFound(new { message = "No open registration found" });
                }

                return Ok(_mapper.Map<EnrollmentSemesterSettingsDto>(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current open registration");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("semester-code/{semesterCode}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<EnrollmentSemesterSettingsDto>> GetBySemesterCode(string semesterCode)
        {
            try
            {
                var settings = await _service.FindBySemesterCodeAsync(semesterCode);
                if (settings == null)
                {
                    return NotFound(new { message = $"Enrollment semester settings not found for code: {semesterCode}" });
                }

                return Ok(_mapper.Map<EnrollmentSemesterSettingsDto>(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment semester settings by code: {SemesterCode}", semesterCode);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("academic-year/{academicYear}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<List<EnrollmentSemesterSettingsDto>>> GetByAcademicYear(string academicYear)
        {
            try
            {
                var settings = await _service.FindByAcademicYearAsync(academicYear);
                return Ok(_mapper.Map<List<EnrollmentSemesterSettingsDto>>(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment semester settings by academic year: {AcademicYear}", academicYear);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}

