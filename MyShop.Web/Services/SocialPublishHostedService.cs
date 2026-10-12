using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.SocialMedia;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>پردازش jobهای زمان‌بندی‌شده SocialMedia — مستقل از DailyChannelTip.</summary>
    public class SocialPublishHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<SocialPublishHostedService> _logger;

        public SocialPublishHostedService(IServiceProvider sp, ILogger<SocialPublishHostedService> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var scheduler = scope.ServiceProvider.GetRequiredService<ISocialScheduleService>();
                    var n = await scheduler.ProcessDueJobsAsync(stoppingToken);
                    if (n > 0)
                        _logger.LogInformation("SocialPublishHostedService processed {Count} job(s)", n);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SocialPublishHostedService failed");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
