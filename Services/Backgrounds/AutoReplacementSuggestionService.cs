using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Contracts;

namespace Services.Backgrounds
{
    public class AutoReplacementSuggestionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoReplacementSuggestionService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check every 30 minutes to reduce load

        public AutoReplacementSuggestionService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoReplacementSuggestionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoReplacementSuggestionService started at: {time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await ProcessPendingLeaveRequestsAsync(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoReplacementSuggestionService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("AutoReplacementSuggestionService stopped at: {time}", DateTime.UtcNow);
        }

        private async Task ProcessPendingLeaveRequestsAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var driverLeaveService = serviceProvider.GetRequiredService<IDriverLeaveService>();
                var notificationService = serviceProvider.GetRequiredService<INotificationService>();
                var leaveRepo = serviceProvider.GetRequiredService<IDriverLeaveRepository>();

                // Get all pending leave requests that need replacement suggestions
                // Limit to 5 requests per batch to prevent connection overload
                var pendingLeaves = await leaveRepo.FindByConditionAsync(
                    l => l.Status == LeaveStatus.Pending && 
                         l.AutoReplacementEnabled && 
                         l.SuggestedReplacementDriverId == null &&
                         l.StartDate > DateTime.UtcNow.AddHours(-24) && // Only process leaves starting within 24 hours
                         l.StartDate <= DateTime.UtcNow.AddDays(7) // And not too far in the future
                );
                
                // Limit batch size to prevent connection pool exhaustion
                var limitedLeaves = pendingLeaves.Take(3).ToList();

                var processedCount = 0;
                var suggestionCount = 0;

                foreach (var leave in limitedLeaves)
                {
                    try
                    {
                        // Check if suggestions are needed (not already generated recently)
                        if (leave.SuggestionGeneratedAt.HasValue && 
                            leave.SuggestionGeneratedAt.Value > DateTime.UtcNow.AddHours(-2))
                        {
                            continue; // Skip if suggestions were generated within last 2 hours
                        }

                        // Generate replacement suggestions
                        var suggestionResponse = await driverLeaveService.GenerateReplacementSuggestionsAsync(leave.Id);
                        
                        if (suggestionResponse.Success && suggestionResponse.Suggestions.Any())
                        {
                            // Update the leave request with suggestion info
                            leave.SuggestionGeneratedAt = DateTime.UtcNow;
                            
                            // Store the best suggestion (highest score) for quick access
                            var bestSuggestion = suggestionResponse.Suggestions
                                .OrderByDescending(s => s.Score)
                                .FirstOrDefault();
                                
                            if (bestSuggestion != null)
                            {
                                leave.SuggestedReplacementDriverId = bestSuggestion.DriverId;
                                leave.SuggestedReplacementVehicleId = bestSuggestion.VehicleId;
                            }
                            
                            await leaveRepo.UpdateAsync(leave);

                            // Create notification for admin
                            var metadata = new Dictionary<string, object>
                            {
                                ["leaveRequestId"] = leave.Id,
                                ["driverId"] = leave.DriverId,
                                ["startDate"] = leave.StartDate,
                                ["endDate"] = leave.EndDate,
                                ["suggestionCount"] = suggestionResponse.Suggestions.Count,
                                ["bestScore"] = suggestionResponse.Suggestions.FirstOrDefault()?.Score ?? 0
                            };

                            await notificationService.CreateReplacementSuggestionNotificationAsync(
                                leave.Id, 
                                suggestionResponse.Suggestions.Count, 
                                metadata
                            );

                            suggestionCount += suggestionResponse.Suggestions.Count;
                            _logger.LogInformation("Generated {Count} replacement suggestions for leave request {LeaveId}", 
                                suggestionResponse.Suggestions.Count, leave.Id);
                        }
                        else
                        {
                            // No suggestions available - create alert notification
                            var metadata = new Dictionary<string, object>
                            {
                                ["leaveRequestId"] = leave.Id,
                                ["driverId"] = leave.DriverId,
                                ["startDate"] = leave.StartDate,
                                ["endDate"] = leave.EndDate,
                                ["reason"] = "No available drivers/vehicles found"
                            };

                            await notificationService.CreateSystemNotificationAsync(new Services.Models.Notification.CreateSystemNotificationDto
                            {
                                Title = "No Replacement Available",
                                Message = $"Unable to generate replacement suggestions for leave request starting {leave.StartDate:yyyy-MM-dd}. Manual intervention required.",
                                NotificationType = NotificationType.SystemAlert,
                                Priority = 3,
                                RelatedEntityId = leave.Id,
                                RelatedEntityType = "DriverLeaveRequest",
                                ActionRequired = true,
                                ActionUrl = $"/admin/leave-requests/{leave.Id}",
                                ExpiresAt = DateTime.UtcNow.AddDays(1),
                                Metadata = metadata
                            });

                            _logger.LogWarning("No replacement suggestions available for leave request {LeaveId}", leave.Id);
                        }

                        processedCount++;
                        
                        // Add delay between operations to prevent connection overload
                        await Task.Delay(3000, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing leave request {LeaveId}", leave.Id);
                    }
                }

                if (processedCount > 0)
                {
                    _logger.LogInformation("AutoReplacementSuggestionService processed {ProcessedCount} leave requests, generated {SuggestionCount} suggestions", 
                        processedCount, suggestionCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessPendingLeaveRequestsAsync");
            }
        }
    }
}
