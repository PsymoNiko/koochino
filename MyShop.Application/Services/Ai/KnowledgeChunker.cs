using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Splits knowledge articles into 300–500 word chunks with a one-sentence overlap so a fact that
    /// sits on a boundary is still fully visible to the embedding model. Short FAQs stay as a single chunk.
    /// </summary>
    public static class KnowledgeChunker
    {
        public const int MinWords = 300;
        public const int TargetWords = 400;
        public const int MaxWords = 500;

        static readonly Regex SentenceSplit = new(@"(?<=[\.!\?؟。])\s+", RegexOptions.Compiled);

        public static List<string> Chunk(string title, string summary, string content)
        {
            var body = Join(title, summary, content);
            if (string.IsNullOrWhiteSpace(body)) return new List<string>();

            var sentences = SplitSentences(body);
            if (sentences.Count == 0) return new List<string>();

            var totalWords = 0;
            foreach (var s in sentences) totalWords += WordCount(s);
            if (totalWords <= MaxWords) return new List<string> { body };

            var chunks = new List<string>();
            var current = new List<string>();
            var words = 0;

            foreach (var sentence in sentences)
            {
                var wc = WordCount(sentence);
                if (current.Count > 0 && words + wc > MaxWords && words >= MinWords)
                {
                    chunks.Add(JoinSentences(current));
                    var overlap = current[current.Count - 1];
                    current.Clear();
                    current.Add(overlap);
                    words = WordCount(overlap);
                }

                current.Add(sentence);
                words += wc;

                if (words >= TargetWords && words >= MinWords && current.Count > 1)
                {
                    chunks.Add(JoinSentences(current));
                    var overlap = current[current.Count - 1];
                    current.Clear();
                    current.Add(overlap);
                    words = WordCount(overlap);
                }
            }

            if (current.Count > 0)
            {
                var tail = JoinSentences(current);
                if (chunks.Count > 0 && WordCount(tail) < 40)
                    chunks[chunks.Count - 1] = chunks[chunks.Count - 1] + " " + tail;
                else
                    chunks.Add(tail);
            }

            return chunks;
        }

        static string Join(string title, string summary, string content)
        {
            var sb = new StringBuilder();
            AppendPart(sb, title);
            AppendPart(sb, summary);
            AppendPart(sb, content);
            return sb.ToString().Trim();
        }

        static void AppendPart(StringBuilder sb, string part)
        {
            if (string.IsNullOrWhiteSpace(part)) return;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(part.Trim());
        }

        static List<string> SplitSentences(string text)
        {
            var parts = SentenceSplit.Split(text);
            var list = new List<string>();
            foreach (var part in parts)
            {
                var t = (part ?? "").Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list;
        }

        static string JoinSentences(List<string> sentences) => string.Join(" ", sentences);

        static int WordCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var count = 0;
            var inWord = false;
            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch))
                {
                    inWord = false;
                }
                else if (!inWord)
                {
                    inWord = true;
                    count++;
                }
            }
            return count;
        }
    }
}
