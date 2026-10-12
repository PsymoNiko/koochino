using MyShop.Application.Interfaces.Ai;
using MyShop.Core.Models.Ai;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    public class KnowledgeBaseSkill : IAiSkill
    {
        public string Intent => "KnowledgeBase";

        /// <summary>
        /// Runs last on purpose: this skill matches on any single keyword hit, so ahead of retrieval it
        /// would swallow almost every message and return a canned answer.
        /// </summary>
        public int Priority => AiSkillPriorities.StaticFaqFallback;

        public bool CanHandle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return KochinoKnowledgeBase.All.Any(f => Score(text, f.Keywords) > 0);
        }

        public Task<string> HandleAsync(string message, AiChatContext context)
        {
            context ??= new AiChatContext();
            var audience = context.Audience;
            var normalized = message.Trim().ToLowerInvariant();

            var candidates = KochinoKnowledgeBase.All
                .Where(f => MatchesAudience(f.Audience, audience))
                .Select(f => new { Faq = f, Score = Score(normalized, f.Keywords) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            if (candidates.Count == 0)
                return Task.FromResult(GetFallback(audience));

            return Task.FromResult(candidates[0].Faq.Answer);
        }

        private static bool MatchesAudience(AiAudience faqAudience, AiAudience contextAudience) =>
            faqAudience == AiAudience.Both ||
            faqAudience == contextAudience ||
            contextAudience == AiAudience.Both;

        private static int Score(string text, string[] keywords)
        {
            var haystack = PersianTextScorer.Normalize(text);
            if (haystack.Length == 0) return 0;
            var score = 0;
            foreach (var kw in keywords)
            {
                if (string.IsNullOrWhiteSpace(kw)) continue;
                var needle = PersianTextScorer.Normalize(kw);
                if (needle.Length > 0 && haystack.Contains(needle, StringComparison.Ordinal))
                    score += needle.Length >= 6 ? 3 : 2;
            }
            return score;
        }

        private static string GetFallback(AiAudience audience)
        {
            if (audience == AiAudience.StoreOwner)
                return "سوال شما را دقیق متوجه نشدم. درباره ثبت فروشگاه، جایزه، تخفیف، مأموریت یا جذب مشتری بپرسید.";
            return "سوال شما را دقیق متوجه نشدم. درباره ثبت‌نام، ثبت خرید، امتیاز، تخفیف، مأموریت یا فروشگاه بپرسید.";
        }
    }
}
