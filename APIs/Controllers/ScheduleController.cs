using AutoMapper;
using Constants;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Schedule;

namespace APIs.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class ScheduleController : ControllerBase
	{
		private readonly IScheduleService _scheduleService;
		private readonly ILogger<ScheduleController> _logger;
		private readonly IMapper _mapper;

		public ScheduleController(IScheduleService scheduleService, ILogger<ScheduleController> logger, IMapper mapper)
		{
			_scheduleService = scheduleService;
			_logger = logger;
			_mapper = mapper;
		}

		[HttpGet]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedules(
			[FromQuery] string? scheduleType = null,
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null,
			[FromQuery] bool? activeOnly = null,
			[FromQuery] int page = 1,
			[FromQuery] int perPage = 20,
			[FromQuery] string sortBy = "createdAt",
			[FromQuery] string sortOrder = "desc")
		{
			try
			{
				var schedules = await _scheduleService.QuerySchedulesAsync(
					scheduleType, startDate, endDate, activeOnly, page, perPage, sortBy, sortOrder);

				return Ok(_mapper.Map<IEnumerable<ScheduleDto>>(schedules));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting schedules");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<ScheduleDto>> GetSchedule(Guid id)
		{
			try
			{
				var schedule = await _scheduleService.GetScheduleByIdAsync(id);
				if (schedule == null)
				{
					return NotFound(new { message = "Schedule not found" });
				}

				return Ok(_mapper.Map<ScheduleDto>(schedule));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting schedule with id: {ScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPost]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<ScheduleDto>> CreateSchedule(CreateScheduleDto request)
		{
			try
			{
				var entity = _mapper.Map<Schedule>(request);
				var created = await _scheduleService.CreateScheduleAsync(entity);
				return CreatedAtAction(nameof(GetSchedule), new { id = created.Id }, _mapper.Map<ScheduleDto>(created));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating schedule");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPut("{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateSchedule(Guid id, UpdateScheduleDto request)
		{
			try
			{
				if (id != request.Id)
				{
					return BadRequest(new { message = "ID mismatch" });
				}

				var entity = _mapper.Map<Schedule>(request);
				var updated = await _scheduleService.UpdateScheduleAsync(entity);
				if (updated == null)
				{
					return NotFound(new { message = "Schedule not found" });
				}

				return NoContent();
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating schedule");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> DeleteSchedule(Guid id)
		{
			try
			{
				var deleted = await _scheduleService.DeleteScheduleAsync(id);
				if (deleted == null)
				{
					return NotFound(new { message = "Schedule not found" });
				}

				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting schedule");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("active")]
		public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetActiveSchedules()
		{
			try
			{
				var schedules = await _scheduleService.GetActiveSchedulesAsync();
				return Ok(_mapper.Map<IEnumerable<ScheduleDto>>(schedules));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting active schedules");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("type/{scheduleType}")]
		public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedulesByType(string scheduleType)
		{
			try
			{
				var schedules = await _scheduleService.GetSchedulesByTypeAsync(scheduleType);
				return Ok(_mapper.Map<IEnumerable<ScheduleDto>>(schedules));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting schedules by type: {ScheduleType}", scheduleType);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPut("{id}/overrides")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<ScheduleDto>> AddTimeOverride(Guid id, [FromBody] ScheduleTimeOverride timeOverride)
		{
			try
			{
				var updatedSchedule = await _scheduleService.AddTimeOverrideAsync(id, timeOverride);
				if (updatedSchedule == null)
				{
					return NotFound(new { message = "Schedule not found" });
				}

				return Ok(_mapper.Map<ScheduleDto>(updatedSchedule));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding time override for schedule {ScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPut("{id}/overrides/batch")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<ScheduleDto>> AddTimeOverridesBatch(Guid id, [FromBody] List<ScheduleTimeOverride> timeOverrides)
		{
			try
			{
				var updatedSchedule = await _scheduleService.AddTimeOverridesBatchAsync(id, timeOverrides);
				if (updatedSchedule == null)
				{
					return NotFound(new { message = "Schedule not found" });
				}

				return Ok(_mapper.Map<ScheduleDto>(updatedSchedule));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding batch time overrides for schedule {ScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpDelete("{id}/overrides")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<ScheduleDto>> RemoveTimeOverride(Guid id, [FromQuery] DateTime date)
		{
			try
			{
				var updatedSchedule = await _scheduleService.RemoveTimeOverrideAsync(id, date);
				if (updatedSchedule == null)
				{
					return NotFound(new { message = "Schedule not found" });
				}

				return Ok(_mapper.Map<ScheduleDto>(updatedSchedule));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error removing time override for schedule {ScheduleId} on {Date}", id, date);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpDelete("{id}/overrides/batch")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<ScheduleDto>> RemoveTimeOverridesBatch(Guid id, [FromBody] List<DateTime> dates)
		{
			try
			{
				var updatedSchedule = await _scheduleService.RemoveTimeOverridesBatchAsync(id, dates);
				if (updatedSchedule == null)
				{
					return NotFound(new { message = "Schedule not found" });
				}

				return Ok(_mapper.Map<ScheduleDto>(updatedSchedule));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error removing batch time overrides for schedule {ScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}/overrides")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<List<ScheduleTimeOverride>>> GetTimeOverrides(Guid id)
		{
			try
			{
				var overrides = await _scheduleService.GetTimeOverridesAsync(id);
				return Ok(overrides);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting time overrides for schedule {ScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}/overrides/{date}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<ScheduleTimeOverride>> GetTimeOverride(Guid id, DateTime date)
		{
			try
			{
				var timeOverride = await _scheduleService.GetTimeOverrideAsync(id, date);
				if (timeOverride == null)
				{
					return NotFound(new { message = "Time override not found" });
				}

				return Ok(timeOverride);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting time override for schedule {ScheduleId} on {Date}", id, date);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}/dates")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<List<DateTime>>> GenerateScheduleDates(Guid id, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
		{
			try
			{
				var dates = await _scheduleService.GenerateScheduleDatesAsync(id, startDate, endDate);
				return Ok(dates);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating schedule dates for schedule {ScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}/check-date")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<bool>> IsDateMatchingSchedule(Guid id, [FromQuery] DateTime date)
		{
			try
			{
				var isMatching = await _scheduleService.IsDateMatchingScheduleAsync(id, date);
				return Ok(isMatching);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking if date matches schedule {ScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}
	}
}