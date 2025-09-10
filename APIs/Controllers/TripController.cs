using AutoMapper;
using Constants;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Trip;

namespace APIs.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class TripController : ControllerBase
	{
		private readonly ITripService _tripService;
		private readonly ILogger<TripController> _logger;
		private readonly IMapper _mapper;

		public TripController(ITripService tripService, ILogger<TripController> logger, IMapper mapper)
		{
			_tripService = tripService;
			_logger = logger;
			_mapper = mapper;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips(
			[FromQuery] Guid? routeId = null,
			[FromQuery] DateTime? serviceDate = null,
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null,
			[FromQuery] string? status = null,
			[FromQuery] int? upcomingDays = null,
			[FromQuery] int page = 1,
			[FromQuery] int perPage = 20,
			[FromQuery] string sortBy = "serviceDate",
			[FromQuery] string sortOrder = "desc")
		{
			try
			{
				// Preserve current logic: upcoming path keeps its own semantics
				if (upcomingDays.HasValue)
				{
					var tripsUpcoming = await _tripService.GetUpcomingTripsAsync(DateTime.UtcNow, upcomingDays.Value);
					return Ok(_mapper.Map<IEnumerable<TripDto>>(tripsUpcoming));
				}

				// Database-level filtering + pagination + sorting
				var result = await _tripService.QueryTripsAsync(
					routeId, serviceDate, startDate, endDate, status,
					page, perPage, sortBy, sortOrder);

				return Ok(_mapper.Map<IEnumerable<TripDto>>(result));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<TripDto>> GetTrip(Guid id)
		{
			try
			{
				var trip = await _tripService.GetTripByIdAsync(id);
				if (trip == null)
				{
					return NotFound(new { message = "Trip not found" });
				}

				return Ok(_mapper.Map<TripDto>(trip));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip with id: {TripId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPost]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<TripDto>> CreateTrip(CreateTripDto request)
		{
			try
			{
				var entity = _mapper.Map<Trip>(request);
				var createdTrip = await _tripService.CreateTripAsync(entity);
				return CreatedAtAction(nameof(GetTrip), new { id = createdTrip.Id }, _mapper.Map<TripDto>(createdTrip));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating trip");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPut("{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> UpdateTrip(Guid id, UpdateTripDto request)
		{
			try
			{
				if (id != request.Id)
				{
					return BadRequest(new { message = "ID mismatch" });
				}

				var entity = _mapper.Map<Trip>(request);
				var updatedTrip = await _tripService.UpdateTripAsync(entity);
				if (updatedTrip == null)
				{
					return NotFound(new { message = "Trip not found" });
				}

				return NoContent();
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating trip");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<IActionResult> DeleteTrip(Guid id)
		{
			try
			{
				var deletedTrip = await _tripService.DeleteTripAsync(id);
				if (deletedTrip == null)
				{
					return NotFound(new { message = "Trip not found" });
				}

				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting trip");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		// Preserve current logic-specific endpoints (DTO outputs)

		[HttpGet("route/{routeId}")]
		public async Task<ActionResult<IEnumerable<TripDto>>> GetTripsByRoute(Guid routeId)
		{
			try
			{
				var trips = await _tripService.GetTripsByRouteAsync(routeId);
				return Ok(_mapper.Map<IEnumerable<TripDto>>(trips));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by route: {RouteId}", routeId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("date/{serviceDate:datetime}")]
		public async Task<ActionResult<IEnumerable<TripDto>>> GetTripsByDate(DateTime serviceDate)
		{
			try
			{
				var trips = await _tripService.GetTripsByDateAsync(serviceDate);
				return Ok(_mapper.Map<IEnumerable<TripDto>>(trips));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by date: {ServiceDate}", serviceDate);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("upcoming")]
		public async Task<ActionResult<IEnumerable<TripDto>>> GetUpcomingTrips([FromQuery] int days = 7)
		{
			try
			{
				var trips = await _tripService.GetUpcomingTripsAsync(DateTime.UtcNow, days);
				return Ok(_mapper.Map<IEnumerable<TripDto>>(trips));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting upcoming trips for {Days} days", days);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPost("generate-from-schedule")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<IEnumerable<TripDto>>> GenerateTripsFromSchedule(
			[FromQuery] Guid scheduleId,
			[FromQuery] DateTime startDate,
			[FromQuery] DateTime endDate)
		{
			try
			{
				var trips = await _tripService.GenerateTripsFromScheduleAsync(scheduleId, startDate, endDate);
				return Ok(_mapper.Map<IEnumerable<TripDto>>(trips));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating trips from schedule: {ScheduleId}", scheduleId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}
	}
}