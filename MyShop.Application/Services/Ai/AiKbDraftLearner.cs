using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Mines closed, well-rated admin chats into knowledge-base drafts. Drafts are never published here;
    /// a human must approve them in the knowledge admin.
    /// </summary>
    public class AiKbDraftLearner : IAiKbDraftLearner
    {
        private readonly IAiKbLearnRepository _learn;
        private readonly ISupportChatRepository _chats;
        private readonly ISupportAnalyticsRepository _analytics;
        private readonly IKnowledgeService _kb;
        private readonly IKnowledgeRetrieval _retrieval;
        private readonly IAiCompletionClient _llm;
        private readonly IAiRuntimeOptionsProvider _options;
        private readonly IAiUsageGuard _usage;

        public AiKbDraftLearner(
            IAiKbLearnRepository learn,
            ISupportChatRepository chats,
            ISupportAnalyticsRepository analytics,
            IKnowledgeService kb,
            IKnowledgeRetrieval retrieval,
            IAiCompletionClient llm,
            IAiRuntimeOptionsProvider options = null,
            IAiUsageGuard usage = null)
        {
            _learn = learn;
            _chats = chats;
            _analytics = analytics;
            _kb = kb;
            _retrieval = retrieval;
            _llm = llm;
            _options = options;
            _usage = usage;
        }

        public async Task<AiLearnRunResult> ProcessSiteAsync(int? siteId)
        {
            if (siteId is > 0)
                return await ProcessOneAsync(siteId.Value);

            List<int> sites;
            try { sites = await _learn.ListEnabledSiteIdsAsync(); }
            catch (Exception ex)
            {
                return new AiLearnRunResult(0, 0, 0, 0, ex.Message);
            }

            if (sites == null || sites.Count == 0)
                return new AiLearnRunResult(0, 0, 0, 0);

            int scanned = 0, drafts = 0, skipped = 0, tokens = 0;
            string error = null;
            foreach (var id in sites)
            {
                var one = await ProcessOneAsync(id);
                scanned += one.ChatsScanned;
                drafts += one.DraftsCreated;
                skipped += one.Skipped;
                tokens += one.Tokens;
                if (!one.Ok && error == null) error = one.Error;
            }

            return new AiLearnRunResult(scanned, drafts, skipped, tokens, error);
        }

        async Task<AiLearnRunResult> ProcessOneAsync(int siteId)
        {
            AiRuntimeOptions options;
            try { options = _options == null ? AiRuntimeOptions.Defaults : await _options.GetAsync(siteId); }
            catch { options = AiRuntimeOptions.Defaults; }

            if (options == null || !options.LearnFromAdminChat)
                return await FinishAsync(siteId, 0, 0, 0, 0, null, "یادگیری از چت خاموش است.");

            if (_llm == null || !await _llm.IsConfiguredAsync())
                return await FinishAsync(siteId, 0, 0, 0, 0, null, "کلید یا سرویس هوش مصنوعی تنظیم نشده است.");

            AiKbLearnCursor cursor;
            try { cursor = await _learn.GetCursorAsync(siteId); }
            catch (Exception ex)
            {
                return new AiLearnRunResult(0, 0, 0, 0, ex.Message);
            }

            List<SupportChatThread> chats;
            try
            {
                chats = await _learn.ListClosedAfterAsync(siteId, cursor?.LastClosedAt, AiLearnDefaults.ChatsPerRun);
            }
            catch (Exception ex)
            {
                return await FinishAsync(siteId, 0, 0, 0, 0, cursor?.LastClosedAt, ex.Message);
            }

            int scanned = 0, drafts = 0, skipped = 0, tokens = 0;
            DateTime? watermark = cursor?.LastClosedAt;

            foreach (var chat in chats ?? new List<SupportChatThread>())
            {
                scanned++;
                if (chat.ClosedAt.HasValue && (watermark == null || chat.ClosedAt > watermark))
                    watermark = chat.ClosedAt;

                try
                {
                    var created = await ProcessChatAsync(chat, options);
                    drafts += created.Drafts;
                    skipped += created.Skipped;
                    tokens += created.Tokens;
                }
                catch
                {
                    skipped++;
                }
            }

            var result = new AiLearnRunResult(scanned, drafts, skipped, tokens);
            await TrySaveCursorAsync(siteId, watermark, Summarize(result));
            return result;
        }

        async Task<(int Drafts, int Skipped, int Tokens)> ProcessChatAsync(SupportChatThread chat, AiRuntimeOptions options)
        {
            int drafts = 0, skipped = 0, tokens = 0;

            SupportCsatRating csat = null;
            try { csat = await _analytics.GetCsatAsync(chat.Id); }
            catch { }

            if (csat == null || csat.Score < AiLearnDefaults.MinCsatScore)
                return (0, 1, 0);

            List<SupportChatMessage> messages;
            try { messages = await _chats.ListMessagesAsync(chat.Id); }
            catch { return (0, 1, 0); }

            var pairs = AdminChatPairExtractor.Extract(messages);
            if (pairs.Count == 0) return (0, 1, 0);

            var audience = AdminChatPairExtractor.MapAudience(chat.Audience);
            var articleAudience = AdminChatPairExtractor.MapArticleAudience(chat.Audience);
            var minScore = options.MinRetrievalScore > 0 ? options.MinRetrievalScore : AiRuntimeOptions.Defaults.MinRetrievalScore;

            foreach (var pair in pairs)
            {
                var sourceKey = AiLearnDefaults.SourceKey(chat.Id, pair.UserMessageId);
                KnowledgeArticle existing = null;
                try { existing = await _kb.GetBySourceKeyAsync(chat.SiteId, sourceKey); }
                catch { }

                if (existing != null)
                {
                    skipped++;
                    continue;
                }

                RetrievalResult retrieval;
                try { retrieval = await _retrieval.RetrieveAsync(pair.Question, chat.SiteId, audience, 4); }
                catch { retrieval = RetrievalResult.Empty; }

                if (retrieval != null && retrieval.BestScore >= minScore)
                {
                    skipped++;
                    continue;
                }

                var completion = await _llm.CompleteAsync(new AiCompletionRequest(
                    SystemPrompt: AiSystemPromptBuilder.BuildLearnDraft(articleAudience),
                    History: Array.Empty<AiChatMessage>(),
                    UserMessage: "QUESTION:\n" + pair.Question + "\n\nADMIN ANSWER:\n" + pair.Answer,
                    MaxTokens: 900,
                    Temperature: options.Temperature));

                tokens += completion.TotalTokens;
                if (_usage != null)
                    await _usage.RecordAsync(new AiChatContext(SiteId: chat.SiteId), completion.PromptTokens, completion.CompletionTokens);

                if (!completion.Ok || string.IsNullOrWhiteSpace(completion.Text))
                {
                    skipped++;
                    continue;
                }

                if (!AiLearnDraftParser.TryParse(completion.Text, articleAudience, out var draft, out _))
                {
                    skipped++;
                    continue;
                }

                var article = new KnowledgeArticle
                {
                    SiteId = chat.SiteId is > 0 ? chat.SiteId : null,
                    Title = draft.Title,
                    Summary = draft.Summary,
                    Content = draft.Content,
                    ArticleType = KnowledgeArticleTypes.Faq,
                    Audience = draft.Audience,
                    SourceKey = sourceKey,
                    SourceChatId = chat.Id,
                    Status = KnowledgeArticleStatuses.Draft,
                    IsPublic = true,
                    PublishedAt = null
                };

                try
                {
                    await _kb.SaveArticleAsync(article, draft.Tags);
                    drafts++;
                }
                catch
                {
                    skipped++;
                }
            }

            return (drafts, skipped, tokens);
        }

        async Task<AiLearnRunResult> FinishAsync(
            int siteId, int scanned, int drafts, int skipped, int tokens, DateTime? watermark, string error)
        {
            var result = new AiLearnRunResult(scanned, drafts, skipped, tokens, error);
            await TrySaveCursorAsync(siteId, watermark, Summarize(result));
            return result;
        }

        async Task TrySaveCursorAsync(int siteId, DateTime? watermark, string summary)
        {
            try { await _learn.SaveCursorAsync(siteId, watermark, summary); }
            catch { }
        }

        static string Summarize(AiLearnRunResult r)
        {
            var text = $"scanned={r.ChatsScanned} drafts={r.DraftsCreated} skipped={r.Skipped} tokens={r.Tokens}";
            if (!string.IsNullOrEmpty(r.Error)) text += " err=" + Trunc(r.Error, 200);
            return Trunc(text, 400);
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
