using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Builds and refreshes article embeddings. Idempotent: unchanged ContentHash skips the provider call.
    /// Failures never throw — retrieval then falls back to keyword search.
    /// </summary>
    public class AiEmbeddingIndexer : IAiEmbeddingIndexer
    {
        private readonly IKnowledgeRepository _kb;
        private readonly IAiKnowledgeChunkRepository _chunks;
        private readonly IAiEmbeddingClient _embeddings;
        private readonly IAiRuntimeOptionsProvider _options;
        private readonly IAiUsageGuard _usage;
        private readonly IMemoryCache _cache;

        public AiEmbeddingIndexer(
            IKnowledgeRepository kb,
            IAiKnowledgeChunkRepository chunks = null,
            IAiEmbeddingClient embeddings = null,
            IAiRuntimeOptionsProvider options = null,
            IAiUsageGuard usage = null,
            IMemoryCache cache = null)
        {
            _kb = kb;
            _chunks = chunks;
            _embeddings = embeddings;
            _options = options;
            _usage = usage;
            _cache = cache;
        }

        public async Task<AiIndexResult> IndexArticleAsync(int articleId)
        {
            if (_kb == null || _chunks == null) return new AiIndexResult(0, 0, 1, 0);

            KnowledgeArticle article;
            try { article = await _kb.GetArticleByIdAsync(articleId); }
            catch { return new AiIndexResult(0, 0, 0, 0, "جدول پایگاه دانش در دسترس نیست."); }

            if (article == null) return new AiIndexResult(0, 0, 1, 0);

            if (!string.Equals(article.Status, KnowledgeArticleStatuses.Published, StringComparison.OrdinalIgnoreCase)
                || !article.IsPublic)
            {
                try { await _chunks.DeleteByArticleAsync(articleId); }
                catch { }
                InvalidateCache(article.SiteId);
                return new AiIndexResult(1, 0, 1, 0);
            }

            var options = _options == null ? AiRuntimeOptions.Defaults : await _options.GetAsync(article.SiteId);
            if (options == null || !options.EmbeddingsEnabled)
                return new AiIndexResult(0, 0, 1, 0);

            if (_embeddings == null || !await _embeddings.IsConfiguredAsync())
                return new AiIndexResult(0, 0, 1, 0, "کلید یا سرویس بردار تنظیم نشده است.");

            var model = await _embeddings.ResolveEmbeddingModelAsync(article.SiteId);
            var sourceHash = EmbeddingMath.Sha256Hex(
                (article.Title ?? "") + "\n" + (article.Summary ?? "") + "\n" + (article.Content ?? "") + "\n" + model);

            List<AiKnowledgeChunk> existing;
            try { existing = await _chunks.GetByArticleAsync(articleId); }
            catch { return new AiIndexResult(0, 0, 0, 0, "جدول تکه‌های بردار هنوز ساخته نشده است."); }

            if (existing.Count > 0 && existing.All(c => c.SourceHash == sourceHash && c.Embedding != null && c.Embedding.Length > 0))
                return new AiIndexResult(1, 0, 1, 0);

            var texts = KnowledgeChunker.Chunk(article.Title, article.Summary, article.Content);
            if (texts.Count == 0) return new AiIndexResult(1, 0, 1, 0);

            IReadOnlyList<AiEmbeddingResult> vectors;
            try { vectors = await _embeddings.EmbedBatchAsync(texts, article.SiteId); }
            catch (Exception ex) { return new AiIndexResult(0, 0, 0, 0, ex.Message); }

            if (vectors == null || vectors.Count == 0 || vectors.Any(v => v == null || !v.Ok || v.Vector == null || v.Vector.Length == 0))
            {
                var err = vectors?.FirstOrDefault(v => v != null && !v.Ok)?.Error ?? "ساخت بردار ناموفق بود.";
                return new AiIndexResult(0, 0, 0, 0, err);
            }

            var rows = new List<AiKnowledgeChunk>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
            {
                var v = vectors[Math.Min(i, vectors.Count - 1)];
                rows.Add(new AiKnowledgeChunk
                {
                    ArticleId = article.Id,
                    SiteId = article.SiteId,
                    ChunkIndex = i,
                    Text = texts[i],
                    ContentHash = EmbeddingMath.Sha256Hex(texts[i] + "\n" + model),
                    SourceHash = sourceHash,
                    Embedding = EmbeddingMath.ToBytes(v.Vector),
                    Dimensions = v.Dimensions,
                    Model = v.Model ?? model
                });
            }

            try { await _chunks.ReplaceArticleChunksAsync(article.Id, rows); }
            catch (Exception ex) { return new AiIndexResult(0, 0, 0, 0, ex.Message); }

            var tokens = vectors.Sum(v => v.Tokens);
            if (tokens > 0 && _usage != null)
            {
                try { await _usage.RecordAsync(new AiChatContext(SiteId: article.SiteId), tokens, 0); }
                catch { }
            }

            InvalidateCache(article.SiteId);
            return new AiIndexResult(1, rows.Count, 0, tokens);
        }

        public async Task<AiIndexResult> IndexStaleAsync(int? siteId, int take = 8)
        {
            if (_chunks == null) return new AiIndexResult(0, 0, 0, 0);
            List<int> ids;
            try { ids = await _chunks.ListStaleArticleIdsAsync(siteId, take); }
            catch { return new AiIndexResult(0, 0, 0, 0); }

            var processed = 0;
            var written = 0;
            var skipped = 0;
            var tokens = 0;
            string error = null;
            foreach (var id in ids)
            {
                var one = await IndexArticleAsync(id);
                processed += one.ArticlesProcessed;
                written += one.ChunksWritten;
                skipped += one.Skipped;
                tokens += one.Tokens;
                if (!one.Ok && error == null) error = one.Error;
            }
            return new AiIndexResult(processed, written, skipped, tokens, error);
        }

        public async Task<AiIndexResult> RebuildSiteAsync(int? siteId)
        {
            if (_kb == null) return new AiIndexResult(0, 0, 0, 0);
            List<KnowledgeArticle> articles;
            try
            {
                articles = await _kb.ListArticlesAsync(siteId, KnowledgeArticleStatuses.Published,
                    publicOnly: true, take: 500);
            }
            catch (Exception ex) { return new AiIndexResult(0, 0, 0, 0, ex.Message); }

            var processed = 0;
            var written = 0;
            var skipped = 0;
            var tokens = 0;
            string error = null;
            foreach (var article in articles ?? new List<KnowledgeArticle>())
            {
                var one = await IndexArticleAsync(article.Id);
                processed += one.ArticlesProcessed;
                written += one.ChunksWritten;
                skipped += one.Skipped;
                tokens += one.Tokens;
                if (!one.Ok && error == null) error = one.Error;
            }
            return new AiIndexResult(processed, written, skipped, tokens, error);
        }

        void InvalidateCache(int? siteId)
        {
            if (_cache == null) return;
            _cache.Remove(ChunkCacheKey(siteId));
        }

        public static string ChunkCacheKey(int? siteId) => $"ai:chunks:{siteId ?? 0}";
        public static string QueryCacheKey(int? siteId, string model, string normalized)
            => $"ai:qemb:{siteId ?? 0}:{model}:{normalized}";
    }
}
