using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Application.Services.Ai;
using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;

namespace MyShop.Tests;

public class AiPhase2LearnTests
{
    static readonly Guid ChatId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    const string LongAnswer = "برای ثبت خرید حضوری وارد پنل شوید، فروشگاه را انتخاب کنید و مبلغ را وارد کنید تا امتیاز ثبت شود.";
    const string LongQuestion = "چطور خرید حضوری را ثبت کنم؟";
    const string DraftJson =
        "{\"skip\":false,\"title\":\"ثبت خرید حضوری\",\"summary\":\"خرید را از پنل ثبت کنید.\",\"content\":\"برای ثبت خرید حضوری وارد پنل مشتری شوید، فروشگاه را انتخاب کنید و مبلغ فاکتور را وارد کنید.\",\"audience\":\"Customer\",\"tags\":[\"خرید\",\"امتیاز\"]}";

    [Fact]
    public void Extractor_PairsLastUserWithFollowingAdmin()
    {
        var pairs = AdminChatPairExtractor.Extract(new[]
        {
            Msg(1, SupportChatSenders.User, LongQuestion),
            Msg(2, SupportChatSenders.Admin, LongAnswer)
        });

        var pair = Assert.Single(pairs);
        Assert.Equal(1, pair.UserMessageId);
        Assert.Equal(2, pair.AdminMessageId);
        Assert.Equal(LongQuestion, pair.Question);
    }

    [Fact]
    public void Extractor_SkipsShortQuestionAndAnswer()
    {
        var pairs = AdminChatPairExtractor.Extract(new[]
        {
            Msg(1, SupportChatSenders.User, "سلام"),
            Msg(2, SupportChatSenders.Admin, LongAnswer),
            Msg(3, SupportChatSenders.User, LongQuestion),
            Msg(4, SupportChatSenders.Admin, "باشه")
        });

        Assert.Empty(pairs);
    }

    [Fact]
    public void Extractor_DoesNotTreatAiOrSystemAsAdminAnswer()
    {
        var pairs = AdminChatPairExtractor.Extract(new[]
        {
            Msg(1, SupportChatSenders.User, LongQuestion),
            Msg(2, SupportChatSenders.Ai, LongAnswer),
            Msg(3, SupportChatSenders.System, "گفتگو بسته شد و این یک پیام سیستمی طولانی است."),
            Msg(4, SupportChatSenders.Admin, LongAnswer)
        });

        var pair = Assert.Single(pairs);
        Assert.Equal(4, pair.AdminMessageId);
        Assert.Equal(1, pair.UserMessageId);
    }

    [Fact]
    public void Extractor_KeepsLastUserMessageWhenSeveralArriveBeforeAdmin()
    {
        var pairs = AdminChatPairExtractor.Extract(new[]
        {
            Msg(1, SupportChatSenders.User, "این پیام اول است که باید جایگزین شود."),
            Msg(2, SupportChatSenders.User, LongQuestion),
            Msg(3, SupportChatSenders.Admin, LongAnswer)
        });

        var pair = Assert.Single(pairs);
        Assert.Equal(2, pair.UserMessageId);
        Assert.Equal(LongQuestion, pair.Question);
    }

    [Fact]
    public void Extractor_CapsPairsPerChat()
    {
        var messages = new List<SupportChatMessage>();
        for (var i = 0; i < 5; i++)
        {
            messages.Add(Msg(i * 2 + 1, SupportChatSenders.User, LongQuestion + " " + i));
            messages.Add(Msg(i * 2 + 2, SupportChatSenders.Admin, LongAnswer + " " + i));
        }

        var pairs = AdminChatPairExtractor.Extract(messages);
        Assert.Equal(AiLearnDefaults.MaxPairsPerChat, pairs.Count);
        Assert.Equal(1, pairs[0].UserMessageId);
        Assert.Equal(3, pairs[1].UserMessageId);
    }

    [Fact]
    public void SourceKey_FitsNvarchar80()
    {
        var key = AiLearnDefaults.SourceKey(ChatId, long.MaxValue);
        Assert.StartsWith(AiLearnDefaults.SourcePrefix, key);
        Assert.True(key.Length <= 80, key);
    }

    [Fact]
    public void Parser_AcceptsJsonInsideMarkdownFence()
    {
        var ok = AiLearnDraftParser.TryParse("```json\n" + DraftJson + "\n```", KnowledgeAudiences.Customer, out var draft, out var skip);
        Assert.True(ok, skip);
        Assert.Equal("ثبت خرید حضوری", draft!.Title);
        Assert.Equal(KnowledgeAudiences.Customer, draft.Audience);
    }

