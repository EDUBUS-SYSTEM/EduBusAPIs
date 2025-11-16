
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Implementations;
using Services.Models.Notification;
using Services.Models.VietMap;
using Data.Models.Enums;
namespace Services.Backgrounds
{
    public class PickupApproachNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PickupApproachNotificationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every 1 minute
        private const int APPROACH_THRESHOLD_MINUTES = 10; // Notify when 5 minutes away

        public PickupApproachNotificationService(
            IServiceScopeFactory scopeFactory,
            ILogger<PickupApproachNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PickupApproachNotificationService started at: {time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await ProcessApproachingPickupsAsync(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PickupApproachNotificationService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("PickupApproachNotificationService stopped at: {time}", DateTime.UtcNow);
        }

        private async Task ProcessApproachingPickupsAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var tripRepo = serviceProvider.GetRequiredService<ITripRepository>();
                var vietMapService = serviceProvider.GetRequiredService<IVietMapService>();
                var notificationService = serviceProvider.GetRequiredService<INotificationService>();
                var tripService = serviceProvider.GetRequiredService<ITripService>();

                // Query trips that are InProgress and have current location
                TimeZoneInfo vnTimeZone;
                try
                {
                    // Try Windows timezone ID first
                    vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    try
                    {
                        // Fallback to Linux/Unix timezone ID
                        vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        // Last fallback
                        vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Saigon");
                    }
                }
                var nowInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                var startOfDay = nowInVietnam.Date;
                var endOfDay = startOfDay.AddDays(1);
                var filter = Builders<Trip>.Filter.And(
                    Builders<Trip>.Filter.Eq(t => t.Status, Constants.TripStatus.InProgress),
                    Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
                    Builders<Trip>.Filter.Ne(t => t.CurrentLocation, null),
                    Builders<Trip>.Filter.Gte(t => t.ServiceDate, startOfDay),
                    Builders<Trip>.Filter.Lt(t => t.ServiceDate, endOfDay)
                );

                var activeTrips = await tripRepo.FindByFilterAsync(filter);

                if (!activeTrips.Any())
                {
                    _logger.LogDebug("No active trips with location found");
                    return;
                }

                _logger.LogInformation("Processing {Count} active trips for pickup approach notifications", activeTrips.Count());

                foreach (var trip in activeTrips)
                {
                    try
                    {
                        await ProcessTripAsync(trip, vietMapService, notificationService, tripService);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing trip {TripId}", trip.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessApproachingPickupsAsync");
            }
        }

        private async Task ProcessTripAsync(
            Trip trip,
            IVietMapService vietMapService,
            INotificationService notificationService,
            ITripService tripService)
        {
            if (trip.CurrentLocation == null || trip.Stops == null || !trip.Stops.Any())
            {
                return;
            }
            // Find all pending pickup points (not arrived yet)
            var pendingStops = trip.Stops
                .Where(s => s.ArrivedAt == null)
                .ToList();

            if (!pendingStops.Any())
            {
                _logger.LogDebug("Trip {TripId} has no pending stops", trip.Id);
                return;
            }
            // Calculate route time for all pending stops
            var stopsWithTime = new List<(TripStop stop, double durationMinutes)>();

            foreach (var stop in pendingStops)
            {
                var routeResult = await vietMapService.GetRouteAsync(
                    trip.CurrentLocation.Latitude,
                    trip.CurrentLocation.Longitude,
                    stop.Location.Latitude,
                    stop.Location.Longitude,
                    "car"
                );

                if (routeResult != null)
                {
                    stopsWithTime.Add((stop, routeResult.DurationMinutes));
                    _logger.LogDebug("Trip {TripId}, Stop {SequenceOrder}: {Minutes} minutes away",
                        trip.Id, stop.SequenceOrder, routeResult.DurationMinutes);
                }
            }

            if (!stopsWithTime.Any())
            {
                _logger.LogWarning("Failed to calculate routes for any stops in trip {TripId}", trip.Id);
                return;
            }
            // Find stops within threshold (10 minutes)
            var approachingStops = stopsWithTime
                .Where(s => s.durationMinutes <= APPROACH_THRESHOLD_MINUTES)
                .ToList();

            if (!approachingStops.Any())
            {
                _logger.LogDebug("No stops approaching within {Threshold} minutes for trip {TripId}",
                    APPROACH_THRESHOLD_MINUTES, trip.Id);
                return;
            }
            // Send notification for all approaching stops (if not already sent)
            foreach (var (stop, durationMinutes) in approachingStops)
            {
                // Get all parents who have children at this pickup point
                var parents = await tripService.GetParentsForPickupPointAsync(trip.Id, stop.PickupPointId);
                
                if (!parents.Any())
                {
                    _logger.LogDebug("No parents found for trip {TripId}, stop {StopOrder}",
                        trip.Id, stop.SequenceOrder);
                    continue;
                }

                // Create a RouteResult for the notification
                var routeResult = new RouteResult
                {
                    DurationMinutes = durationMinutes,
                };

                var notificationKey = $"pickup_approach_{trip.Id}_{stop.SequenceOrder}";
                
                // Check and send notification for each parent individually
                var notifiedCount = 0;
                foreach (var parentId in parents)
                {
                    // Check if this specific parent already received notification for this stop
                    var existingNotification = await notificationService.GetNotificationByMetadataAsync(
                        parentId,        
                        "Trip",          
                        notificationKey);

                    if (existingNotification != null)
                    {
                        _logger.LogDebug("Notification already sent to parent {ParentId} for trip {TripId}, stop {StopOrder}",
                            parentId, trip.Id, stop.SequenceOrder);
                        continue;
                    }

                    // Send notification to this parent
                    await SendApproachNotificationToParentAsync(
                        trip,
                        stop,
                        routeResult,
                        notificationService,
                        parentId,
                        notificationKey);

                    notifiedCount++;
                }

                if (notifiedCount > 0)
                {
                    _logger.LogInformation("Sent approach notification to {Count} parents for trip {TripId}, stop {StopOrder} ({Minutes} minutes away)",
                        notifiedCount, trip.Id, stop.SequenceOrder, durationMinutes);
                }
            }

        }

        private async Task SendApproachNotificationToParentAsync(
            Trip trip,
            TripStop stop,
            RouteResult routeResult,
            INotificationService notificationService,
            Guid parentId,
            string notificationKey)
        {
            try
            {
                var minutes = (int)Math.Ceiling(routeResult.DurationMinutes);
                var message = "Please get ready!";

                var notificationDto = new CreateNotificationDto
                {
                    UserId = parentId,
                    Title = "The bus is approaching the pickup point",
                    Message = message,
                    NotificationType = NotificationType.TripInfo,
                    RecipientType = RecipientType.Parent,
                    RelatedEntityId = trip.Id,
                    RelatedEntityType = "Trip",
                    Metadata = new Dictionary<string, object>
                    {
                        { "tripId", trip.Id.ToString() },
                        { "stopSequenceOrder", stop.SequenceOrder },
                        { "pickupPointId", stop.PickupPointId.ToString() },
                        { "estimatedMinutes", minutes },
                        { "notificationKey", notificationKey }
                    }
                };

                await notificationService.CreateNotificationAsync(notificationDto);
                _logger.LogInformation("Sent approach notification to parent {ParentId} for trip {TripId}, stop {StopOrder}",
                    parentId, trip.Id, stop.SequenceOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approach notification to parent {ParentId} for trip {TripId}, stop {StopOrder}",
                    parentId, trip.Id, stop.SequenceOrder);
            }
        }
    }
}