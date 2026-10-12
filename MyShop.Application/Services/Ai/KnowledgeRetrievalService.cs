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
    /// Hybrid retrieval: keyword search (phase 1) plus optional cosine over cached article chunks.
    /// Embedding failures fall back to keyword-only so the assistant never goes offline.
    /// </summary>
    public class KnowledgeRetrievalService : IKnowledgeRetrieval
    {
        const int CandidateLimit = 24;
        const int ContextCharsPerArticle = 900;

        private readonly IKnowledgeService _kb;
        private readonly IAiEmbeddingClient _embeddings;
        private readonly IAiKnowledgeChunkRepository _chunks;
        private readonly IAiRuntimeOptionsProvider _options;
        private readonly IAiUsageGuard _usage;
        private readonly IMemoryCache _cache;

        public KnowledgeRetrievalService(
            IKnowledgeService kb,
            IAiEmbeddingClient embeddings = null,
            IAiKnowledgeChunkRepository chunks = null,
            IAiRuntimeOptionsProvider options = null,
            IAiUsageGuard usage = null,
            IMemoryCache cache = null)
        {
            _kb = kb;
            _embeddings = embeddings;
            _chunks = chunks;
            _options = options;
            _usage = usage;
            _cache = cache;
        }

        public Task<RetrievalResult> RetrieveAsync(string query, int? siteId, AiAudience audience, int topK = 4)
            => RetrieveCoreAsync(query, siteId, audience, topK, useEmbeddings: true);

        public Task<RetrievalResult> RetrieveKeywordOnlyAsync(string query, int? siteId, AiAudience audience, int topK = 4)
            => RetrieveCoreAsync(query, siteId, audience, topK, useEmbeddings: false);

        async Task<RetrievalResult> RetrieveCoreAsync(string query, int? siteId, AiAudience audience, int topK, bool useEmbeddings)
        {
            if (string.IsNullOrWhiteSpace(query)) return RetrievalResult.Empty;

            var tokens = PersianTextScorer.Tokenize(query);
            var keywordArticles = await LoadKeywordCandidatesAsync(siteId, tokens, audience);
            var keywordScores = new Dictionary<int, int>();
            foreach (var article in keywordArticles)
            {
                var score = PersianTextScorer.ScoreArticle(query, tokens, article);
                if (score > 0) keywordScores[article.Id] = score;
            }

            var articlesById = keywordArticles.ToDictionary(a => a.Id);
            var cosineByArticle = new Dictionary<int, float>();

            if (useEmbeddings)
                await TryAddSemanticCandidatesAsync(query, siteId, audience, articlesById, keywordScores, cosineByArticle);

            var take = topK <= 0 ? 4 : topK;
            var scored = new List<RetrievedArticle>();
            foreach (var id in articlesById.Keys)
            {
                var keyword = keywordScores.TryGetValue(id, out var k) ? k : 0;
                var cosine = cosineByArticle.TryGetValue(id, out var c) ? c : 0f;
                if (!EmbeddingMath.PassesSemanticFloor(keyword, cosine)) continue;
                var hybrid = EmbeddingMath.HybridScore(keyword, cosine);
                if (hybrid <= 0 && keyword <= 0) continue;
                var article = articlesById[id];
                scored.Add(new RetrievedArticle(
                    article.Id,
                    article.Title,
                    article.Slug,
                    PersianTextScorer.Excerpt(article.Summary, article.Content, ContextCharsPerArticle),
                    hybrid > 0 ? hybrid : keyword));
            }

            scored = scored.OrderByDescending(x => x.Score).Take(take).ToList();
            if (scored.Count == 0) return RetrievalResult.Empty;
            return new RetrievalResult(scored, scored.Max(x => x.Score));
        }

        async Task<List<KnowledgeArticle>> LoadKeywordCandidatesAsync(int? siteId, List<string> tokens, AiAudience audience)
        {
            try
            {
                var candidates = await _kb.SearchForRagAsync(siteId, tokens, MapAudience(audience), CandidateLimit);
                return candidates ?? new List<KnowledgeArticle>();
            }
            catch
            {
                return new List<KnowledgeArticle>();
            }
        }

        async Task TryAddSemanticCandidatesAsync(
            string query,
            int? siteId,
            AiAudience audience,
            Dictionary<int, KnowledgeArticle> articlesById,
            Dictionary<int, int> keywordScores,
            Dictionary<int, float> cosineByArticle)
        {
            try
            {
                var options = _options == null ? null : await _options.GetAsync(siteId);
                if (options != null && !options.EmbeddingsEnabled) return;
                if (_embeddings == null || _chunks == null) return;
                if (!await _embeddings.IsConfiguredAsync()) return;

                var model = await _embeddings.ResolveEmbeddingModelAsync(siteId);
                var queryVector = await EmbedQueryAsync(query, siteId, model);
                if (queryVector == null || queryVector.Length == 0) return;

                var cached = await LoadChunksAsync(siteId, audience, model);
                if (cached.Count == 0) return;

                var best = new Dictionary<int, (float Cosine, CachedChunk Chunk)>();
                foreach (var chunk in cached)
                {
                    var cosine = EmbeddingMath.Cosine(queryVector, chunk.Vector);
                    if (!best.TryGetValue(chunk.ArticleId, out var current) || cosine > current.Cosine)
                        best[chunk.ArticleId] = (cosine, chunk);
                }

                foreach (var pair in best.OrderByDescending(x => x.Value.Cosine).Take(AiEmbeddingDefaults.SemanticTopK))
                {
                    cosineByArticle[pair.Key] = pair.Value.Cosine;
                    if (articlesById.ContainsKey(pair.Key)) continue;
                    if (!EmbeddingMath.PassesSemanticFloor(0, pair.Value.Cosine)) continue;
                    var article = await _kb.GetAsync(pair.Key);
                    if (article != null) articlesById[pair.Key] = article;
                    else
                    {
                        var c = pair.Value.Chunk;
                        articlesById[pair.Key] = new KnowledgeArticle
                        {
                            Id = c.ArticleId,
                            Title = c.Title,
                            Slug = c.Slug,
                            Summary = c.Text,
                            Content = c.Text,
                            Audience = c.Audience,
                            ArticleType = c.ArticleType,
                            Status = KnowledgeArticleStatuses.Published,
                            IsPublic = true
                        };
                    }
                    if (!keywordScores.ContainsKey(pair.Key))
                        keywordScores[pair.Key] = 0;
                }
            }
            catch
            {
                // Provider / table missing: keyword path already populated.
            }
        }

        async Task<float[]> EmbedQueryAsync(string query, int? siteId, string model)
        {
            var normalized = PersianTextScorer.CacheKey(query);
            var cacheKey = AiEmbeddingIndexer.QueryCacheKey(siteId, model, normalized);
            if (_cache != null && _cache.TryGetValue(cacheKey, out float[] cached) && cached != null && cached.Length > 0)
                return cached;

            var result = await _embeddings.EmbedAsync(query, siteId);
            if (result == null || !result.Ok || result.Vector == null || result.Vector.Length == 0)
                return null;

            if (result.Tokens > 0 && _usage != null)
            {
                try { await _usage.RecordAsync(new AiChatContext(SiteId: siteId), result.Tokens, 0); }
                catch { }
            }

            _cache?.Set(cacheKey, result.Vector, TimeSpan.FromMinutes(30));
            return result.Vector;
        }

        async Task<List<CachedChunk>> LoadChunksAsync(int? siteId, AiAudience audience, string model)
        {
            var cacheKey = AiEmbeddingIndexer.ChunkCacheKey(siteId) + ":" + (model ?? "") + ":" + MapAudience(audience);
            if (_cache != null && _cache.TryGetValue(cacheKey, out List<CachedChunk> cached) && cached != null)
                return cached;

            var rows = await _chunks.ListPublishedAsync(siteId, MapAudience(audience), model);
            var list = new List<CachedChunk>(rows?.Count ?? 0);
            foreach (var row in rows ?? new List<AiKnowledgeChunk>())
            {
                var vector = EmbeddingMath.FromBytes(row.Embedding);
                if (vector.Length == 0) continue;
                list.Add(new CachedChunk(row.ArticleId, row.Title, row.Slug, row.Text, row.Audience, row.ArticleType, vector));
            }

            _cache?.Set(cacheKey, list, TimeSpan.FromMinutes(10));
            return list;
        }

        static string MapAudience(AiAudience audience) => audience switch
        {
            AiAudience.Customer => KnowledgeAudiences.Customer,
            AiAudience.StoreOwner => KnowledgeAudiences.StoreOwner,
            _ => null
        };

        sealed record CachedChunk(int ArticleId, string Title, string Slug, string Text, string Audience, string ArticleType, float[] Vector);
    }
}
