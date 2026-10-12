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
    /// Catches up article embeddings after an app-pool recycle on shared hosting. Idempotent: unchanged
    /// hashes are skipped, and a missing table or disabled switch is a no-op.
    /// </summary>
    public class AiEmbeddingHostedService : BackgroundService
    {
        static readonly TimeSpan Poll = TimeSpan.FromMinutes(2);

        private readonly IServiceProvider _sp;
        private readonly ILogger<AiEmbeddingHostedService> _logger;

        public AiEmbeddingHostedService(IServiceProvider sp, ILogger<AiEmbeddingHostedService> logger)
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
                    var indexer = scope.ServiceProvider.GetService<IAiEmbeddingIndexer>();
                    if (indexer != null)
                        await indexer.IndexStaleAsync(null, 8);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ai embedding catch-up skipped");
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
    }
}
