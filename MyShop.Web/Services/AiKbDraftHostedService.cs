using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces.Ai;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// Periodically mines closed admin chats into knowledge-base drafts. Idempotent via SourceKey and a
    /// persisted watermark, so an app-pool recycle on shared hosting resumes instead of duplicating.
    /// Without an HTTP tenant the connection factory throws; that is a no-op here. Admins run the same
    /// learner from the knowledge page, which has a resolved tenant.
    /// </summary>
    public class AiKbDraftHostedService : BackgroundService
    {
        static readonly TimeSpan Poll = TimeSpan.FromMinutes(8);

        private readonly IServiceProvider _sp;
        private readonly ILogger<AiKbDraftHostedService> _logger;

        public AiKbDraftHostedService(IServiceProvider sp, ILogger<AiKbDraftHostedService> logger)
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
                    var learner = scope.ServiceProvider.GetService<IAiKbDraftLearner>();
                    if (learner != null)
                        await learner.ProcessSiteAsync(null);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (InvalidOperationException ex) when (IsTenantUnresolved(ex))
                {
                    _logger.LogDebug(ex, "Ai KB learn skipped: no HTTP tenant on background thread");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ai KB learn catch-up skipped");
                }

                try
                {
                    await Task.Delay(Poll, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        static bool IsTenantUnresolved(Exception ex)
        {
            var m = ex.Message ?? "";
            return m.IndexOf("Tenant is not resolved", StringComparison.OrdinalIgnoreCase) >= 0
                || m.IndexOf("connection string", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
