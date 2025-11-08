// backend/EduBusAPIs/APIs/Hubs/TripHub.cs
using Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Services.Contracts;
using Data.Repos.Interfaces;
using Utils;

namespace APIs.Hubs
{
    [Authorize]
    public class TripHub : Hub
    {
        private readonly ILogger<TripHub> _logger;

        public TripHub(
            ILogger<TripHub> logger,
            IDatabaseFactory databaseFactory,
            ITripService? tripService = null)
        {
            _logger = logger;
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
                Console.WriteLine($"SendLocation called: {tripId.ToString()}");
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

                // Broadcast đến tất cả parents trong group Trip_{tripId}
                await Clients.Group($"Trip_{tripId}").SendAsync("ReceiveLocationUpdate", locationData);
                _logger.LogDebug(
                    "Driver {DriverId} sent location for trip {TripId}: ({Lat}, {Lng})",
                    driverId, tripId, latitude, longitude);
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

                if (userRole != Roles.Parent || parentId == Guid.Empty)
                {
                    await Clients.Caller.SendAsync("Error", "Only parents can join trip");
                    return;
                }

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