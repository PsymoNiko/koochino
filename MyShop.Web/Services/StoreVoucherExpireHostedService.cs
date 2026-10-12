using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Finance;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class StoreVoucherExpireHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _config;
        private readonly ILogger<StoreVoucherExpireHostedService> _logger;
        private DateTime? _lastRunLocalDate;

        public StoreVoucherExpireHostedService(IServiceProvider sp, IConfiguration config, ILogger<StoreVoucherExpireHostedService> logger)
        {
            _sp = sp;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await TryProcessIfDueAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Store voucher expire job failed"); }

                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }

        private async Task TryProcessIfDueAsync(CancellationToken ct)
        {
            var enabled = _config.GetValue("StoreVoucherExpire:Enabled", true);
            if (!enabled) return;
            var hour = _config.GetValue("StoreVoucherExpire:HourLocal", 8);
            var minute = _config.GetValue("StoreVoucherExpire:MinuteLocal", 15);
            var now = DateTime.Now;
            var today = now.Date;
            if (_lastRunLocalDate == today) return;
            if (now.Hour < hour || (now.Hour == hour && now.Minute < minute)) return;

            using var scope = _sp.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IStoreVoucherWalletService>();
            var (n, amt) = await svc.ExpireBreakageAsync();
            ct.ThrowIfCancellationRequested();
            _lastRunLocalDate = today;
            _logger.LogInformation("Store voucher expire: {Count} lots, {Amount}", n, amt);
        }
    }
}
