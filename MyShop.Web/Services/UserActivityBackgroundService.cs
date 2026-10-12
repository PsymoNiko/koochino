using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class UserActivityBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserActivityBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5); // هر 5 دقیقه
        private readonly int _inactiveMinutes = 30; // session های 30 دقیقه غیرفعال

        public UserActivityBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<UserActivityBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IUserActivityRepository>();
                        var cleanedCount = await repository.CleanupInactiveSessionsAsync(_inactiveMinutes);
                        
                        if (cleanedCount > 0)
                        {
                            _logger.LogInformation($"Cleaned up {cleanedCount} inactive sessions");
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up inactive sessions");
                }

                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
