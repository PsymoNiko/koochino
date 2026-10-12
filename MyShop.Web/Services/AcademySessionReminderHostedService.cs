using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Academy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>هر روز در ساعت تنظیم‌شده، یادآوری SMS جلسات امروز آموزشگاه را می‌فرستد.</summary>
    public class AcademySessionReminderHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _config;
        private readonly ILogger<AcademySessionReminderHostedService> _logger;
        private DateTime? _lastSentLocalDate;

        public AcademySessionReminderHostedService(
            IServiceProvider sp,
            IConfiguration config,
            ILogger<AcademySessionReminderHostedService> logger)
        {
            _sp = sp;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await TrySendIfDueAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Academy session reminder failed"); }

                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }

        private async Task TrySendIfDueAsync(CancellationToken ct)
        {
            var enabled = _config.GetValue("AcademyReminders:Enabled", true);
            if (!enabled) return;

            var hour = _config.GetValue("AcademyReminders:HourLocal", 8);
            var minute = _config.GetValue("AcademyReminders:MinuteLocal", 0);
            var now = DateTime.Now;
            var today = now.Date;
            if (_lastSentLocalDate == today) return;
            if (now.Hour < hour || (now.Hour == hour && now.Minute < minute)) return;

            using var scope = _sp.CreateScope();
            var notify = scope.ServiceProvider.GetRequiredService<IAcademyNotificationService>();
            var sent = await notify.SendSessionRemindersAsync(today);
            var tuition = await notify.SendTuitionRemindersAsync(today);
            ct.ThrowIfCancellationRequested();
            _lastSentLocalDate = today;
            _logger.LogInformation("Academy reminders sent. sessions={SessionCount} tuition={TuitionCount}", sent, tuition);
        }
    }
}
