using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Application.Services.Ai;
using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyShop.Tests;

public class AiPhase3EmbeddingTests
{
    const string ArvanChat = "https://arvancloudai.ir/gateway/models/GPT-4o-mini/fakeTokenValue123/v1";
    const string ArvanEmbed = "https://arvancloudai.ir/gateway/models/text-embedding-3-small/fakeTokenValue123/v1";

    [Fact]
    public void ArvanChatGateway_IsChatOnly_AndDoesNotServeEmbeddings()
    {
        Assert.True(AiGatewayProfile.IsChatOnlyGateway(ArvanChat));
        Assert.False(AiGatewayProfile.IsEmbeddingGateway(ArvanChat));
        Assert.Contains("embedding", AiGatewayProfile.ExplainEmbedError("HTTP 424: embedding failed", ArvanChat), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ArvanEmbeddingGateway_IsNotChatOnly()
    {
        Assert.False(AiGatewayProfile.IsChatOnlyGateway(ArvanEmbed));
        Assert.True(AiGatewayProfile.IsEmbeddingGateway(ArvanEmbed));
    }

    [Fact]
    public void ArvanGateway_RedactsPathToken()
    {
        var redacted = AiGatewayProfile.Redact(ArvanChat);
        Assert.DoesNotContain("fakeTokenValue123", redacted);
        Assert.Contains("GPT-4o-mini", redacted);
        Assert.Contains("***", redacted);
    }

    [Fact]
    public void EmbeddingAuth_PrefersStoredApiKey_ThenPathToken_ThenAnonymous()
    {
        var attempts = AiGatewayProfile.EmbeddingAuthAttempts(ArvanEmbed, "stored-chat-key");
        Assert.Equal(new string[] { "stored-chat-key", "fakeTokenValue123", null }, attempts);
    }

    [Fact]
    public void OpenAiBaseUrl_IsNotTreatedAsChatOnlyGateway()
    {
        Assert.False(AiGatewayProfile.IsChatOnlyGateway("https://api.openai.com/v1"));
    }

    [Fact]
    public void Cosine_OfIdenticalVectors_IsOne()
    {
        var v = new[] { 0.3f, -0.1f, 0.9f };
        Assert.Equal(1f, EmbeddingMath.Cosine(v, v), 4);
    }

    [Fact]
    public void Cosine_OfOrthogonalVectors_IsZero()
    {
        Assert.Equal(0f, EmbeddingMath.Cosine(new[] { 1f, 0f }, new[] { 0f, 1f }), 4);
    }

    [Fact]
    public void VectorBytes_RoundTrip()
    {
        var original = new[] { 0.5f, -2f, 0f, 1.25f };
        var restored = EmbeddingMath.FromBytes(EmbeddingMath.ToBytes(original));
        Assert.Equal(original, restored);
    }

    [Fact]
    public void HybridScore_KeepsStrongKeywordEvenWithWeakCosine()
    {
        var score = EmbeddingMath.HybridScore(80, 0.2f);
        Assert.True(score >= 24, $"hybrid was {score}");
        Assert.True(EmbeddingMath.PassesSemanticFloor(80, 0.2f));
    }

    [Fact]
    public void SemanticFloor_RejectsWeakCosineWithoutKeyword()
    {
        Assert.False(EmbeddingMath.PassesSemanticFloor(0, 0.54f));
        Assert.True(EmbeddingMath.PassesSemanticFloor(0, 0.55f));
        Assert.True(EmbeddingMath.HybridScore(0, 0.54f) < 24 || !EmbeddingMath.PassesSemanticFloor(0, 0.54f));
    }

    [Fact]
    public void Chunker_KeepsShortFaqAsOneChunk()
    {
        var chunks = KnowledgeChunker.Chunk("کد تخفیف چیست؟", "خلاصه کوتاه", "متن کوتاه FAQ.");
        Assert.Single(chunks);
    }

    [Fact]
    public void Chunker_SplitsLongArticleAroundTargetLength()
    {
        var sentence = string.Join(" ", Enumerable.Repeat("واژه", 20)) + ".";
        var content = string.Join(" ", Enumerable.Repeat(sentence, 40));
        var chunks = KnowledgeChunker.Chunk("عنوان", "", content);
        Assert.True(chunks.Count >= 2, $"got {chunks.Count} chunks");
        Assert.All(chunks, c => Assert.False(string.IsNullOrWhiteSpace(c)));
    }

    [Fact]
    public async Task Retrieval_FallsBackToKeyword_WhenEmbeddingsFail()
    {
        var kb = SeedKb();
        var retrieval = new KnowledgeRetrievalService(
            kb,
            embeddings: new FakeEmbeddings { Failure = "HTTP 404" },
            chunks: new FakeChunks(),
            options: new FakeOptions(Enabled: true),
            cache: new MemoryCache(new MemoryCacheOptions()));

        var result = await retrieval.RetrieveAsync("کد تخفیف چیست؟", 1, AiAudience.Customer);

        Assert.True(result.HasContent);
        Assert.True(result.BestScore >= 24, $"score was {result.BestScore}");
    }

    [Fact]
    public async Task Retrieval_IgnoresEmbeddings_WhenSwitchIsOff()
    {
        var kb = SeedKb();
        var embeddings = new FakeEmbeddings();
        var retrieval = new KnowledgeRetrievalService(
            kb,
            embeddings,
            new FakeChunks(),
            new FakeOptions(Enabled: false),
            cache: new MemoryCache(new MemoryCacheOptions()));

        await retrieval.RetrieveAsync("کد تخفیف چیست؟", 1, AiAudience.Customer);
        Assert.Equal(0, embeddings.CallCount);
    }

    [Fact]
    public async Task Retrieval_RanksParaphraseViaCosine_WhenKeywordsMiss()
    {
        var kb = new FakeKb();
        kb.Add(1, "تعریف جایزه برای مشتریان", "با جایزه می‌توانید مشتری را برگردانید.", KnowledgeAudiences.Customer);
        kb.Add(2, "ساعات کاری انبار", "انبار از شنبه تا چهارشنبه باز است.", KnowledgeAudiences.Customer);

        var chunks = new FakeChunks();
        chunks.Add(1, "تعریف جایزه برای مشتریان", new[] { 1f, 0f });
        chunks.Add(2, "ساعات کاری انبار", new[] { 0f, 1f });

        var retrieval = new KnowledgeRetrievalService(
            kb,
            new FakeEmbeddings { Vector = new[] { 0.99f, 0.01f } },
            chunks,
            new FakeOptions(Enabled: true),
            cache: new MemoryCache(new MemoryCacheOptions()));

        var hybrid = await retrieval.RetrieveAsync("چطور به مشتریام هدیه بدم؟", 1, AiAudience.Customer);
        var keyword = await retrieval.RetrieveKeywordOnlyAsync("چطور به مشتریام هدیه بدم؟", 1, AiAudience.Customer);

        Assert.Contains(hybrid.Items, i => i.Id == 1);
        Assert.True(hybrid.BestScore >= 24, $"hybrid score {hybrid.BestScore}");
        Assert.DoesNotContain(hybrid.Items, i => i.Id == 2 && i.Score >= 24);
        Assert.True(keyword.BestScore < hybrid.BestScore || !keyword.Items.Any(i => i.Id == 1),
            "semantic path should improve recall over keyword-only");
    }

    [Fact]
    public async Task OffTopic_StaysRejected_EvenWithEmbeddingsOn()
    {
        var kb = SeedKb();
        var chunks = new FakeChunks();
        foreach (var article in kb.Articles.Values)
            chunks.Add(article.Id, article.Title, new[] { 0f, 1f });

        var retrieval = new KnowledgeRetrievalService(
            kb,
            new FakeEmbeddings { Vector = new[] { 1f, 0f } },
            chunks,
            new FakeOptions(Enabled: true),
            cache: new MemoryCache(new MemoryCacheOptions()));

        var result = await retrieval.RetrieveAsync("پایتخت فرانسه کجاست؟", 1, AiAudience.Customer);
        Assert.True(result.BestScore < 24, $"off-topic scored {result.BestScore}");
    }

    [Fact]
    public async Task Indexer_SkipsProvider_WhenSourceHashMatches()
    {
        var article = new KnowledgeArticle
        {
            Id = 7,
            SiteId = 1,
            Title = "عنوان",
            Summary = "خلاصه",
            Content = "متن",
            Status = KnowledgeArticleStatuses.Published,
            IsPublic = true
        };
        var kb = new FakeKbRepo();
        kb.Articles[7] = article;
        var model = AiEmbeddingDefaults.Model;
        var sourceHash = EmbeddingMath.Sha256Hex(article.Title + "\n" + article.Summary + "\n" + article.Content + "\n" + model);
        var chunks = new FakeChunks();
        chunks.Rows.Add(new AiKnowledgeChunk
        {
            ArticleId = 7,
            ChunkIndex = 0,
            SourceHash = sourceHash,
            Embedding = EmbeddingMath.ToBytes(new[] { 1f, 0f }),
            Model = model
        });
        var embeddings = new FakeEmbeddings();
        var indexer = new AiEmbeddingIndexer(kb, chunks, embeddings, new FakeOptions(Enabled: true));

        var result = await indexer.IndexArticleAsync(7);

        Assert.True(result.Ok);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, embeddings.CallCount);
    }

    [Fact]
    public async Task Indexer_ReplacesStaleChunks()
    {
        var article = new KnowledgeArticle
        {
            Id = 8,
            SiteId = 1,
            Title = "عنوان تازه",
            Summary = "",
            Content = "متن تازه",
            Status = KnowledgeArticleStatuses.Published,
            IsPublic = true
        };
        var kb = new FakeKbRepo();
        kb.Articles[8] = article;
        var chunks = new FakeChunks();
        chunks.Rows.Add(new AiKnowledgeChunk
        {
            ArticleId = 8,
            ChunkIndex = 0,
            SourceHash = "old",
            Embedding = EmbeddingMath.ToBytes(new[] { 0f, 1f }),
            Model = "old-model"
        });
        var embeddings = new FakeEmbeddings { Vector = new[] { 0.2f, 0.8f } };
        var indexer = new AiEmbeddingIndexer(kb, chunks, embeddings, new FakeOptions(Enabled: true));

        var result = await indexer.IndexArticleAsync(8);

        Assert.True(result.Ok, result.Error);
        Assert.Equal(1, result.ChunksWritten);
        Assert.True(embeddings.CallCount > 0);
        Assert.Single(chunks.Rows);
        Assert.NotEqual("old", chunks.Rows[0].SourceHash);
    }

    static FakeKb SeedKb()
    {
        var kb = new FakeKb();
        kb.Add(1, "کد تخفیف چیست؟", "کد تخفیف مبلغ سفارش را کم می‌کند.", KnowledgeAudiences.Both);
        return kb;
    }

    sealed class FakeOptions : IAiRuntimeOptionsProvider
    {
        readonly AiRuntimeOptions _options;
        public FakeOptions(bool Enabled)
            => _options = AiRuntimeOptions.Defaults with { EmbeddingsEnabled = Enabled };
        public FakeOptions(AiRuntimeOptions options) => _options = options;
        public Task<AiRuntimeOptions> GetAsync(int? siteId) => Task.FromResult(_options);
    }

    sealed class FakeEmbeddings : IAiEmbeddingClient
    {
        public float[] Vector { get; set; } = new[] { 1f, 0f };
        public string? Failure { get; set; }
        public int CallCount { get; private set; }

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> ResolveEmbeddingModelAsync(int? siteId = null) => Task.FromResult(AiEmbeddingDefaults.Model);

        public Task<AiEmbeddingResult> EmbedAsync(string input, int? siteId = null)
        {
            CallCount++;
            if (Failure != null) return Task.FromResult(new AiEmbeddingResult(false, Array.Empty<float>(), Error: Failure));
            return Task.FromResult(new AiEmbeddingResult(true, Vector, Vector.Length, 3, AiEmbeddingDefaults.Model));
        }

        public async Task<IReadOnlyList<AiEmbeddingResult>> EmbedBatchAsync(IReadOnlyList<string> inputs, int? siteId = null)
        {
            var list = new List<AiEmbeddingResult>();
            foreach (var input in inputs)
                list.Add(await EmbedAsync(input, siteId));
            return list;
        }
    }

    sealed class FakeChunks : IAiKnowledgeChunkRepository
    {
        public List<AiKnowledgeChunk> Rows { get; } = new();

        public void Add(int articleId, string title, float[] vector)
        {
            Rows.Add(new AiKnowledgeChunk
            {
                ArticleId = articleId,
                Title = title,
                Slug = "s-" + articleId,
                Text = title,
                Audience = KnowledgeAudiences.Customer,
                Embedding = EmbeddingMath.ToBytes(vector),
                Model = AiEmbeddingDefaults.Model,
                Dimensions = vector.Length
            });
        }

        public Task ReplaceArticleChunksAsync(int articleId, IReadOnlyList<AiKnowledgeChunk> chunks)
        {
            Rows.RemoveAll(c => c.ArticleId == articleId);
            Rows.AddRange(chunks);
            return Task.CompletedTask;
        }

        public Task DeleteByArticleAsync(int articleId)
        {
            Rows.RemoveAll(c => c.ArticleId == articleId);
            return Task.CompletedTask;
        }

        public Task<List<AiKnowledgeChunk>> GetByArticleAsync(int articleId)
            => Task.FromResult(Rows.Where(c => c.ArticleId == articleId).ToList());

        public Task<List<AiKnowledgeChunk>> ListPublishedAsync(int? siteId, string audience, string model)
            => Task.FromResult(Rows.ToList());

        public Task<List<int>> ListStaleArticleIdsAsync(int? siteId, int take)
            => Task.FromResult(new List<int>());

        public Task<int> CountAsync(int? siteId) => Task.FromResult(Rows.Count);
    }

    sealed class FakeKb : IKnowledgeService
    {
        public Dictionary<int, KnowledgeArticle> Articles { get; } = new();

        public void Add(int id, string title, string content, string audience)
        {
            Articles[id] = new KnowledgeArticle
            {
                Id = id,
                SiteId = 1,
                Title = title,
                Slug = "s-" + id,
                Content = content,
                Summary = content,
                Status = KnowledgeArticleStatuses.Published,
                IsPublic = true,
                Audience = audience,
                ArticleType = KnowledgeArticleTypes.Faq
            };
        }

        public Task<List<KnowledgeCategory>> ListCategoriesAsync(int? siteId, bool activeOnly = false)
            => Task.FromResult(new List<KnowledgeCategory>());
        public Task<int> SaveCategoryAsync(KnowledgeCategory cat) => Task.FromResult(0);
        public Task DeleteCategoryAsync(int id) => Task.CompletedTask;
        public Task<List<KnowledgeArticle>> ListArticlesAsync(int? siteId, string? status = null, int? categoryId = null, string? type = null, string? search = null, bool publicOnly = false)
            => Task.FromResult(Articles.Values.ToList());
        public Task<List<KnowledgeArticle>> SearchForRagAsync(int? siteId, IEnumerable<string> tokens, string audience, int take = 24)
            => Task.FromResult(Articles.Values
                .Where(a => audience == null || a.Audience == KnowledgeAudiences.Both || a.Audience == audience)
                .Take(take).ToList());
        public Task<KnowledgeArticle?> GetAsync(int id)
            => Task.FromResult(Articles.TryGetValue(id, out var a) ? a : null);
        public Task<KnowledgeArticle?> GetBySlugAsync(int? siteId, string slug) => Task.FromResult<KnowledgeArticle?>(null);
        public Task<KnowledgeArticle?> GetBySourceKeyAsync(int? siteId, string sourceKey) => Task.FromResult<KnowledgeArticle?>(null);
        public Task<List<KnowledgeArticle>> ListBySourcePrefixAsync(int? siteId, string prefix, string status = null)
            => Task.FromResult(Articles.Values.Where(a => (a.SourceKey ?? "").StartsWith(prefix ?? "") && (status == null || a.Status == status)).ToList());
        public Task<int> SaveArticleAsync(KnowledgeArticle article, IEnumerable<string> tags, int? authorUserId = null) => Task.FromResult(article.Id);
        public Task PublishAsync(int id) => Task.CompletedTask;
        public Task ArchiveAsync(int id) => Task.CompletedTask;
        public Task DeleteArticleAsync(int id) => Task.CompletedTask;
        public Task<List<KnowledgeTag>> ListTagsAsync(int? siteId) => Task.FromResult(new List<KnowledgeTag>());
        public Task<List<KnowledgeArticle>> SearchPublicAsync(int? siteId, string query, int take = 20) => Task.FromResult(new List<KnowledgeArticle>());
    }

    sealed class FakeKbRepo : IKnowledgeRepository
    {
        public Dictionary<int, KnowledgeArticle> Articles { get; } = new();
        public Task<List<KnowledgeCategory>> ListCategoriesAsync(int? siteId, bool activeOnly = false) => Task.FromResult(new List<KnowledgeCategory>());
        public Task<int> UpsertCategoryAsync(KnowledgeCategory cat) => Task.FromResult(0);
        public Task DeleteCategoryAsync(int id) => Task.CompletedTask;
        public Task<List<KnowledgeArticle>> ListArticlesAsync(int? siteId, string status = null, int? categoryId = null, string type = null, string search = null, bool publicOnly = false, int take = 100)
            => Task.FromResult(Articles.Values.ToList());
        public Task<List<KnowledgeArticle>> SearchForRagAsync(int? siteId, IEnumerable<string> tokens, string audience, int take = 24)
            => Task.FromResult(Articles.Values.ToList());
        public Task<KnowledgeArticle> GetArticleByIdAsync(int id)
            => Task.FromResult(Articles.TryGetValue(id, out var a) ? a : null);
        public Task<KnowledgeArticle> GetArticleBySlugAsync(int? siteId, string slug) => Task.FromResult<KnowledgeArticle>(null);
        public Task<KnowledgeArticle> GetArticleBySourceKeyAsync(int? siteId, string sourceKey) => Task.FromResult<KnowledgeArticle>(null);
        public Task<List<KnowledgeArticle>> ListBySourcePrefixAsync(int? siteId, string prefix, string status = null, int take = 100)
            => Task.FromResult(Articles.Values.Where(a => (a.SourceKey ?? "").StartsWith(prefix ?? "")).ToList());
        public Task<int> UpsertArticleAsync(KnowledgeArticle article, IEnumerable<string> tags) => Task.FromResult(article.Id);
        public Task SetArticleStatusAsync(int id, string status) => Task.CompletedTask;
        public Task DeleteArticleAsync(int id) => Task.CompletedTask;
        public Task<List<KnowledgeTag>> ListTagsAsync(int? siteId) => Task.FromResult(new List<KnowledgeTag>());
    }
}
