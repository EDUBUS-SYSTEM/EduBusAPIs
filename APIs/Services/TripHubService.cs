using Microsoft.AspNetCore.SignalR;
using APIs.Hubs;
using Services.Contracts;

namespace APIs.Services
{
	public class TripHubService : ITripHubService
	{
		private readonly IHubContext<TripHub> _hubContext;
		private readonly ILogger<TripHubService> _logger;

		public TripHubService(
			IHubContext<TripHub> hubContext,
			ILogger<TripHubService> logger)
		{
			_hubContext = hubContext;
			_logger = logger;
		}

		public async Task BroadcastTripStatusChangedAsync(Guid tripId, string status, DateTime? startTime, DateTime? endTime)
		{
			try
			{
				var data = new
				{
					tripId = tripId,
					status = status,
					startTime = startTime,
					endTime = endTime,
					timestamp = DateTime.UtcNow
				};

				await _hubContext.Clients.Group("Admins").SendAsync("TripStatusChanged", data);
			
			// Broadcast to parents tracking this trip
			await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("TripStatusChanged", data);
				_logger.LogInformation("Broadcasted trip status change to admins and parents: TripId={TripId}, Status={Status}", tripId, status);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error broadcasting trip status change: TripId={TripId}", tripId);
			}
		}

		public async Task BroadcastAttendanceUpdatedAsync(Guid tripId, Guid stopId, object attendanceSummary)
		{
			try
			{
				var data = new
				{
					tripId = tripId,
					stopId = stopId,
					attendance = attendanceSummary,
					timestamp = DateTime.UtcNow
				};

				await _hubContext.Clients.Group("Admins").SendAsync("AttendanceUpdated", data);
				await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("AttendanceUpdated", data);
				
				_logger.LogInformation("Broadcasted attendance update to admins and parents: TripId={TripId}, StopId={StopId}", tripId, stopId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error broadcasting attendance update: TripId={TripId}", tripId);
			}
		}

		public async Task BroadcastStopArrivalAsync(Guid tripId, Guid stopId, DateTime arrivedAt)
		{
			try
			{
				var data = new
				{
					tripId = tripId,
					stopId = stopId,
					arrivedAt = arrivedAt
				};

				// Broadcast to parents tracking this trip
				await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("StopArrivalConfirmed", data);	
				
				_logger.LogInformation("Broadcasted stop arrival: TripId={TripId}, StopId={StopId}", tripId, stopId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error broadcasting stop arrival: TripId={TripId}, StopId={StopId}", tripId, stopId);
			}
		}

		public async Task BroadcastStopsReorderedAsync(Guid tripId, List<object> stops)
		{
			try
			{
				var data = new
				{
					tripId = tripId,
					stops = stops,
					timestamp = DateTime.UtcNow
				};

				// Broadcast to parents tracking this trip
				await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("StopsReordered", data);
				
				// Also broadcast to admins for real-time monitoring
				await _hubContext.Clients.Group("Admins").SendAsync("StopsReordered", data);
				
				_logger.LogInformation("Broadcasted stops reordered: TripId={TripId}, StopsCount={Count}", tripId, stops.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error broadcasting stops reordered: TripId={TripId}", tripId);
			}
		}
	}
}