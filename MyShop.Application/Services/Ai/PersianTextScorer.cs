using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Persian-aware normalization, tokenization and keyword scoring shared by the AI retrieval path
    /// and the free live-chat auto-reply. Both must agree, otherwise the relevance threshold configured
    /// by the admin would mean two different things in two places.
    /// </summary>
    public static class PersianTextScorer
    {
        static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "و","از","در","به","که","را","با","این","آن","برای","یک","می","من","شما","ما","است","هست",
            "شد","بشه","میشه","باشد","بود","کرد","کنم","کنید","چه","چی","چگونه","چطور","آیا","تا","هم",
            "لطفا","لطفاً","سلام","خسته","نباشید","ممنون","مرسی","درباره","مورد","های","ها"
        };

        static readonly Regex Splitter = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);
        static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Folds the Arabic/Persian character variants that Persian users mix freely. Without this,
        /// "كد تخفيف" typed with Arabic letters never matches "کد تخفیف" stored in the knowledge base.
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var sb = new StringBuilder(text.Length);
            foreach (var raw in text.Trim())
            {
                var ch = raw;
                switch (ch)
                {
                    case '\u064A': // Arabic yeh
                    case '\u0649': // Alef maksura
                        ch = '\u06CC'; break;
                    case '\u0643': // Arabic kaf
                        ch = '\u06A9'; break;
                    case '\u0629': // Teh marbuta
                        ch = '\u0647'; break;
                    case '\u0623':
                    case '\u0625':
                    case '\u0622':
                    case '\u0671':
                        ch = '\u0627'; break;
                    case '\u0624':
                        ch = '\u0648'; break;
                    case '\u200C': // ZWNJ removed so "نمی‌دانم" and "نمیدانم" collapse together
                    case '\u200D':
                    case '\u200E':
                    case '\u200F':
                        continue;
                }

                // Arabic-Indic and extended (Persian) digits to ASCII
                if (ch >= '\u0660' && ch <= '\u0669') ch = (char)('0' + (ch - '\u0660'));
                else if (ch >= '\u06F0' && ch <= '\u06F9') ch = (char)('0' + (ch - '\u06F0'));

                // Harakat / tatweel carry no search value
                if ((ch >= '\u064B' && ch <= '\u0652') || ch == '\u0640' || ch == '\u0670') continue;

                sb.Append(char.ToLowerInvariant(ch));
            }
            return Whitespace.Replace(sb.ToString(), " ").Trim();
        }

        /// <summary>Normalized, stop-word filtered tokens, longest first so the most specific term drives the SQL filter.</summary>
        public static List<string> Tokenize(string text, int max = 8)
        {
            var normalized = Normalize(text);
            if (normalized.Length == 0) return new List<string>();
            return Splitter.Split(normalized)
                .Where(t => t.Length >= 2 && !StopWords.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(t => t.Length)
                .Take(max <= 0 ? 8 : max)
                .ToList();
        }

        /// <summary>Keyword relevance of an article for a query, capped at 100.</summary>
        public static int ScoreArticle(string query, IReadOnlyList<string> tokens, KnowledgeArticle article)
        {
            if (article == null) return 0;

            var q = Normalize(query);
            var title = Normalize(article.Title);
            var summary = Normalize(article.Summary);
            var content = Normalize(article.Content);
            var tags = (article.Tags ?? new List<string>()).Select(Normalize).ToList();

            var score = 0;
            if (q.Length > 0)
            {
                if (title.Contains(q, StringComparison.Ordinal)) score += 50;
                if (summary.Contains(q, StringComparison.Ordinal)) score += 18;
            }

            foreach (var token in tokens ?? Array.Empty<string>())
            {
                if (title.Contains(token, StringComparison.Ordinal)) score += 20;
                if (tags.Any(t => t.Contains(token, StringComparison.Ordinal))) score += 15;
                if (summary.Contains(token, StringComparison.Ordinal)) score += 8;
                if (content.Contains(token, StringComparison.Ordinal)) score += 4;
            }

            if (string.Equals(article.ArticleType, KnowledgeArticleTypes.Faq, StringComparison.OrdinalIgnoreCase))
                score += 6;

            return score > 100 ? 100 : score;
        }

        public static string Excerpt(string summary, string content, int max)
        {
            var text = !string.IsNullOrWhiteSpace(summary) ? summary.Trim() : (content ?? "").Trim();
            text = Whitespace.Replace(text, " ");
            if (text.Length <= max) return text;
            return text.Substring(0, max) + "…";
        }

        /// <summary>Stable key used to deduplicate unanswered questions and cache repeated answers.</summary>
        public static string CacheKey(string text, int maxLength = 300)
        {
            var tokens = Tokenize(text, 12);
            var key = tokens.Count > 0
                ? string.Join(" ", tokens.OrderBy(t => t, StringComparer.Ordinal))
                : Normalize(text);
            return key.Length <= maxLength ? key : key.Substring(0, maxLength);
        }
    }
}
