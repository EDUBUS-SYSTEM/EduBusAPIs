using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Contracts;
using Data.Repos.Interfaces;
using Data.Models;
using MongoDB.Driver;

namespace Services.Backgrounds
{
    public class AutoTripGenerationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoTripGenerationService> _logger;
        private readonly TimeSpan _generationInterval = TimeSpan.FromHours(6); // Run every 6 hours
        private readonly int _daysAhead = 7; // Generate trips for next 7 days

        public AutoTripGenerationService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoTripGenerationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoTripGenerationService started at: {time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await ProcessTripGenerationAsync(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoTripGenerationService");
                }

                await Task.Delay(_generationInterval, stoppingToken);
            }

            _logger.LogInformation("AutoTripGenerationService stopped at: {time}", DateTime.UtcNow);
        }

        private async Task ProcessTripGenerationAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var tripService = serviceProvider.GetRequiredService<ITripService>();
                var scheduleRepo = serviceProvider.GetRequiredService<IScheduleRepository>();
                var routeScheduleRepo = serviceProvider.GetRequiredService<IRouteScheduleRepository>();

                var startDate = DateTime.UtcNow.Date;
                var endDate = startDate.AddDays(_daysAhead);

                _logger.LogInformation("Starting automatic trip generation from {StartDate} to {EndDate}", startDate, endDate);

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

                if (!activeSchedules.Any())
                {
                    _logger.LogInformation("No active schedules found for trip generation");
                    return;
                }

                var totalGenerated = 0;
                var processedSchedules = 0;

                foreach (var schedule in activeSchedules)
                {
                    try
                    {
                        // Check if schedule has active route schedules
                        var routeSchedules = await routeScheduleRepo.GetRouteSchedulesByScheduleAsync(schedule.Id);
                        var activeRouteSchedules = routeSchedules.Where(rs => rs.IsActive && !rs.IsDeleted).ToList();

                        if (!activeRouteSchedules.Any())
                        {
                            _logger.LogDebug("Schedule {ScheduleId} has no active route schedules", schedule.Id);
                            continue;
                        }

                        // Generate trips for this schedule
                        var generatedTrips = await tripService.GenerateTripsFromScheduleAsync(
                            schedule.Id, 
                            startDate, 
                            endDate
                        );

                        var tripCount = generatedTrips.Count();
                        totalGenerated += tripCount;
                        processedSchedules++;

                        if (tripCount > 0)
                        {
                            _logger.LogInformation("Generated {TripCount} trips for schedule {ScheduleId} ({ScheduleName})", 
                                tripCount, schedule.Id, schedule.Name);
                        }

                        // Add small delay to prevent overwhelming the database
                        await Task.Delay(1000, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating trips for schedule {ScheduleId}", schedule.Id);
                    }
                }

                _logger.LogInformation("AutoTripGenerationService completed: processed {ProcessedSchedules} schedules, generated {TotalGenerated} trips", 
                    processedSchedules, totalGenerated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessTripGenerationAsync");
            }
        }
    }
}

