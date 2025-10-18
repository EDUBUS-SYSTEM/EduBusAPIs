using AutoMapper;
using Constants;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Trip;
using MongoDB.Driver;
using Utils;
using Data.Repos.Interfaces;

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
		private readonly IDatabaseFactory _databaseFactory;

		public TripController(ITripService tripService, ILogger<TripController> logger, IMapper mapper, IDatabaseFactory databaseFactory)
		{
			_tripService = tripService;
			_logger = logger;
			_mapper = mapper;
			_databaseFactory = databaseFactory;
		}

		[HttpGet]
		[Authorize(Roles = Roles.Admin)]
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
		[Authorize(Roles = Roles.Admin)]
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

		[HttpPost("generate-all-automatic")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<object>> GenerateAllTripsAutomatic(
			[FromQuery] int daysAhead = 7)
		{
			try
			{
				var startDate = DateTime.UtcNow.Date;
				var endDate = startDate.AddDays(daysAhead);

				var scheduleRepo = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				var routeScheduleRepo = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);

				// Get all active schedules
				var activeSchedules = await scheduleRepo.FindByFilterAsync(
					Builders<Schedule>.Filter.And(
						Builders<Schedule>.Filter.Eq(s => s.IsActive, true),
						Builders<Schedule>.Filter.Eq(s => s.IsDeleted, false),
						Builders<Schedule>.Filter.Lte(s => s.EffectiveFrom, endDate),
						Builders<Schedule>.Filter.Or(
							Builders<Schedule>.Filter.Eq(s => s.EffectiveTo, null),
							Builders<Schedule>.Filter.Gte(s => s.EffectiveTo, startDate)
						)
					)
				);

				var totalGenerated = 0;
				var processedSchedules = 0;
				var results = new List<object>();

				foreach (var schedule in activeSchedules)
				{
					try
					{
						// Check if schedule has active route schedules
						var routeSchedules = await routeScheduleRepo.GetRouteSchedulesByScheduleAsync(schedule.Id);
						var activeRouteSchedules = routeSchedules.Where(rs => rs.IsActive && !rs.IsDeleted).ToList();

						if (!activeRouteSchedules.Any())
							continue;

						// Generate trips for this schedule
						var generatedTrips = await _tripService.GenerateTripsFromScheduleAsync(
							schedule.Id, 
							startDate, 
							endDate
						);

						var tripCount = generatedTrips.Count();
						totalGenerated += tripCount;
						processedSchedules++;

						results.Add(new
						{
							scheduleId = schedule.Id,
							scheduleName = schedule.Name,
							tripCount = tripCount,
							routeScheduleCount = activeRouteSchedules.Count
						});
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error generating trips for schedule {ScheduleId}", schedule.Id);
						results.Add(new
						{
							scheduleId = schedule.Id,
							scheduleName = schedule.Name,
							error = ex.Message
						});
					}
				}

				return Ok(new
				{
					message = "Automatic trip generation completed",
					startDate = startDate,
					endDate = endDate,
					processedSchedules = processedSchedules,
					totalGenerated = totalGenerated,
					results = results
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in automatic trip generation");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		#region Enhanced Trip Management Endpoints

		[HttpPut("{id}/status")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<object>> UpdateTripStatus(Guid id, [FromBody] UpdateTripStatusRequest request)
		{
			try
			{
				var success = await _tripService.UpdateTripStatusAsync(id, request.Status, request.Reason);
				if (!success)
				{
					return NotFound(new { message = "Trip not found" });
				}

				return Ok(new { 
					tripId = id, 
					status = request.Status, 
					reason = request.Reason,
					message = "Trip status updated successfully" 
				});
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating trip status: {TripId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPut("{id}/attendance")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<object>> UpdateAttendance(Guid id, [FromBody] UpdateAttendanceRequest request)
		{
			try
			{
				var success = await _tripService.UpdateAttendanceAsync(id, request.StopId, request.StudentId, request.State);
				if (!success)
				{
					return NotFound(new { message = "Trip, stop, or student not found" });
				}

				return Ok(new { 
					tripId = id, 
					stopId = request.StopId, 
					studentId = request.StudentId,
					state = request.State,
					message = "Attendance updated successfully" 
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating attendance: {TripId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}/stops")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<IEnumerable<TripStopDto>>> GetTripStops(Guid id)
		{
			try
			{
				var trip = await _tripService.GetTripByIdAsync(id);
				if (trip == null)
				{
					return NotFound(new { message = "Trip not found" });
				}

				return Ok(_mapper.Map<IEnumerable<TripStopDto>>(trip.Stops));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip stops: {TripId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{id}/attendance")]
		[Authorize(Roles = Roles.Admin)]
		public async Task<ActionResult<object>> GetTripAttendance(Guid id)
		{
			try
			{
				var trip = await _tripService.GetTripByIdAsync(id);
				if (trip == null)
				{
					return NotFound(new { message = "Trip not found" });
				}

				var attendanceSummary = trip.Stops.SelectMany(s => s.Attendance)
					.GroupBy(a => a.State)
					.ToDictionary(g => g.Key, g => g.Count());

				return Ok(new { 
					tripId = id, 
					attendanceSummary = attendanceSummary,
					totalStops = trip.Stops.Count,
					totalAttendanceRecords = trip.Stops.Sum(s => s.Attendance.Count)
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip attendance: {TripId}", id);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		#endregion
	}
}