    [Fact]
    public void Parser_BareSkip_CreatesNoDraft()
    {
        Assert.False(AiLearnDraftParser.TryParse("SKIP", KnowledgeAudiences.Customer, out _, out var reason));
        Assert.Equal("SKIP", reason);
    }

    [Fact]
    public void Parser_JsonSkip_CreatesNoDraft()
    {
        Assert.False(AiLearnDraftParser.TryParse(
            "{\"skip\":true,\"skipReason\":\"temporary-outage\"}",
            KnowledgeAudiences.Customer, out _, out var reason));
        Assert.Equal("temporary-outage", reason);
    }

    [Fact]
    public async Task Learner_SkipsLowCsat()
    {
        var llm = new FakeLlm(DraftJson);
        var kb = new FakeKbStore();
        var sut = Build(llm, kb, csatScore: 2);

        var result = await sut.ProcessSiteAsync(1);

        Assert.Equal(0, llm.CallCount);
        Assert.Empty(kb.Articles);
        Assert.Equal(0, result.DraftsCreated);
        Assert.True(result.Skipped >= 1);
    }

    [Fact]
    public async Task Learner_AcceptsHighCsat_AndSavesDraftNotPublished()
    {
        var llm = new FakeLlm(DraftJson);
        var kb = new FakeKbStore();
        var sut = Build(llm, kb, csatScore: 5);

        var result = await sut.ProcessSiteAsync(1);

        Assert.Equal(1, llm.CallCount);
        Assert.Equal(1, result.DraftsCreated);
        var article = Assert.Single(kb.Articles.Values);
        Assert.Equal(KnowledgeArticleStatuses.Draft, article.Status);
        Assert.Null(article.PublishedAt);
        Assert.Equal(ChatId, article.SourceChatId);
        Assert.Equal(AiLearnDefaults.SourceKey(ChatId, 1), article.SourceKey);
        Assert.StartsWith(AiLearnDefaults.SourcePrefix, article.SourceKey);
    }

    [Fact]
    public async Task Learner_SkipsWhenRetrievalAlreadyCoversQuestion()
    {
        var llm = new FakeLlm(DraftJson);
        var kb = new FakeKbStore();
        var retrieval = new RetrievalResult(
            new[] { new RetrievedArticle(9, "ثبت خرید", "buy", "متن", 80) }, 80);
        var sut = Build(llm, kb, csatScore: 5, retrieval: retrieval);

        var result = await sut.ProcessSiteAsync(1);

        Assert.Equal(0, llm.CallCount);
        Assert.Empty(kb.Articles);
        Assert.Equal(0, result.DraftsCreated);
    }

    [Fact]
    public async Task Learner_IsIdempotentOnSourceKey()
    {
        var llm = new FakeLlm(DraftJson);
        var kb = new FakeKbStore();
        kb.Articles[1] = new KnowledgeArticle
        {
            Id = 1,
            SourceKey = AiLearnDefaults.SourceKey(ChatId, 1),
            Status = KnowledgeArticleStatuses.Draft,
            Title = "قبلی"
        };
        var sut = Build(llm, kb, csatScore: 5);

        var result = await sut.ProcessSiteAsync(1);

        Assert.Equal(0, llm.CallCount);
        Assert.Equal(0, result.DraftsCreated);
        Assert.Single(kb.Articles);
        Assert.Equal("قبلی", kb.Articles[1].Title);
    }

    [Fact]
    public async Task Learner_LlmSkip_CreatesNoArticle()
    {
        var llm = new FakeLlm("{\"skip\":true,\"skipReason\":\"temporary-outage\"}");
        var kb = new FakeKbStore();
        var sut = Build(llm, kb, csatScore: 5);

        var result = await sut.ProcessSiteAsync(1);

        Assert.Equal(1, llm.CallCount);
        Assert.Empty(kb.Articles);
        Assert.Equal(0, result.DraftsCreated);
    }

    [Fact]
    public async Task Learner_NeverPublishesEvenIfSwitchIsOn()
    {
        var llm = new FakeLlm(DraftJson);
        var kb = new FakeKbStore();
        var sut = Build(llm, kb, csatScore: 5);

        await sut.ProcessSiteAsync(1);

        Assert.All(kb.Articles.Values, a =>
        {
            Assert.Equal(KnowledgeArticleStatuses.Draft, a.Status);
            Assert.Null(a.PublishedAt);
        });
    }

