using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyShop.Application.Services.Ai
{
    /// <summary>
    /// Arvan AIaaS (and similar) bind one model per gateway URL. A GPT chat URL cannot serve /embeddings.
    /// </summary>
    public static class AiGatewayProfile
    {
        static readonly Regex ArvanGateway = new(
            @"/gateway/models/(?<model>[^/]+)/(?<token>[^/]+)/v1/?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public const string ChatOnlyEmbedMessage =
            "این گیت‌وی برای مدل چت ساخته شده، نه بردار معنایی. در پنل آروان یک API جدا برای مدل embedding بسازید، آدرس کامل آن را در تنظیمات ماژول به‌عنوان «آدرس سرویس بردار» ذخیره کنید، بعد تست بردار را دوباره اجرا کنید. تا آن موقع جستجوی معنایی را خاموش بگذارید؛ بازیابی کلیدواژه‌ای به کار خود ادامه می‌دهد.";

        public static bool TryParseArvan(string baseUrl, out string model, out string token)
        {
            model = null;
            token = null;
            if (string.IsNullOrWhiteSpace(baseUrl)) return false;
            var m = ArvanGateway.Match(baseUrl.Trim().TrimEnd('/'));
            if (!m.Success) return false;
            model = Uri.UnescapeDataString(m.Groups["model"].Value);
            token = m.Groups["token"].Value;
            return !string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(token);
        }

        public static bool IsEmbeddingModelName(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) return false;
            return model.IndexOf("embed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>True when this URL is an Arvan (or lookalike) gateway pinned to a chat model.</summary>
        public static bool IsChatOnlyGateway(string baseUrl)
            => TryParseArvan(baseUrl, out var model, out _) && !IsEmbeddingModelName(model);

        public static bool IsEmbeddingGateway(string baseUrl)
            => TryParseArvan(baseUrl, out var model, out _) && IsEmbeddingModelName(model);

        public static string ExplainEmbedError(string providerError, string embedBaseUrl)
        {
            if (IsChatOnlyGateway(embedBaseUrl))
                return ChatOnlyEmbedMessage;

            var err = providerError ?? "";
            if (err.IndexOf("401", StringComparison.Ordinal) >= 0
                || err.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "گیت‌وی بردار درخواست را Unauthorized کرد. مدل Embedding-3-Small درست است؛ مشکل کلید است. "
                    + "همان ApiKey چت را می‌فرستیم و توکن داخل آدرس را جداگانه امتحان می‌کنیم. "
                    + "اگر باز هم 401 بود، در پنل آروان API مربوط به Embedding را فعال کنید و آدرس کامل گیت‌وی را دوباره در «آدرس سرویس بردار» ذخیره کنید. "
                    + "پاسخ سرویس: " + Trunc(err, 160);
            }

            if (err.IndexOf("424", StringComparison.Ordinal) >= 0
                || err.IndexOf("embedding failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ChatOnlyEmbedMessage + " پاسخ سرویس: " + Trunc(err, 180);
            }

            return string.IsNullOrWhiteSpace(err) ? ChatOnlyEmbedMessage : err;
        }

        /// <summary>
        /// Auth to try for /embeddings. Chat already works with the stored ApiKey, so that goes first.
        /// The path token is tried next; a final null means "no Authorization header" (Arvan path-only).
        /// </summary>
        public static IReadOnlyList<string> EmbeddingAuthAttempts(string baseUrl, string configuredApiKey)
        {
            var list = new List<string>();
            void add(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                if (list.Any(x => string.Equals(x, value, StringComparison.Ordinal))) return;
                list.Add(value);
            }

            add(configuredApiKey);
            if (TryParseArvan(baseUrl, out _, out var token))
                add(token);
            list.Add(null);
            return list;
        }

        public static bool OmitEmbeddingDimensions(string baseUrl)
            => TryParseArvan(baseUrl, out _, out _);

        public static string Redact(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            var m = ArvanGateway.Match(url.Trim().TrimEnd('/'));
            if (!m.Success) return url.Trim();
            return url.Trim().Substring(0, m.Groups["token"].Index) + "***" + url.Trim().Substring(m.Groups["token"].Index + m.Groups["token"].Length);
        }

        static string Trunc(string s, int n)
        {
            s = (s ?? "").Trim();
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }
    }
}
