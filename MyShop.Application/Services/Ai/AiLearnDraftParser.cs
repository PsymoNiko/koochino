using MyShop.Core.Models.Ai;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MyShop.Application.Services.Ai
{
    public record AiLearnDraft(string Title, string Summary, string Content, string Audience, IReadOnlyList<string> Tags);

    /// <summary>
    /// Turns the learning-model reply into either a draft article or an explicit skip.
    /// Accepts raw JSON, JSON wrapped in markdown fences, or a bare SKIP token.
    /// </summary>
    public static class AiLearnDraftParser
    {
        static readonly Regex Fence = new(@"^```(?:json)?\s*|\s*```$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static bool TryParse(string llmText, string fallbackAudience, out AiLearnDraft draft, out string skipReason)
        {
            draft = null;
            skipReason = null;
            var text = (llmText ?? "").Trim();
            if (text.Length == 0)
            {
                skipReason = "empty";
                return false;
            }

            if (IsBareSkip(text))
            {
                skipReason = "SKIP";
                return false;
            }

            text = Fence.Replace(text, "").Trim();
            var json = ExtractJsonObject(text);
            if (json == null)
            {
                skipReason = "not-json";
                return false;
            }

            LearnDto dto;
            try { dto = JsonSerializer.Deserialize<LearnDto>(json, JsonOpts); }
            catch
            {
                skipReason = "bad-json";
                return false;
            }

            if (dto == null)
            {
                skipReason = "bad-json";
                return false;
            }

            if (dto.Skip)
            {
                skipReason = string.IsNullOrWhiteSpace(dto.SkipReason) ? "SKIP" : dto.SkipReason.Trim();
                return false;
            }

            var title = (dto.Title ?? "").Trim();
            var content = (dto.Content ?? "").Trim();
            if (title.Length < 4 || content.Length < AiLearnDefaults.MinAnswerChars)
            {
                skipReason = "too-short";
                return false;
            }

            var summary = string.IsNullOrWhiteSpace(dto.Summary)
                ? PersianTextScorer.Excerpt(null, content, 280)
                : dto.Summary.Trim();
            var audience = NormalizeAudience(dto.Audience, fallbackAudience);
            var tags = (dto.Tags ?? Array.Empty<string>())
                .Select(t => (t ?? "").Trim())
                .Where(t => t.Length > 0 && t.Length <= 40)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            draft = new AiLearnDraft(title, summary, content, audience, tags);
            return true;
        }

        static bool IsBareSkip(string text)
        {
            if (text.Equals("SKIP", StringComparison.OrdinalIgnoreCase)) return true;
            if (text.StartsWith("SKIP", StringComparison.OrdinalIgnoreCase)
                && (text.Length == 4 || char.IsWhiteSpace(text[4]) || text[4] == '\n' || text[4] == '\r'))
                return true;
            return false;
        }

        static string ExtractJsonObject(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            return text.Substring(start, end - start + 1);
        }

        static string NormalizeAudience(string value, string fallback)
        {
            if (string.Equals(value, KnowledgeAudiences.StoreOwner, StringComparison.OrdinalIgnoreCase))
                return KnowledgeAudiences.StoreOwner;
            if (string.Equals(value, KnowledgeAudiences.Customer, StringComparison.OrdinalIgnoreCase))
                return KnowledgeAudiences.Customer;
            if (string.Equals(value, KnowledgeAudiences.Both, StringComparison.OrdinalIgnoreCase))
                return KnowledgeAudiences.Both;
            return string.IsNullOrWhiteSpace(fallback) ? KnowledgeAudiences.Customer : fallback;
        }

        sealed class LearnDto
        {
            public bool Skip { get; set; }
            public string SkipReason { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public string Content { get; set; }
            public string Audience { get; set; }
            public string[] Tags { get; set; }
        }
    }
}