    static AiKbDraftLearner Build(
        FakeLlm llm,
        FakeKbStore kb,
        byte csatScore,
        RetrievalResult? retrieval = null)
    {
        var chat = new SupportChatThread
        {
            Id = ChatId,
            SiteId = 1,
            Status = SupportChatStatuses.Closed,
            ClosedAt = DateTime.UtcNow.AddMinutes(-10),
            Audience = "Guest"
        };
        var messages = new List<SupportChatMessage>
        {
            Msg(1, SupportChatSenders.User, LongQuestion),
            Msg(2, SupportChatSenders.Admin, LongAnswer)
        };
        var options = AiRuntimeOptions.Defaults with { LearnFromAdminChat = true, MinRetrievalScore = 24 };

        return new AiKbDraftLearner(
            new FakeLearnRepo(chat),
            new FakeChats(messages),
            new FakeAnalytics(csatScore),
            kb,
            new FakeRetrieval(retrieval ?? RetrievalResult.Empty),
            llm,
            new FakeOptions(options));
    }

    static SupportChatMessage Msg(long id, string sender, string body) => new()
    {
        Id = id,
        ChatId = ChatId,
        SenderType = sender,
        Body = body,
        MessageType = SupportMessageTypes.Text,
        CreatedAt = DateTime.UtcNow
    };

