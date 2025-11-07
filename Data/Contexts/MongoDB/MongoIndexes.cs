using Data.Models;
using MongoDB.Driver;

namespace Data.Contexts.MongoDB
{
    public static class MongoIndexes
    {
        public static async Task EnsureAsync(IMongoDatabase db)
        {
            // Notification indexes
            await CreateNotificationIndexesAsync(db);
            
            // Route indexes
            await CreateRouteIndexesAsync(db);
            
            // Schedule indexes
            await CreateScheduleIndexesAsync(db);
            
            // RouteSchedule indexes
            await CreateRouteScheduleIndexesAsync(db);
            
            // Trip indexes
            await CreateTripIndexesAsync(db);

            // Trip location history indexes
            await CreateTripLocationHistoryIndexesAsync(db);
            
            // FileStorage indexes
            await CreateFileStorageIndexesAsync(db);

            //PickupPointRequest
            await CreatePickupPointRequestIndexesAsync(db);

        }

        private static async Task CreateNotificationIndexesAsync(IMongoDatabase db)
        {
            var notificationCollection = db.GetCollection<Notification>("notifications");
            
            // Index for user notifications
            var userKeys = Builders<Notification>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.TimeStamp);
            await notificationCollection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(userKeys));
            
            // Index for notification type and status
            var typeStatusKeys = Builders<Notification>.IndexKeys
                .Ascending(x => x.NotificationType)
                .Ascending(x => x.Status)
                .Descending(x => x.TimeStamp);
            await notificationCollection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(typeStatusKeys));
        }

        private static async Task CreateRouteIndexesAsync(IMongoDatabase db)
        {
            var routeCollection = db.GetCollection<Route>("routes");
            
            // Index for active routes
            var activeKeys = Builders<Route>.IndexKeys
                .Ascending(x => x.IsActive)
                .Ascending(x => x.RouteName);
            await routeCollection.Indexes.CreateOneAsync(new CreateIndexModel<Route>(activeKeys));
            
            // Index for vehicle assignment
            var vehicleKeys = Builders<Route>.IndexKeys.Ascending(x => x.VehicleId);
            await routeCollection.Indexes.CreateOneAsync(new CreateIndexModel<Route>(vehicleKeys));
        }

        private static async Task CreateScheduleIndexesAsync(IMongoDatabase db)
        {
            var scheduleCollection = db.GetCollection<Schedule>("schedules");
            
            // Index for active schedules
            var activeKeys = Builders<Schedule>.IndexKeys
                .Ascending(x => x.IsActive)
                .Ascending(x => x.ScheduleType);
            await scheduleCollection.Indexes.CreateOneAsync(new CreateIndexModel<Schedule>(activeKeys));
            
            // Index for effective date range
            var effectiveKeys = Builders<Schedule>.IndexKeys
                .Ascending(x => x.EffectiveFrom)
                .Ascending(x => x.EffectiveTo);
            await scheduleCollection.Indexes.CreateOneAsync(new CreateIndexModel<Schedule>(effectiveKeys));
        }

        private static async Task CreateRouteScheduleIndexesAsync(IMongoDatabase db)
        {
            var routeScheduleCollection = db.GetCollection<RouteSchedule>("route_schedules");
            
            // Index for route schedules
            var routeKeys = Builders<RouteSchedule>.IndexKeys
                .Ascending(x => x.RouteId)
                .Ascending(x => x.IsActive);
            await routeScheduleCollection.Indexes.CreateOneAsync(new CreateIndexModel<RouteSchedule>(routeKeys));
            
            // Index for schedule assignments
            var scheduleKeys = Builders<RouteSchedule>.IndexKeys
                .Ascending(x => x.ScheduleId)
                .Ascending(x => x.IsActive);
            await routeScheduleCollection.Indexes.CreateOneAsync(new CreateIndexModel<RouteSchedule>(scheduleKeys));
            
            // Index for effective date range
            var effectiveKeys = Builders<RouteSchedule>.IndexKeys
                .Ascending(x => x.EffectiveFrom)
                .Ascending(x => x.EffectiveTo);
            await routeScheduleCollection.Indexes.CreateOneAsync(new CreateIndexModel<RouteSchedule>(effectiveKeys));
        }

        private static async Task CreateTripIndexesAsync(IMongoDatabase db)
        {
            var tripCollection = db.GetCollection<Trip>("trips");
            
            // Index for route trips
            var routeKeys = Builders<Trip>.IndexKeys
                .Ascending(x => x.RouteId)
                .Descending(x => x.ServiceDate);
            await tripCollection.Indexes.CreateOneAsync(new CreateIndexModel<Trip>(routeKeys));
            
            // Index for service date and status
            var serviceKeys = Builders<Trip>.IndexKeys
                .Ascending(x => x.ServiceDate)
                .Ascending(x => x.Status);
            await tripCollection.Indexes.CreateOneAsync(new CreateIndexModel<Trip>(serviceKeys));
            
            // Index for planned time range
            var plannedKeys = Builders<Trip>.IndexKeys
                .Ascending(x => x.PlannedStartAt)
                .Ascending(x => x.PlannedEndAt);
            await tripCollection.Indexes.CreateOneAsync(new CreateIndexModel<Trip>(plannedKeys));

            //VehicleId + ServiceDate (list trip today)
            var vehicleDay = Builders<Trip>.IndexKeys
                .Ascending(x => x.VehicleId)
                .Ascending(x => x.ServiceDate);
            await tripCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Trip>(vehicleDay, new CreateIndexOptions { Name = "idx_trip_vehicleId_serviceDate" })
            );

            // Driver snapshot + ServiceDate (only trip had driver)
            var driverKeys = Builders<Trip>.IndexKeys
                .Ascending("driver.id")
                .Descending(x => x.ServiceDate);
            await tripCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Trip>(
                    driverKeys,
                    new CreateIndexOptions<Trip>
                    {
                        Name = "idx_trip_driverId_serviceDate",
                        PartialFilterExpression = Builders<Trip>.Filter.Exists("driver.id", true)
                    })
            );

            // Actual Start/End (filter follow actual time) — only apply when had startTime
            var actualKeys = Builders<Trip>.IndexKeys
                .Ascending(x => x.StartTime)
                .Ascending(x => x.EndTime);
            await tripCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Trip>(
                    actualKeys,
                    new CreateIndexOptions<Trip>
                    {
                        Name = "idx_trip_actualStart_actualEnd",
                        PartialFilterExpression = Builders<Trip>.Filter.Ne(t => t.StartTime, null)
                    })
            );

            // Query follow assignment 
            var driverVehicleIdx = Builders<Trip>.IndexKeys.Ascending(x => x.DriverVehicleId);
            await tripCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<Trip>(driverVehicleIdx, new CreateIndexOptions { Name = "idx_trip_driverVehicleId" })
            );
        }

        private static async Task CreateFileStorageIndexesAsync(IMongoDatabase db)
        {
            var fileStorageCollection = db.GetCollection<FileStorage>("file_storage");
            
            // Index for entity files
            var entityKeys = Builders<FileStorage>.IndexKeys
                .Ascending(x => x.EntityId)
                .Ascending(x => x.EntityType)
                .Ascending(x => x.FileType);
            await fileStorageCollection.Indexes.CreateOneAsync(new CreateIndexModel<FileStorage>(entityKeys));
            
            // Index for active files
            var activeKeys = Builders<FileStorage>.IndexKeys
                .Ascending(x => x.IsActive)
                .Descending(x => x.CreatedAt);
            await fileStorageCollection.Indexes.CreateOneAsync(new CreateIndexModel<FileStorage>(activeKeys));
            
            // Index for file type
            var fileTypeKeys = Builders<FileStorage>.IndexKeys
                .Ascending(x => x.FileType)
                .Ascending(x => x.IsActive);
            await fileStorageCollection.Indexes.CreateOneAsync(new CreateIndexModel<FileStorage>(fileTypeKeys));
        }
        private static async Task CreatePickupPointRequestIndexesAsync(IMongoDatabase db)
        {
            var col = db.GetCollection<PickupPointRequestDocument>("pickuppointrequestdocument");

            var emailIdx = Builders<PickupPointRequestDocument>.IndexKeys
                .Ascending(x => x.ParentEmail)
                .Descending(x => x.CreatedAt);
            await col.Indexes.CreateOneAsync(new CreateIndexModel<PickupPointRequestDocument>(emailIdx));

            var statusIdx = Builders<PickupPointRequestDocument>.IndexKeys
                .Ascending(x => x.Status)
                .Descending(x => x.CreatedAt);
            await col.Indexes.CreateOneAsync(new CreateIndexModel<PickupPointRequestDocument>(statusIdx));
        }

        private static async Task CreateTripLocationHistoryIndexesAsync(IMongoDatabase db)
        {
            var historyCollection = db.GetCollection<TripLocationHistory>("trip_location_history");

            // Index for quickly filtering by tripId
            var tripIdIndex = Builders<TripLocationHistory>.IndexKeys
                .Ascending(x => x.TripId);

            await historyCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<TripLocationHistory>(tripIdIndex, new CreateIndexOptions
                {
                    Name = "idx_tripLocationHistory_tripId"
                }));

            // Compound index for ordering by recordedAt within each trip (supports latest-location queries)
            var tripRecordedAtIndex = Builders<TripLocationHistory>.IndexKeys
                .Ascending(x => x.TripId)
                .Descending(x => x.Location.RecordedAt);

            await historyCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<TripLocationHistory>(tripRecordedAtIndex, new CreateIndexOptions
                {
                    Name = "idx_tripLocationHistory_tripId_recordedAt"
                }));
        }

    }
}
