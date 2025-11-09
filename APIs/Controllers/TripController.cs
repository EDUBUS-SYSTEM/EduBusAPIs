using AutoMapper;
using Constants;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Trip;
using System.Security.Claims;
using Utils;

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
					return Ok(MapTripsToDto(tripsUpcoming));
				}

				// Database-level filtering + pagination + sorting (stops populated)
				var trips = await _tripService.QueryTripsAsync(
					routeId, serviceDate, startDate, endDate, status,
					page, perPage, sortBy, sortOrder);

				return Ok(MapTripsToDto(trips));
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
				var trip = await _tripService.GetTripWithStopsAsync(id);
				if (trip == null)
				{
					return NotFound(new { message = "Trip not found" });
				}

				return Ok(MapTripToDto(trip));
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
				return CreatedAtAction(nameof(GetTrip), new { id = createdTrip.Id }, MapTripToDto(createdTrip));
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
				var tripsList = trips.ToList();
				var tripDtos = _mapper.Map<IEnumerable<TripDto>>(tripsList).ToList();

				// Map stops for each trip
				foreach (var tripDto in tripDtos)
				{
					var trip = tripsList.FirstOrDefault(t => t.Id == tripDto.Id);
					if (trip != null)
					{
						tripDto.Stops = MapStopsToDto(trip);
					}
				}

				return Ok(tripDtos);
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
				var trips = await _tripService.GetTripsByDateWithDetailsAsync(serviceDate);
				var tripsList = trips.ToList();

				// Map to DTOs
				var tripDtos = _mapper.Map<IEnumerable<TripDto>>(tripsList).ToList();

				// Map stops for each trip
				foreach (var tripDto in tripDtos)
				{
					var trip = tripsList.FirstOrDefault(t => t.Id == tripDto.Id);
					if (trip != null)
					{
						tripDto.Stops = MapStopsToDto(trip);
					}
				}

				return Ok(tripDtos);
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
				var tripsList = trips.ToList();
				var tripDtos = _mapper.Map<IEnumerable<TripDto>>(tripsList).ToList();
				
				foreach (var tripDto in tripDtos)
				{
					var trip = tripsList.FirstOrDefault(t => t.Id == tripDto.Id);
					if (trip != null)
					{
						tripDto.Stops = MapStopsToDto(trip);
					}
				}
				
				return Ok(tripDtos);
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
				return Ok(MapTripsToDto(trips));
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
				var result = await _tripService.GenerateAllTripsAutomaticAsync(daysAhead);
				return Ok(result);
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
				var trip = await _tripService.GetTripWithStopsAsync(id);
				if (trip == null)
				{
					return NotFound(new { message = "Trip not found" });
				}

				var stops = MapStopsToDto(trip);
				return Ok(stops);
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

		[HttpGet("driver/{driverId}/schedule/date/{serviceDate:datetime}")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<IEnumerable<DriverScheduleDto>>> GetDriverScheduleByDate(Guid driverId, DateTime serviceDate)
		{
			try
			{
				var trips = await _tripService.GetDriverScheduleByDateAsync(driverId, serviceDate);
				return Ok(_mapper.Map<IEnumerable<DriverScheduleDto>>(trips));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver schedule by date: {DriverId}, {ServiceDate}", driverId, serviceDate);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("driver/{driverId}/schedule/range")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<IEnumerable<DriverScheduleDto>>> GetDriverScheduleByRange(
			Guid driverId,
			[FromQuery] DateTime startDate,
			[FromQuery] DateTime endDate)
		{
			try
			{
				var trips = await _tripService.GetDriverScheduleByRangeAsync(driverId, startDate, endDate);
				return Ok(_mapper.Map<IEnumerable<DriverScheduleDto>>(trips));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver schedule by range: {DriverId}, {StartDate} to {EndDate}", driverId, startDate, endDate);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("driver/{driverId}/schedule/upcoming")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<IEnumerable<DriverScheduleDto>>> GetDriverUpcomingSchedule(
			Guid driverId,
			[FromQuery] int days = 7)
		{
			try
			{
				var trips = await _tripService.GetDriverUpcomingScheduleAsync(driverId, days);
				return Ok(_mapper.Map<IEnumerable<DriverScheduleDto>>(trips));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver upcoming schedule: {DriverId}, {Days} days", driverId, days);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("driver/{driverId}/schedule/summary")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<DriverScheduleSummaryDto>> GetDriverScheduleSummary(
			Guid driverId,
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null)
		{
			try
			{
				var start = startDate ?? DateTime.UtcNow.Date;
				var end = endDate ?? start.AddDays(30);
				
				var summary = await _tripService.GetDriverScheduleSummaryAsync(driverId, start, end);
				return Ok(_mapper.Map<DriverScheduleSummaryDto>(summary));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver schedule summary: {DriverId}", driverId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		#region Driver Trip 

		[HttpGet("by-date")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<object>> GetTripsByDate([FromQuery] DateTime? date = null)
		{
			try
			{
				var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
				if (!driverId.HasValue)
				{
					return Unauthorized(new { message = "User ID not found in token" });
				}

				var targetDate = date ?? DateTime.UtcNow.Date;
				var trips = await _tripService.GetTripsByDateForDriverAsync(driverId.Value, targetDate);
				
				// Map to simple DTO with only required fields
				var simpleTrips = trips.Select(trip => new SimpleTripDto
				{
					Name = trip.ScheduleSnapshot?.Name ?? string.Empty,
					PlannedStartAt = trip.PlannedStartAt,
					PlannedEndAt = trip.PlannedEndAt,
					PlateVehicle = trip.Vehicle?.MaskedPlate ?? string.Empty,
					Status = trip.Status
				});
				
				return Ok(new { 
					date = targetDate,
					trips = simpleTrips
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by date for driver: {Date}", date);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("{tripId}/detail-for-driver")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<TripDto>> GetTripDetailForDriver(Guid tripId)
		{
			try
			{
				var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
				if (!driverId.HasValue)
				{
					return Unauthorized(new { message = "User ID not found in token" });
				}

				var trip = await _tripService.GetTripDetailForDriverWithStopsAsync(tripId, driverId.Value);
				if (trip == null)
				{
					return NotFound(new { message = "Trip not found or you don't have access to this trip" });
				}

				return Ok(MapTripToDto(trip));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip detail for driver: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPost("{tripId}/start")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<object>> StartTrip(Guid tripId)
		{
			try
			{
				var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
				if (!driverId.HasValue)
				{
					return Unauthorized(new { message = "User ID not found in token" });
				}

				var success = await _tripService.StartTripAsync(tripId, driverId.Value);
				if (!success)
				{
					return BadRequest(new { message = "Cannot start trip. Trip not found, you don't have access, or trip is not in Scheduled status" });
				}

				return Ok(new { 
					tripId = tripId, 
					message = "Trip started successfully",
					startedAt = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error starting trip: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPost("{tripId}/end")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<object>> EndTrip(Guid tripId)
		{
			try
			{
				var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
				if (!driverId.HasValue)
				{
					return Unauthorized(new { message = "User ID not found in token" });
				}

				var success = await _tripService.EndTripAsync(tripId, driverId.Value);
				if (!success)
				{
					return BadRequest(new { message = "Cannot end trip. Trip not found, you don't have access, or trip is not in InProgress status" });
				}

				return Ok(new { 
					tripId = tripId, 
					message = "Trip ended successfully",
					endedAt = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error ending trip: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPost("{tripId}/location")]
		[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
		public async Task<ActionResult<object>> UpdateTripLocation(Guid tripId, [FromBody] UpdateTripLocationRequest request)
		{
			try
			{
				var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
				if (!driverId.HasValue)
				{
					return Unauthorized(new { message = "User ID not found in token" });
				}

				var success = await _tripService.UpdateTripLocationAsync(
					tripId, 
					driverId.Value, 
					request.Latitude, 
					request.Longitude, 
					request.Speed, 
					request.Accuracy, 
					request.IsMoving
				);

				if (!success)
				{
					return BadRequest(new { message = "Cannot update location. Trip not found or you don't have access to this trip" });
				}

				return Ok(new { 
					tripId = tripId, 
					message = "Location updated successfully",
					updatedAt = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating trip location: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		#endregion

		#region Parent Endpoints

		[HttpGet("parent/upcoming")]
		[Authorize(Roles = Roles.Parent)]
		public async Task<ActionResult<IEnumerable<TripDto>>> GetUpcomingTripsForParent([FromQuery] int days = 7)
		{
			try
			{
				var parentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
				if (string.IsNullOrEmpty(parentEmail))
				{
					return Unauthorized(new { message = "Email not found in token" });
				}

				var trips = await _tripService.GetTripsByScheduleForParentAsync(parentEmail, days);
				var tripDtos = _mapper.Map<IEnumerable<TripDto>>(trips).ToList();

				foreach (var tripDto in tripDtos)
				{
					var trip = trips.FirstOrDefault(t => t.Id == tripDto.Id);
					if (trip != null)
					{
						tripDto.Stops = MapStopsToDto(trip);
					}
				}

				return Ok(tripDtos);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting upcoming trips for parent");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("parent/date")]
		[Authorize(Roles = Roles.Parent)]
		public async Task<ActionResult<IEnumerable<TripDto>>> GetTripsByDateForParent([FromQuery] DateTime? date = null)
		{
			try
			{
				var parentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
				if (string.IsNullOrEmpty(parentEmail))
				{
					return Unauthorized(new { message = "Email not found in token" });
				}

				var trips = await _tripService.GetTripsByDateForParentAsync(parentEmail, date);
				var tripDtos = _mapper.Map<IEnumerable<TripDto>>(trips).ToList();

				foreach (var tripDto in tripDtos)
				{
					var trip = trips.FirstOrDefault(t => t.Id == tripDto.Id);
					if (trip != null)
					{
						tripDto.Stops = MapStopsToDto(trip);
					}
				}

				return Ok(tripDtos);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by date for parent");
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("parent/{tripId}")]
		[Authorize(Roles = Roles.Parent)]
		public async Task<ActionResult<TripDto>> GetTripDetailForParent(Guid tripId)
		{
			try
			{
				var parentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
				if (string.IsNullOrEmpty(parentEmail))
				{
					return Unauthorized(new { message = "Email not found in token" });
				}

				var trip = await _tripService.GetTripDetailForParentAsync(tripId, parentEmail);
				if (trip == null)
				{
					return NotFound(new { message = "Trip not found or you don't have access to this trip" });
				}

				return Ok(MapTripToDto(trip));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip detail for parent: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpGet("parent/{tripId}/location")]
		[Authorize(Roles = Roles.Parent)]
		public async Task<ActionResult<Trip.VehicleLocation>> GetTripCurrentLocationForParent(Guid tripId)
		{
			try
			{
				var parentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
				if (string.IsNullOrEmpty(parentEmail))
				{
					return Unauthorized(new { message = "Email not found in token" });
				}

				var location = await _tripService.GetTripCurrentLocationAsync(tripId, parentEmail);
				if (location == null)
				{
					return NotFound(new { message = "Trip not found, location not available, or you don't have access to this trip" });
				}

				return Ok(location);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip current location for parent: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		#endregion

		private TripDto? MapTripToDto(Trip? trip)
		{
			if (trip == null) return null;
			
			var tripDto = _mapper.Map<TripDto>(trip);
			tripDto.Stops = MapStopsToDto(trip);
			return tripDto;
		}

		private IEnumerable<TripDto> MapTripsToDto(IEnumerable<Trip> trips)
		{
			if (trips == null) return Enumerable.Empty<TripDto>();
			
			var tripsList = trips.ToList();
			var tripDtos = _mapper.Map<IEnumerable<TripDto>>(tripsList).ToList();
			
			foreach (var tripDto in tripDtos)
			{
				var trip = tripsList.FirstOrDefault(t => t.Id == tripDto.Id);
				if (trip != null)
				{
					tripDto.Stops = MapStopsToDto(trip);
				}
			}
			
			return tripDtos;
		}

		private List<TripStopDto> MapStopsToDto(Trip trip)
		{
			if (trip.Stops == null || !trip.Stops.Any())
				return new List<TripStopDto>();

			return trip.Stops
				.Where(stop => stop.PickupPointId != Guid.Empty) // Filter out stops with empty PickupPointId
				.Select(stop => new TripStopDto
				{
					Id = stop.PickupPointId,
					Name = stop.Location?.Address ?? string.Empty, // Use Address as Name since service populates it
					PlannedArrival = stop.PlannedAt,
					ActualArrival = stop.ArrivedAt,
					PlannedDeparture = stop.PlannedAt,
					ActualDeparture = stop.DepartedAt,
					Sequence = stop.SequenceOrder,
					Attendance = stop.Attendance?.Select(a => new ParentAttendanceDto
					{
						StudentId = a.StudentId,
						StudentName = a.StudentName ?? string.Empty,
						BoardedAt = a.BoardedAt,
						State = a.State ?? string.Empty
					}).ToList() ?? new List<ParentAttendanceDto>()
				})
				.OrderBy(s => s.Sequence)
				.ToList();
		}

	}
}