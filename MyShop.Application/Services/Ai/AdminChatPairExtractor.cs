using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Pulls durable FAQ-shaped pairs out of a closed live-chat transcript. Pure: no I/O, so the pairing
    /// rules can be unit-tested without a database.
    /// </summary>
    public static class AdminChatPairExtractor
    {
        public static List<AdminChatPair> Extract(IReadOnlyList<SupportChatMessage> messages)
        {
            var pairs = new List<AdminChatPair>();
            SupportChatMessage pendingUser = null;

            if (messages == null) return pairs;

            foreach (var msg in messages)
            {
                if (msg == null) continue;
                var sender = msg.SenderType ?? "";
                var body = (msg.Body ?? "").Trim();

                if (string.Equals(sender, SupportChatSenders.User, StringComparison.OrdinalIgnoreCase))
                {
                    if (body.Length > 0) pendingUser = msg;
                    continue;
                }

                if (string.Equals(sender, SupportChatSenders.Admin, StringComparison.OrdinalIgnoreCase))
                {
                    if (pendingUser != null)
                    {
                        var question = (pendingUser.Body ?? "").Trim();
                        if (question.Length >= AiLearnDefaults.MinQuestionChars
                            && body.Length >= AiLearnDefaults.MinAnswerChars
                            && pairs.Count < AiLearnDefaults.MaxPairsPerChat)
                        {
                            pairs.Add(new AdminChatPair(pendingUser.Id, question, msg.Id, body));
                        }
                        pendingUser = null;
                    }
                    continue;
                }

                // Ai / System: never treat as the answer, and do not consume the pending user question.
                // The human operator's later reply is what may become a knowledge-base draft.
            }

            return pairs;
        }

        public static AiAudience MapAudience(string threadAudience)
        {
            if (string.Equals(threadAudience, KnowledgeAudiences.StoreOwner, StringComparison.OrdinalIgnoreCase)
                || string.Equals(threadAudience, "Store", StringComparison.OrdinalIgnoreCase))
                return AiAudience.StoreOwner;
            return AiAudience.Customer;
        }

        public static string MapArticleAudience(string threadAudience)
        {
            return MapAudience(threadAudience) == AiAudience.StoreOwner
                ? KnowledgeAudiences.StoreOwner
                : KnowledgeAudiences.Customer;
        }
    }
}
