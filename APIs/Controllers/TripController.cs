using AutoMapper;
using Constants;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Services.Contracts;
using Services.Implementations;
using Services.Models.Notification;
using Services.Models.Trip;
using System.Security.Claims;
using Utils;
using Route = Data.Models.Route;

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
		public async Task<ActionResult<object>> GetTrips(
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
				// Handle upcomingDays special case (returns all upcoming trips, no pagination)
				if (upcomingDays.HasValue)
				{
					var tripsUpcoming = await _tripService.GetUpcomingTripsAsync(DateTime.UtcNow, upcomingDays.Value);
					var tripDtosUpcoming = MapTripsToDto(tripsUpcoming).ToList();
					await PopulateRouteNamesAndVehiclePlatesAsync(tripDtosUpcoming, tripsUpcoming);
					// Return as simple array for upcoming trips (no pagination needed)
					return Ok(tripDtosUpcoming);
				}

				// Use new paginated method for regular queries
				var response = await _tripService.QueryTripsWithPaginationAsync(
					routeId, serviceDate, startDate, endDate, status,
					page, perPage, sortBy, sortOrder);

				// Map trips to DTOs using MapTripsToDto to avoid AutoMapper issues
				var tripDtos = MapTripsToDto(response.Trips).ToList();

				// Map stops for each trip
				foreach (var tripDto in tripDtos)
				{
					var trip = response.Trips.FirstOrDefault(t => t.Id == tripDto.Id);
					if (trip != null)
					{
						tripDto.Stops = MapStopsToDto(trip);
					}
				}

				// Populate route names and vehicle plates
				await PopulateRouteNamesAndVehiclePlatesAsync(tripDtos, response.Trips);

				// Extract computed properties to avoid CS0828 error
				var hasNextPage = response.HasNextPage;
				var hasPreviousPage = response.HasPreviousPage;

				// Return paginated response with DTOs
				return Ok(new
				{
					data = tripDtos,
					total = response.TotalCount,
					page = response.Page,
					perPage = response.PerPage,
					totalPages = response.TotalPages,
					hasNextPage = hasNextPage,
					hasPreviousPage = hasPreviousPage
				});
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
			var tripDtos = MapTripsToDto(tripsList).ToList();

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

			// Map to DTOs using MapTripsToDto
			var tripDtos = MapTripsToDto(tripsList).ToList();

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
			var tripDtos = MapTripsToDto(tripsList).ToList();
			
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

		[HttpPut("{tripId}/stops/arrange")]
		[Authorize(Roles = $"{Roles.Driver}")]
		public async Task<ActionResult<object>> ArrangeStopSequence(Guid tripId, [FromBody] ArrangeStopRequest request)
		{
			try
			{
				var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
				if (!driverId.HasValue)
				{
					return Unauthorized(new { message = "User ID not found in token" });
				}

				var trip = await _tripService.ArrangeStopSequenceAsync(
					tripId,
					driverId.Value,
					request.PickupPointId,
					request.NewSequenceOrder);

				if (trip == null)
				{
					return NotFound(new { message = "Trip not found or you don't have access to this trip" });
				}

				// Return sorted pickup points
				var sortedStops = trip.Stops
					.OrderBy(s => s.SequenceOrder)
					.Select(s => new
					{
						pickupPointId = s.PickupPointId,
						sequenceOrder = s.SequenceOrder,
						address = s.Location?.Address,
						arrivedAt = s.ArrivedAt,
						departedAt = s.DepartedAt
					})
					.ToList();

				return Ok(new
				{
					tripId = tripId,
					stops = sortedStops,
					message = "Stop sequence updated successfully",
					updatedAt = DateTime.UtcNow
				});
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
				_logger.LogError(ex, "Error arranging stop sequence: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		[HttpPut("{tripId}/stops/arrange-multiple")]
		[Authorize(Roles = $"{Roles.Driver}")]
		public async Task<ActionResult<object>> UpdateMultipleStopsSequence(Guid tripId, [FromBody] UpdateMultipleStopsSequenceRequest request)
		{
			try
			{
				var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
				if (!driverId.HasValue)
				{
					return Unauthorized(new { message = "User ID not found in token" });
				}

				if (request.Stops == null || !request.Stops.Any())
				{
					return BadRequest(new { message = "Stops list cannot be empty" });
				}

				// Convert request to tuple list
				var stopSequences = request.Stops
					.Select(s => (s.PickupPointId, s.SequenceOrder))
					.ToList();

				var trip = await _tripService.UpdateMultipleStopsSequenceAsync(
					tripId,
					driverId.Value,
					stopSequences);

				if (trip == null)
				{
					return NotFound(new { message = "Trip not found or you don't have access to this trip" });
				}

				// Return sorted pickup points
				var sortedStops = trip.Stops
					.OrderBy(s => s.SequenceOrder)
					.Select(s => new
					{
						pickupPointId = s.PickupPointId,
						sequenceOrder = s.SequenceOrder,
						address = s.Location?.Address,
						arrivedAt = s.ArrivedAt,
						departedAt = s.DepartedAt
					})
					.ToList();

				return Ok(new
				{
					tripId = tripId,
					stops = sortedStops,
					message = "Multiple stops sequence updated successfully",
					updatedAt = DateTime.UtcNow
				});
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
				_logger.LogError(ex, "Error updating multiple stops sequence: {TripId}", tripId);
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
					Id = trip.Id,
					Name = trip.ScheduleSnapshot?.Name ?? string.Empty,
					PlannedStartAt = trip.PlannedStartAt,
					PlannedEndAt = trip.PlannedEndAt,
					PlateVehicle = trip.Vehicle?.MaskedPlate ?? string.Empty,
					Status = trip.Status,
                    TotalStops = trip.Stops?.Count(s => s.PickupPointId != Guid.Empty) ?? 0,
                    CompletedStops = trip.Stops?.Count(s => s.DepartedAt != null && s.PickupPointId != Guid.Empty) ?? 0
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

        [HttpPost("{tripId}/stops/{stopId}/confirm-arrival")]
        [Authorize(Roles = $"{Roles.Driver}")]
        public async Task<ActionResult<object>> ConfirmArrival(Guid tripId, Guid stopId)
        {
            try
            {
                var driverId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
                if (!driverId.HasValue)
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }
                try
                {
                    await _tripService.ConfirmArrivalAtStopAsync(tripId, stopId, driverId.Value);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
                return Ok(new
                {
                    tripId = tripId,
                    stopId = stopId,
                    message = "Arrival confirmed and parents notified",
                    confirmedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming arrival at stop: {TripId}, {StopId}", tripId, stopId);
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
				var tripDtos = MapTripsToDto(trips).ToList();

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
			var tripDtos = MapTripsToDto(trips).ToList();

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

		#region Supervisor Manual Attendance Endpoints

		/// <summary>
		/// Get list of students for attendance in a trip
		/// </summary>
		[HttpGet("{tripId}/students-for-attendance")]
		[Authorize(Roles = Roles.Supervisor)]
		public async Task<ActionResult<StudentsForAttendanceResponse>> GetStudentsForAttendance(Guid tripId)
		{
			try
			{
				var response = await _tripService.GetStudentsForAttendanceAsync(tripId);
				return Ok(response);
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Resource not found: {TripId}", tripId);
				return NotFound(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting students for attendance: {TripId}", tripId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}

		/// <summary>
		/// Supervisor marks manual attendance (Boarding or Alighting)
		/// </summary>
		[HttpPost("{tripId}/attendance/manual")]
		[Authorize(Roles = Roles.Supervisor)]
		public async Task<ActionResult> SubmitManualAttendance(
			Guid tripId,
			[FromBody] ManualAttendanceRequest request)
		{
			try
			{
				var (success, message, studentId, timestamp) = await _tripService.SubmitManualAttendanceAsync(tripId, request);
				
				return Ok(new { 
					success = success, 
					message = message,
					studentId = studentId,
					timestamp = timestamp
				});
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid request: {TripId}", tripId);
				return BadRequest(new { message = ex.Message });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Resource not found: {TripId}", tripId);
				return NotFound(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error submitting manual attendance: {TripId}", tripId);
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
			var tripDtos = new List<TripDto>();
			
			foreach (var trip in tripsList)
			{
				var tripDto = MapTripToDto(trip);
				if (tripDto != null)
				{
					tripDtos.Add(tripDto);
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
					Location = stop.Location != null ? new StopLocationDto
					{
						Latitude = stop.Location.Latitude,
						Longitude = stop.Location.Longitude,
						Address = stop.Location.Address
					} : null,
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

		private async Task PopulateRouteNamesAndVehiclePlatesAsync(List<TripDto> tripDtos, IEnumerable<Trip> trips)
		{
			try
			{
				// Get unique route IDs
				var routeIds = tripDtos.Select(t => t.RouteId).Distinct().ToList();

				// Fetch routes from MongoDB
				var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
				var routes = new List<Route>();

				foreach (var routeId in routeIds)
				{
					try
					{
						var route = await routeRepo.FindAsync(routeId);
						if (route != null && !route.IsDeleted)
						{
							routes.Add(route);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error fetching route {RouteId} for trip", routeId);
					}
				}

				// Create route lookup dictionary
				var routeLookup = routes.ToDictionary(r => r.Id, r => r.RouteName);

				// Populate route names in trip DTOs
				foreach (var tripDto in tripDtos)
				{
					if (routeLookup.TryGetValue(tripDto.RouteId, out var routeName))
					{
						tripDto.RouteName = routeName;
					}

					// Ensure vehicle plate is populated (it should already be set in QueryTripsAsync)
					// But we verify it's there from the trip entity
					var trip = trips.FirstOrDefault(t => t.Id == tripDto.Id);
					if (trip != null && trip.Vehicle != null && !string.IsNullOrEmpty(trip.Vehicle.MaskedPlate))
					{
						if (tripDto.Vehicle == null)
						{
							tripDto.Vehicle = new VehicleSnapshotDto
							{
								Id = trip.Vehicle.Id,
								MaskedPlate = trip.Vehicle.MaskedPlate,
								Capacity = trip.Vehicle.Capacity,
								Status = trip.Vehicle.Status
							};
						}
						else if (string.IsNullOrEmpty(tripDto.Vehicle.MaskedPlate))
						{
							tripDto.Vehicle.MaskedPlate = trip.Vehicle.MaskedPlate;
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error populating route names and vehicle plates");
				// Don't throw - just log the error
			}
		}

	}
}