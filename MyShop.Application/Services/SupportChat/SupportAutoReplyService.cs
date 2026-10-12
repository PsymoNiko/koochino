using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Application.Services.Ai;
using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SupportChat
{
    /// <summary>
    /// First-line answer in the live chat while the visitor waits for an operator. Uses the same knowledge
    /// retrieval and relevance threshold as the AI assistant; the LLM only rephrases the retrieved article
    /// and is skipped entirely unless the site enabled it.
    /// </summary>
    public class SupportAutoReplyService : ISupportAutoReplyService
    {
        static readonly Regex RefPattern = new(@"\[\[ref:\s*(\d+)\s*\]\]", RegexOptions.Compiled);
        const string OutOfScopeMarker = "OUT_OF_SCOPE";

        private readonly IKnowledgeRetrieval _retrieval;
        private readonly ISupportChatRepository _chats;
        private readonly ISupportChatRealtimePublisher _realtime;
        private readonly IAiCompletionClient _ai;

        public SupportAutoReplyService(
            IKnowledgeRetrieval retrieval,
            ISupportChatRepository chats,
            ISupportChatRealtimePublisher realtime = null,
            IAiCompletionClient ai = null)
        {
            _retrieval = retrieval;
            _chats = chats;
            _realtime = realtime;
            _ai = ai;
        }

        public async Task TryAfterUserMessageAsync(SupportChatThread chat, SupportChatMessage userMessage)
        {
            if (chat == null || userMessage == null || string.IsNullOrWhiteSpace(userMessage.Body)) return;
            if (chat.AssignedAdminUserId.HasValue && chat.Status == SupportChatStatuses.Active) return;
            if (!chat.SiteId.HasValue || chat.SiteId.Value <= 0) return;

            SupportSettings settings;
            try { settings = await _chats.GetSettingsAsync(chat.SiteId.Value); }
            catch { return; }
            if (settings == null || !settings.AutoReplyEnabled) return;

            var existing = await _chats.ListMessagesAsync(chat.Id);
            var aiCount = existing.Count(m => string.Equals(m.SenderType, SupportChatSenders.Ai, StringComparison.OrdinalIgnoreCase));
            var max = settings.AutoReplyMaxPerChat <= 0 ? 2 : settings.AutoReplyMaxPerChat;
            if (aiCount >= max) return;

            RetrievalResult retrieval;
            try
            {
                retrieval = await _retrieval.RetrieveAsync(userMessage.Body, chat.SiteId, MapAudience(chat.Audience), 3);
            }
            catch { return; }

            var min = settings.AutoReplyMinScore <= 0 ? 40 : settings.AutoReplyMinScore;
            if (!retrieval.HasContent || retrieval.BestScore < min) return;

            var body = await ComposeAsync(settings, retrieval, userMessage.Body);
            if (string.IsNullOrWhiteSpace(body)) return;

            var msg = new SupportChatMessage
            {
                ChatId = chat.Id,
                SenderType = SupportChatSenders.Ai,
                Body = body.Trim(),
                MessageType = SupportMessageTypes.Text
            };
            await _chats.AddMessageAsync(msg);
            await _chats.TouchAsync(chat.Id, Trunc(msg.Body, 200), SupportChatStatuses.Waiting,
                incrementAdminUnread: false, incrementUserUnread: true, clearAdminUnread: false, clearUserUnread: false, assignedAdminUserId: null);
            if (_realtime != null)
                await _realtime.NewMessageAsync(chat.Id, chat.SiteId, msg);
        }

        async Task<string> ComposeAsync(SupportSettings settings, RetrievalResult retrieval, string question)
        {
            var top = retrieval.Items.First();

            if (settings.AutoReplyUseAi && _ai != null)
            {
                try
                {
                    if (await _ai.IsConfiguredAsync())
                    {
                        var result = await _ai.CompleteAsync(new AiCompletionRequest(
                            AiSystemPromptBuilder.BuildAutoReply(retrieval),
                            Array.Empty<AiChatMessage>(),
                            question,
                            MaxTokens: 350,
                            Temperature: 0.2));

                        var phrased = Verify(result, retrieval);
                        if (!string.IsNullOrWhiteSpace(phrased))
                            return Decorate(phrased, top.Slug, settings.AutoReplyHandoffHint);
                    }
                }
                catch
                {
                    // Falls through to the free excerpt reply below.
                }
            }

            var excerpt = "پاسخ پیشنهادی از پایگاه دانش:\n\n" + top.Title + "\n" + top.Text;
            return Decorate(excerpt, top.Slug, settings.AutoReplyHandoffHint);
        }

        static string Verify(AiCompletionResult result, RetrievalResult retrieval)
        {
            if (result == null || !result.Ok || string.IsNullOrWhiteSpace(result.Text)) return null;
            var text = result.Text.Trim();
            if (text.IndexOf(OutOfScopeMarker, StringComparison.OrdinalIgnoreCase) >= 0) return null;

            var match = RefPattern.Match(text);
            text = RefPattern.Replace(text, "").Trim();
            if (text.Length < 4) return null;

            if (match.Success && int.TryParse(match.Groups[1].Value, out var id) &&
                retrieval.Items.Any(x => x.Id == id))
                return text;

            // No usable citation: only trust it when the question matched the knowledge base very strongly.
            return retrieval.BestScore >= 60 ? text : null;
        }

        static string Decorate(string body, string slug, string handoffHint)
        {
            var sb = body.TrimEnd();
            if (!string.IsNullOrWhiteSpace(slug))
                sb += "\n\nجزئیات: /help/" + slug;
            if (!string.IsNullOrWhiteSpace(handoffHint))
                sb += "\n" + handoffHint;
            return sb;
        }

        static AiAudience MapAudience(string audience) =>
            string.Equals(audience, "StoreOwner", StringComparison.OrdinalIgnoreCase)
                ? AiAudience.StoreOwner
                : AiAudience.Customer;

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
