using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(IServiceProvider provider, ILogger<RefreshTokenCleanupService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run once per day
            var delay = TimeSpan.FromHours(24);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var svc = scope.ServiceProvider.GetService<Application.Interfaces.IRefreshTokenService>();
                    if (svc != null)
                    {
                        var removed = await svc.RemoveExpiredAsync();
                        if (removed > 0) _logger.LogInformation("Removed {Count} expired refresh tokens", removed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running refresh token cleanup");
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
