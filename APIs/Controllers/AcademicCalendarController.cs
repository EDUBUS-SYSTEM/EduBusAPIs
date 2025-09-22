using AutoMapper;
using Constants;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.AcademicCalendar;

namespace APIs.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicCalendarController : ControllerBase
    {
        private readonly IAcademicCalendarService _academicCalendarService;
        private readonly ILogger<AcademicCalendarController> _logger;
        private readonly IMapper _mapper;

        public AcademicCalendarController(
            IAcademicCalendarService academicCalendarService, 
            ILogger<AcademicCalendarController> logger, 
            IMapper mapper)
        {
            _academicCalendarService = academicCalendarService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<IEnumerable<AcademicCalendarDto>>> GetAcademicCalendars(
            [FromQuery] string? academicYear = null,
            [FromQuery] bool? activeOnly = null,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20,
            [FromQuery] string sortBy = "createdAt",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                var calendars = await _academicCalendarService.QueryAcademicCalendarsAsync(
                    academicYear, activeOnly, page, perPage, sortBy, sortOrder);

                return Ok(_mapper.Map<IEnumerable<AcademicCalendarDto>>(calendars));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting academic calendars");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<AcademicCalendarDto>> GetAcademicCalendar(Guid id)
        {
            try
            {
                var calendar = await _academicCalendarService.GetAcademicCalendarByIdAsync(id);
                if (calendar == null)
                {
                    return NotFound(new { message = "Academic calendar not found" });
                }

                return Ok(_mapper.Map<AcademicCalendarDto>(calendar));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting academic calendar with id: {CalendarId}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<AcademicCalendarDto>> CreateAcademicCalendar([FromBody] AcademicCalendarCreateDto createDto)
        {
            try
            {
                var calendar = _mapper.Map<AcademicCalendar>(createDto);
                var createdCalendar = await _academicCalendarService.CreateAcademicCalendarAsync(calendar);
                return CreatedAtAction(nameof(GetAcademicCalendar), new { id = createdCalendar.Id }, _mapper.Map<AcademicCalendarDto>(createdCalendar));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating academic calendar");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateAcademicCalendar(Guid id, [FromBody] AcademicCalendarUpdateDto updateDto)
        {
            try
            {
                if (updateDto.Id != id)
                {
                    return BadRequest(new { message = "ID mismatch between route and request body" });
                }

                var calendar = _mapper.Map<AcademicCalendar>(updateDto);

                var updatedCalendar = await _academicCalendarService.UpdateAcademicCalendarAsync(calendar);
                if (updatedCalendar == null)
                {
                    return NotFound(new { message = "Academic calendar not found" });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating academic calendar");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteAcademicCalendar(Guid id)
        {
            try
            {
                var deleted = await _academicCalendarService.DeleteAcademicCalendarAsync(id);
                if (deleted == null)
                {
                    return NotFound(new { message = "Academic calendar not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting academic calendar");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<AcademicCalendarDto>>> GetActiveAcademicCalendars()
        {
            try
            {
                var calendars = await _academicCalendarService.GetActiveAcademicCalendarsAsync();
                return Ok(_mapper.Map<IEnumerable<AcademicCalendarDto>>(calendars));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active academic calendars");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("year/{academicYear}")]
        public async Task<ActionResult<AcademicCalendarDto>> GetAcademicCalendarByYear(string academicYear)
        {
            try
            {
                var calendar = await _academicCalendarService.GetAcademicCalendarByYearAsync(academicYear);
                if (calendar == null)
                {
                    return NotFound(new { message = "Academic calendar not found for year: " + academicYear });
                }

                return Ok(_mapper.Map<AcademicCalendarDto>(calendar));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting academic calendar for year: {AcademicYear}", academicYear);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}/holidays")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<List<SchoolHoliday>>> GetHolidays(Guid id)
        {
            try
            {
                var holidays = await _academicCalendarService.GetHolidaysAsync(id);
                return Ok(holidays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting holidays for academic calendar {CalendarId}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}/school-days")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<List<SchoolDay>>> GetSchoolDays(Guid id)
        {
            try
            {
                var schoolDays = await _academicCalendarService.GetSchoolDaysAsync(id);
                return Ok(schoolDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting school days for academic calendar {CalendarId}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}/is-school-day")]
        public async Task<ActionResult<bool>> IsSchoolDay(Guid id, [FromQuery] DateTime date)
        {
            try
            {
                var isSchoolDay = await _academicCalendarService.IsSchoolDayAsync(id, date);
                return Ok(isSchoolDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if date is school day for academic calendar {CalendarId}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}
