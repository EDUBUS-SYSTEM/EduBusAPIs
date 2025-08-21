using Data.Repos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.Backgrounds
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

                    await repo.CleanupExpiredTokensAsync();
                    _logger.LogInformation("RefreshToken cleanup executed at: {time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cleaning refresh tokens");
                }

                // Run every hour/minutes
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                //await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
