
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.Notification;
using Services.Models.VietMap;
using Constants;
namespace Services.Backgrounds
{
    public class PickupApproachNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PickupApproachNotificationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); 
        private const int APPROACH_THRESHOLD_METERS = 1000; 

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
                    Builders<Trip>.Filter.Eq(t => t.Status, TripConstants.TripStatus.InProgress),
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

            // Find all pending stops (not arrived yet), sorted by sequence order
            var pendingStops = trip.Stops
                //.Where(s => s.ArrivedAt == null)
                .OrderBy(s => s.SequenceOrder)
                .ToList();

            if (!pendingStops.Any())
            {
                _logger.LogDebug("Trip {TripId} has no pending stops", trip.Id);
                return;
            }

            _logger.LogDebug("Trip {TripId} has {Count} pending stops to check", trip.Id, pendingStops.Count);

            // Process each pending stop
            foreach (var pendingStop in pendingStops)
            {
                try
                {
                    await ProcessPendingStopAsync(
                        trip, 
                        pendingStop, 
                        vietMapService, 
                        notificationService, 
                        tripService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stop {StopOrder} for trip {TripId}", 
                        pendingStop.SequenceOrder, trip.Id);
                }
            }
        }

        private async Task ProcessPendingStopAsync(
            Trip trip,
            TripStop pendingStop,
            IVietMapService vietMapService,
            INotificationService notificationService,
            ITripService tripService)
        {
            // Base notification key per trip and pickup point (sequence order may change)
            // Include TripType to differentiate between Departure and Return trips
            var tripType = trip.ScheduleSnapshot?.TripType ?? TripType.Unknown;
            var notificationKeyPrefix = $"pickup_approach_{tripType}_{trip.Id}_{pendingStop.PickupPointId}";

            // Get all parents who have children at this pickup point
            var parentAssignments = await tripService.GetParentStudentAssignmentsForPickupPointAsync(
                trip.Id, pendingStop.PickupPointId);

            if (!parentAssignments.Any())
            {
                _logger.LogDebug("No parents found for trip {TripId}, stop {StopOrder}",
                    trip.Id, pendingStop.SequenceOrder);
                return;
            }

            var parentGroups = parentAssignments
                .GroupBy(a => a.ParentId)
                .Select(g => new
                {
                    ParentId = g.Key,
                    StudentNames = g.Select(a => a.StudentName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                })
                .ToList();

            if (!parentGroups.Any())
            {
                _logger.LogDebug("No parent groups with students found for trip {TripId}, stop {StopOrder}",
                    trip.Id, pendingStop.SequenceOrder);
                return;
            }

            // Call VietMap API to calculate route to this stop
            var routeResult = await vietMapService.GetRouteAsync(
                trip.CurrentLocation.Latitude,
                trip.CurrentLocation.Longitude,
                pendingStop.Location.Latitude,
                pendingStop.Location.Longitude,
                "car"
            );

            if (routeResult == null)
            {
                _logger.LogWarning("Failed to calculate route for trip {TripId}, stop {StopOrder}",
                    trip.Id, pendingStop.SequenceOrder);
                return;
            }

            // Convert distance from km to meters
            var distanceMeters = routeResult.Distance * 1000;

            _logger.LogDebug("Trip {TripId}, Stop {SequenceOrder}: {Meters}m away ({Minutes} minutes)",
                trip.Id, pendingStop.SequenceOrder, distanceMeters, routeResult.DurationMinutes);

            // Check if within threshold (based on distance)
            if (distanceMeters > APPROACH_THRESHOLD_METERS)
            {
                _logger.LogDebug("Stop {StopOrder} for trip {TripId} is {Meters}m away, not within threshold",
                    pendingStop.SequenceOrder, trip.Id, distanceMeters);
                return;
            }

            var notificationTasks = parentGroups.Select(async parentGroup =>
            {
                var notificationKey = $"{notificationKeyPrefix}_{parentGroup.ParentId}";

                var existingNotification = await notificationService.GetNotificationByMetadataAsync(
                    parentGroup.ParentId, "Trip", notificationKey);

                if (existingNotification != null)
                {
                    _logger.LogDebug("Notification already sent to parent {ParentId} for trip {TripId}, stop {StopOrder}", 
                        parentGroup.ParentId, trip.Id, pendingStop.SequenceOrder);
                    return;
                }

                var studentList = string.Join(", ", parentGroup.StudentNames);
                if (string.IsNullOrWhiteSpace(studentList))
                {
                    studentList = "Student";
                }

                await SendApproachNotificationToParentAsync(
                    trip, pendingStop, routeResult,
                    notificationService, parentGroup.ParentId, notificationKey, studentList, parentGroup.StudentNames);
            });

            await Task.WhenAll(notificationTasks);
        }

        private async Task SendApproachNotificationToParentAsync(
            Trip trip,
            TripStop stop,
            RouteResult routeResult,
            INotificationService notificationService,
            Guid parentId,
            string notificationKey,
            string studentList,
            IReadOnlyCollection<string> studentNames)
        {
            try
            {
                var minutes = (int)Math.Ceiling(routeResult.DurationMinutes);
                var distanceMeters = (int)Math.Round(routeResult.Distance * 1000); // Convert km to meters
                var studentArray = studentNames?.Any() == true
                    ? studentNames.ToArray()
                    : Array.Empty<string>();

                // Customize notification based on TripType
                var tripType = trip.ScheduleSnapshot?.TripType ?? TripType.Unknown;
                string title;
                string message;

                switch (tripType)
                {
                    case TripType.Departure:
                        title = "The bus is approaching the pickup point";
                        message = $"{studentList} Please get ready for school!";
                        break;
                    case TripType.Return:
                        title = "The bus is approaching the drop-off point";
                        message = $"{studentList} Please get ready, the bus is almost home!";
                        break;
                    default:
                        title = "The bus is approaching";
                        message = $"{studentList} Please get ready!";
                        break;
                }

                var notificationDto = new CreateNotificationDto
                {
                    UserId = parentId,
                    Title = title,
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
                        { "distanceMeters", distanceMeters },
                        { "notificationKey", notificationKey },
                        { "students", studentArray },
                        { "tripType", tripType.ToString() }
                    }
                };

                await notificationService.CreateNotificationAsync(notificationDto);
                _logger.LogInformation("Sent approach notification to parent {ParentId} for trip {TripId}, stop {StopOrder} ({Meters}m away)",
                    parentId, trip.Id, stop.SequenceOrder, distanceMeters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approach notification to parent {ParentId} for trip {TripId}, stop {StopOrder}",
                    parentId, trip.Id, stop.SequenceOrder);
            }
        }
    }
}