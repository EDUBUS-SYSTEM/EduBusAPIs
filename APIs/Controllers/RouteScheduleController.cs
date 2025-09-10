using AutoMapper;
using Constants;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.RouteSchedule;

namespace APIs.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class RouteScheduleController : ControllerBase
	{
		private readonly IRouteScheduleService _routeScheduleService;
		private readonly ILogger<RouteScheduleController> _logger;
		private readonly IMapper _mapper;

		public RouteScheduleController(IRouteScheduleService routeScheduleService, ILogger<RouteScheduleController> logger, IMapper mapper)
		{
			_routeScheduleService = routeScheduleService;
			_logger = logger;
			_mapper = mapper;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<RouteScheduleDto>>> GetRouteSchedules(
			[FromQuery] Guid? routeId = null,
			[FromQuery] Guid? scheduleId = null,
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null,
			[FromQuery] bool? activeOnly = null,
			[FromQuery] int page = 1,
			[FromQuery] int perPage = 20,
			[FromQuery] string sortBy = "effectiveFrom",
			[FromQuery] string sortOrder = "desc")
		{
			try
			{
				var items = await _routeScheduleService.QueryRouteSchedulesAsync(
					routeId, scheduleId, startDate, endDate, activeOnly,
					page, perPage, sortBy, sortOrder);

				return Ok(_mapper.Map<IEnumerable<RouteScheduleDto>>(items));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedules");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<RouteScheduleDto>> GetRouteSchedule(Guid id)
		{
			try
			{
				var routeSchedule = await _routeScheduleService.GetRouteScheduleByIdAsync(id);
				if (routeSchedule == null)
				{
					return NotFound(new { message = "Route schedule not found" });
				}

				return Ok(_mapper.Map<RouteScheduleDto>(routeSchedule));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedule with id: {RouteScheduleId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPost]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<RouteScheduleDto>> CreateRouteSchedule(CreateRouteScheduleDto request)
		{
			try
			{
				var entity = _mapper.Map<RouteSchedule>(request);
				var createdRouteSchedule = await _routeScheduleService.CreateRouteScheduleAsync(entity);
				return CreatedAtAction(nameof(GetRouteSchedule), new { id = createdRouteSchedule.Id }, _mapper.Map<RouteScheduleDto>(createdRouteSchedule));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating route schedule");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPut("{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateRouteSchedule(Guid id, UpdateRouteScheduleDto request)
		{
			try
			{
				if (id != request.Id)
				{
					return BadRequest(new { message = "ID mismatch" });
				}

				var entity = _mapper.Map<RouteSchedule>(request);
				var updatedRouteSchedule = await _routeScheduleService.UpdateRouteScheduleAsync(entity);
				if (updatedRouteSchedule == null)
				{
					return NotFound(new { message = "Route schedule not found" });
				}

				return NoContent();
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating route schedule");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> DeleteRouteSchedule(Guid id)
		{
			try
			{
				var deletedRouteSchedule = await _routeScheduleService.DeleteRouteScheduleAsync(id);
				if (deletedRouteSchedule == null)
				{
					return NotFound(new { message = "Route schedule not found" });
				}

				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting route schedule");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("active")]
		public async Task<ActionResult<IEnumerable<RouteScheduleDto>>> GetActiveRouteSchedules()
		{
			try
			{
				var routeSchedules = await _routeScheduleService.GetActiveRouteSchedulesAsync();
				return Ok(_mapper.Map<IEnumerable<RouteScheduleDto>>(routeSchedules));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting active route schedules");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("route/{routeId}")]
		public async Task<ActionResult<IEnumerable<RouteScheduleDto>>> GetRouteSchedulesByRoute(Guid routeId)
		{
			try
			{
				var routeSchedules = await _routeScheduleService.GetRouteSchedulesByRouteAsync(routeId);
				return Ok(_mapper.Map<IEnumerable<RouteScheduleDto>>(routeSchedules));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedules by route: {RouteId}", routeId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("schedule/{scheduleId}")]
		public async Task<ActionResult<IEnumerable<RouteScheduleDto>>> GetRouteSchedulesBySchedule(Guid scheduleId)
		{
			try
			{
				var routeSchedules = await _routeScheduleService.GetRouteSchedulesByScheduleAsync(scheduleId);
				return Ok(_mapper.Map<IEnumerable<RouteScheduleDto>>(routeSchedules));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedules by schedule: {ScheduleId}", scheduleId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}
	}
}