    sealed class FakeLlm : IAiCompletionClient
    {
        readonly string _text;
        public FakeLlm(string text) => _text = text;
        public int CallCount { get; private set; }
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request)
        {
            CallCount++;
            return Task.FromResult(new AiCompletionResult(true, _text, null, 10, 20, "fake", 5));
        }
    }

    sealed class FakeRetrieval : IKnowledgeRetrieval
    {
        readonly RetrievalResult _result;
        public FakeRetrieval(RetrievalResult result) => _result = result;
        public Task<RetrievalResult> RetrieveAsync(string query, int? siteId, AiAudience audience, int topK = 4)
            => Task.FromResult(_result);
    }

    sealed class FakeOptions : IAiRuntimeOptionsProvider
    {
        readonly AiRuntimeOptions _options;
        public FakeOptions(AiRuntimeOptions options) => _options = options;
        public Task<AiRuntimeOptions> GetAsync(int? siteId) => Task.FromResult(_options);
    }

    sealed class FakeLearnRepo : IAiKbLearnRepository
    {
        readonly SupportChatThread _chat;
        public FakeLearnRepo(SupportChatThread chat) => _chat = chat;
        public DateTime? SavedWatermark { get; private set; }

        public Task<AiKbLearnCursor> GetCursorAsync(int siteId)
            => Task.FromResult(new AiKbLearnCursor { SiteId = siteId });

        public Task SaveCursorAsync(int siteId, DateTime? lastClosedAt, string result)
        {
            SavedWatermark = lastClosedAt;
            return Task.CompletedTask;
        }

        public Task<List<int>> ListEnabledSiteIdsAsync() => Task.FromResult(new List<int> { 1 });

        public Task<List<SupportChatThread>> ListClosedAfterAsync(int? siteId, DateTime? afterClosedAt, int take)
            => Task.FromResult(new List<SupportChatThread> { _chat });
    }

    sealed class FakeChats : ISupportChatRepository
    {
        readonly List<SupportChatMessage> _messages;
        public FakeChats(List<SupportChatMessage> messages) => _messages = messages;

        public Task<SupportChatThread> GetAsync(Guid id) => Task.FromResult<SupportChatThread>(null!);
        public Task<SupportChatThread> GetOpenAsync(int? siteId, int? userId, string guestToken) => Task.FromResult<SupportChatThread>(null!);
        public Task<SupportChatThread> CreateAsync(SupportChatThread row) => Task.FromResult(row);
        public Task<List<SupportChatThread>> ListForAdminAsync(int? siteId, string status, int take = 80)
            => Task.FromResult(new List<SupportChatThread>());
        public Task AddMessageAsync(SupportChatMessage msg) => Task.CompletedTask;
        public Task<List<SupportChatMessage>> ListMessagesAsync(Guid chatId, long afterId = 0)
            => Task.FromResult(_messages.Where(m => m.Id > afterId).ToList());
        public Task TouchAsync(Guid chatId, string preview, string status, bool incrementAdminUnread, bool incrementUserUnread, bool clearAdminUnread, bool clearUserUnread, int? assignedAdminUserId, string currentPageUrl = null)
            => Task.CompletedTask;
        public Task CloseAsync(Guid chatId) => Task.CompletedTask;
        public Task MarkMessagesReadAsync(Guid chatId, string readerSide) => Task.CompletedTask;
        public Task UpdatePageAsync(Guid chatId, string pageUrl) => Task.CompletedTask;
        public Task<SupportSettings> GetSettingsAsync(int siteId) => Task.FromResult(new SupportSettings { SiteId = siteId });
        public Task UpsertSettingsAsync(SupportSettings settings) => Task.CompletedTask;
    }

    sealed class FakeAnalytics : ISupportAnalyticsRepository
    {
        readonly byte _score;
        public FakeAnalytics(byte score) => _score = score;

        public Task MarkFirstResponseAsync(Guid chatId) => Task.CompletedTask;
        public Task UpsertCsatAsync(SupportCsatRating rating) => Task.CompletedTask;
        public Task<SupportCsatRating> GetCsatAsync(Guid chatId)
            => Task.FromResult(new SupportCsatRating { ChatId = chatId, Score = _score });
        public Task<SupportAnalyticsSummary> GetSummaryAsync(SupportAnalyticsFilter filter)
            => Task.FromResult(new SupportAnalyticsSummary());
        public Task<List<SupportDailyStat>> GetDailyAsync(SupportAnalyticsFilter filter)
            => Task.FromResult(new List<SupportDailyStat>());
        public Task<List<SupportOperatorStat>> GetOperatorStatsAsync(SupportAnalyticsFilter filter)
            => Task.FromResult(new List<SupportOperatorStat>());
    }

    sealed class FakeKbStore : IKnowledgeService
    {
        int _next = 1;
        public Dictionary<int, KnowledgeArticle> Articles { get; } = new();

        public Task<List<KnowledgeCategory>> ListCategoriesAsync(int? siteId, bool activeOnly = false)
            => Task.FromResult(new List<KnowledgeCategory>());
        public Task<int> SaveCategoryAsync(KnowledgeCategory cat) => Task.FromResult(0);
        public Task DeleteCategoryAsync(int id) => Task.CompletedTask;
        public Task<List<KnowledgeArticle>> ListArticlesAsync(int? siteId, string? status = null, int? categoryId = null, string? type = null, string? search = null, bool publicOnly = false)
            => Task.FromResult(Articles.Values.ToList());
        public Task<List<KnowledgeArticle>> SearchForRagAsync(int? siteId, IEnumerable<string> tokens, string audience, int take = 24)
            => Task.FromResult(new List<KnowledgeArticle>());
        public Task<KnowledgeArticle?> GetAsync(int id)
            => Task.FromResult(Articles.TryGetValue(id, out var a) ? a : null);
        public Task<KnowledgeArticle?> GetBySlugAsync(int? siteId, string slug) => Task.FromResult<KnowledgeArticle?>(null);
        public Task<KnowledgeArticle?> GetBySourceKeyAsync(int? siteId, string sourceKey)
            => Task.FromResult(Articles.Values.FirstOrDefault(a => a.SourceKey == sourceKey));
        public Task<List<KnowledgeArticle>> ListBySourcePrefixAsync(int? siteId, string prefix, string status = null)
            => Task.FromResult(Articles.Values.Where(a => (a.SourceKey ?? "").StartsWith(prefix ?? "")).ToList());
        public Task<int> SaveArticleAsync(KnowledgeArticle article, IEnumerable<string> tags, int? authorUserId = null)
        {
            if (article.Id <= 0) article.Id = _next++;
            Articles[article.Id] = article;
            return Task.FromResult(article.Id);
        }
        public Task PublishAsync(int id)
        {
            if (Articles.TryGetValue(id, out var a))
            {
                a.Status = KnowledgeArticleStatuses.Published;
                a.PublishedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }
        public Task ArchiveAsync(int id) => Task.CompletedTask;
        public Task DeleteArticleAsync(int id) => Task.CompletedTask;
        public Task<List<KnowledgeTag>> ListTagsAsync(int? siteId) => Task.FromResult(new List<KnowledgeTag>());
        public Task<List<KnowledgeArticle>> SearchPublicAsync(int? siteId, string query, int take = 20)
            => Task.FromResult(new List<KnowledgeArticle>());
    }
}
