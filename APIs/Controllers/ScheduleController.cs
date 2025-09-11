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
	}
}