using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using System;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Resolves per-site AI behaviour from support settings. Cached briefly because it is read on every
    /// message and the values change only from the admin panel.
    /// </summary>
    public class AiRuntimeOptionsProvider : IAiRuntimeOptionsProvider
    {
        static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(2);

        private readonly ISupportChatRepository _chats;
        private readonly IMemoryCache _cache;

        public AiRuntimeOptionsProvider(ISupportChatRepository chats, IMemoryCache cache = null)
        {
            _chats = chats;
            _cache = cache;
        }

        public async Task<AiRuntimeOptions> GetAsync(int? siteId)
        {
            if (siteId == null || siteId <= 0) return AiRuntimeOptions.Defaults;

            var key = $"ai:options:{siteId}";
            if (_cache != null && _cache.TryGetValue(key, out AiRuntimeOptions cached) && cached != null)
                return cached;

            AiRuntimeOptions options;
            try
            {
                var s = await _chats.GetSettingsAsync(siteId.Value);
                options = s == null
                    ? AiRuntimeOptions.Defaults
                    : new AiRuntimeOptions(
                        Enabled: s.AiEnabled,
                        MinRetrievalScore: s.AiMinRetrievalScore > 0 ? s.AiMinRetrievalScore : AiRuntimeOptions.Defaults.MinRetrievalScore,
                        MaxMessagesPerIdentityPerDay: s.AiMaxMessagesPerUserPerDay > 0 ? s.AiMaxMessagesPerUserPerDay : AiRuntimeOptions.Defaults.MaxMessagesPerIdentityPerDay,
                        MaxTokensPerSitePerDay: s.AiMaxTokensPerSitePerDay > 0 ? s.AiMaxTokensPerSitePerDay : AiRuntimeOptions.Defaults.MaxTokensPerSitePerDay,
                        MaxInputChars: s.AiMaxInputChars > 0 ? s.AiMaxInputChars : AiRuntimeOptions.Defaults.MaxInputChars,
                        HistoryTurns: s.AiHistoryTurns >= 0 ? s.AiHistoryTurns : AiRuntimeOptions.Defaults.HistoryTurns,
                        Temperature: (double)s.AiTemperature,
                        OutOfScopeMessage: string.IsNullOrWhiteSpace(s.AiOutOfScopeMessage)
                            ? AiRuntimeOptions.Defaults.OutOfScopeMessage
                            : s.AiOutOfScopeMessage,
                        EmbeddingsEnabled: s.AiEmbeddingsEnabled,
                        LearnFromAdminChat: s.AiLearnFromAdminChat);
            }
            catch
            {
                // Settings columns missing (migration not run yet) must not take the assistant offline.
                options = AiRuntimeOptions.Defaults;
            }

            _cache?.Set(key, options, CacheLifetime);
            return options;
        }
    }
}
