using Constants;
using Data.Repos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Services.Contracts;
using System.Security.Claims;
using Utils;

namespace APIs.Hubs
{
	[Authorize]
	public class TripHub : Hub
	{
		private readonly ILogger<TripHub> _logger;
		private readonly ITripService _tripService;
		private readonly IServiceScopeFactory _serviceScopeFactory;
		public TripHub(
			ILogger<TripHub> logger,
			IDatabaseFactory databaseFactory,
			ITripService tripService,
			IServiceScopeFactory serviceScopeFactory)
		{
			_logger = logger;
			_tripService = tripService;
			_serviceScopeFactory = serviceScopeFactory;
		}

		public override async Task OnConnectedAsync()
		{
			var userId = GetUserId();
			var userRole = GetUserRole();

			if (userId != Guid.Empty)
			{
				if (userRole == Roles.Driver)
				{
					await Groups.AddToGroupAsync(Context.ConnectionId, $"Driver_{userId}");
					_logger.LogInformation("Driver {DriverId} connected to TripHub. ConnectionId: {ConnectionId}",
						userId, Context.ConnectionId);
				}
				else if (userRole == Roles.Parent)
				{
					_logger.LogInformation("Parent {ParentId} connected to TripHub. ConnectionId: {ConnectionId}",
						userId, Context.ConnectionId);
				}
				else if (userRole == Roles.Admin)
				{
					await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
					_logger.LogInformation("Admin {AdminId} connected to TripHub. ConnectionId: {ConnectionId}",
						userId, Context.ConnectionId);
				}
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userId = GetUserId();
			var userRole = GetUserRole();

			if (userId != Guid.Empty)
			{
				if (userRole == Roles.Driver)
				{
					await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Driver_{userId}");
					_logger.LogInformation("Driver {DriverId} disconnected from TripHub. ConnectionId: {ConnectionId}",
						userId, Context.ConnectionId);
				}
				else if (userRole == Roles.Admin)
				{
					await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
					_logger.LogInformation("Admin {AdminId} disconnected from TripHub. ConnectionId: {ConnectionId}",
						userId, Context.ConnectionId);
				}
			}

			await base.OnDisconnectedAsync(exception);
		}

		/// <summary>
		/// Driver send location data
		/// </summary>
		public async Task SendLocation(
			Guid tripId,
			double latitude,
			double longitude,
			double? speed = null,
			double? accuracy = null,
			bool isMoving = false)
		{
			try
			{
				var driverId = GetUserId();
				var userRole = GetUserRole();
				if (userRole != Roles.Driver || driverId == Guid.Empty)
				{
					await Clients.Caller.SendAsync("Error", "Only drivers can send location updates");
					return;
				}
				var locationData = new
				{
					tripId = tripId,
					driverId = driverId,
					latitude = latitude,
					longitude = longitude,
					speed = speed,
					accuracy = accuracy,
					isMoving = isMoving,
					timestamp = DateTime.UtcNow.ToLocalTime()
				};

				// Broadcast to parents have children in this trip
				await Clients.Group($"Trip_{tripId}").SendAsync("ReceiveLocationUpdate", locationData);

				// Also broadcast to admins for real-time monitoring
				await Clients.Group("Admins").SendAsync("ReceiveLocationUpdate", locationData);

				_logger.LogDebug(
					"Driver {DriverId} sent location for trip {TripId}: ({Lat}, {Lng})",
					driverId, tripId, latitude, longitude);

				// save to database in background
				_ = Task.Run(async () =>
				{
					using var scope = _serviceScopeFactory.CreateScope();
					var tripService = scope.ServiceProvider.GetRequiredService<ITripService>();

					try
					{
						var ok = await tripService.UpdateTripLocationAsync(
							tripId, driverId, latitude, longitude, speed, accuracy, isMoving);

						if (!ok)
						{
							_logger.LogWarning(
								"Failed to update trip location: TripId={TripId}, DriverId={DriverId}",
								tripId, driverId);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex,
							"Error saving location to database: TripId={TripId}, DriverId={DriverId}",
							tripId, driverId);
					}

				});

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in SendLocation for trip {TripId}", tripId);
				await Clients.Caller.SendAsync("Error", "Failed to send location update");
			}
		}

		/// <summary>
		/// Parent join vào trip group để nhận location updates
		/// </summary>
		public async Task JoinTrip(Guid tripId)
		{
			try
			{
				var parentId = GetUserId();
				var userRole = GetUserRole();
				await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
				_logger.LogInformation("Parent {ParentId} joined trip {TripId}", parentId, tripId);

				await Clients.Caller.SendAsync("JoinedTrip", new { tripId = tripId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error joining trip {TripId}", tripId);
				await Clients.Caller.SendAsync("Error", "Failed to join trip");
			}
		}

		/// <summary>
		/// Parent leave trip group
		/// </summary>
		public async Task LeaveTrip(Guid tripId)
		{
			try
			{
				var parentId = GetUserId();
				await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
				_logger.LogInformation("Parent {ParentId} left trip {TripId}", parentId, tripId);

				await Clients.Caller.SendAsync("LeftTrip", new { tripId = tripId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error leaving trip {TripId}", tripId);
			}
		}

		/// <summary>
		/// Admin join admin monitoring group to receive all trip updates
		/// </summary>
		public async Task JoinAdminMonitoring()
		{
			try
			{
				var adminId = GetUserId();
				var userRole = GetUserRole();

				if (userRole != Roles.Admin || adminId == Guid.Empty)
				{
					await Clients.Caller.SendAsync("Error", "Only admins can join admin monitoring");
					return;
				}

				await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
				_logger.LogInformation("Admin {AdminId} joined admin monitoring", adminId);

				await Clients.Caller.SendAsync("JoinedAdminMonitoring", new { adminId = adminId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error joining admin monitoring");
				await Clients.Caller.SendAsync("Error", "Failed to join admin monitoring");
			}
		}

		private Guid GetUserId()
		{
			var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (Guid.TryParse(userIdClaim, out var userId))
			{
				return userId;
			}
			return Guid.Empty;
		}

		private string GetUserRole()
		{
			return Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? Roles.Unknown;
		}
	}
}