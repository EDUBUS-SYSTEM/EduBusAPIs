using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Jetson;

namespace APIs.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class JetsonController : ControllerBase
	{
		private readonly IJetsonService _jetsonService;
		private readonly ILogger<JetsonController> _logger;

		public JetsonController(IJetsonService jetsonService, ILogger<JetsonController> logger)
		{
			_jetsonService = jetsonService;
			_logger = logger;
		}

		
		[HttpGet("active-trip")]
		public async Task<ActionResult<ActiveTripResponse>> GetActiveTrip([FromQuery] string plate)
		{
			if (string.IsNullOrEmpty(plate))
				return BadRequest("License plate is required");

			try
			{
				var result = await _jetsonService.GetActiveTripForPlateAsync(plate);
				if (result == null)
					return NotFound(new { message = "No active trip found for this vehicle" });
				
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching active trip for plate {Plate}", plate);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		[HttpGet("{deviceId}/students")]
		public async Task<ActionResult<JetsonStudentSyncResponse>> GetStudentsForSync(
			string deviceId,
			[FromQuery] Guid routeId)
		{
			try
			{
				_logger.LogInformation("Jetson device {DeviceId} requesting student sync for route {RouteId}", 
					deviceId, routeId);

				var response = await _jetsonService.GetStudentsForSyncAsync(deviceId, routeId);
				
				_logger.LogInformation("Successfully synced {Count} students for device {DeviceId}", 
					response.TotalStudents, deviceId);

				return Ok(response);
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Route not found: {RouteId}", routeId);
				return NotFound(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error syncing students for device: {DeviceId}, route: {RouteId}", 
					deviceId, routeId);
				return StatusCode(500, new { message = "Internal server error", error = ex.Message });
			}
		}
	}
}
