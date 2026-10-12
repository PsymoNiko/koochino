using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Messaging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>هر روز در ساعت تنظیم‌شده، یک نکته محلی به همه کانال‌های فعال می‌فرستد.</summary>
    public class DailyChannelTipHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _config;
        private readonly ILogger<DailyChannelTipHostedService> _logger;
        private DateTime? _lastSentLocalDate;

        public DailyChannelTipHostedService(
            IServiceProvider sp,
            IConfiguration config,
            ILogger<DailyChannelTipHostedService> logger)
        {
            _sp = sp;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TrySendIfDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DailyChannelTip failed");
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

        async Task TrySendIfDueAsync(CancellationToken ct)
        {
            var enabled = _config.GetValue("ChannelDailyTip:Enabled", false);
            if (!enabled) return;

            var hour = _config.GetValue("ChannelDailyTip:HourLocal", 10);
            var minute = _config.GetValue("ChannelDailyTip:MinuteLocal", 0);
            var now = DateTime.Now;
            var today = now.Date;

            if (_lastSentLocalDate == today) return;
            if (now.Hour < hour || (now.Hour == hour && now.Minute < minute)) return;

            var text = ChannelDailyTipComposer.ComposeForDate(today);
            using var scope = _sp.CreateScope();
            var broadcast = scope.ServiceProvider.GetRequiredService<IChannelBroadcastService>();
            var results = await broadcast.SendToAllActiveAsync(text, siteId: null);
            ct.ThrowIfCancellationRequested();

            _lastSentLocalDate = today;
            var ok = results?.Count(r => r.Ok) ?? 0;
            var fail = results?.Count(r => !r.Ok) ?? 0;
            _logger.LogInformation("DailyChannelTip sent for {Date}: {Ok} ok, {Fail} fail", today.ToString("yyyy-MM-dd"), ok, fail);
        }
    }
}
