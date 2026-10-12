using System;
using System.Text.RegularExpressions;

namespace MyShop.Core.Helpers
{
    /// <summary>
    /// Minimal HTML sanitizer for MarkupString rendering (announcements / CMS).
    /// Strips scripts, event handlers, and dangerous URLs without extra packages.
    /// </summary>
    public static class HtmlSanitize
    {
        static readonly Regex DangerousTags = new(
            @"<\s*/?\s*(script|iframe|object|embed|form|link|meta|base|svg|math|template|applet|style)(\s[^>]*)?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex EventHandlers = new(
            @"\s+on[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex DangerousUrls = new(
            @"\s*(href|src|xlink:href|action|formaction|srcdoc)\s*=\s*(""|')\s*(javascript|vbscript|data)\s*:",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex DangerousUrlsUnquoted = new(
            @"\s*(href|src|xlink:href|action|formaction)\s*=\s*(javascript|vbscript|data)\s*:",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex StyleExpression = new(
            @"\s+style\s*=\s*(""[^""]*""|'[^']*')",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html ?? string.Empty;

            var result = html.Replace("\0", string.Empty);
            result = DangerousTags.Replace(result, string.Empty);
            result = EventHandlers.Replace(result, string.Empty);
            result = DangerousUrls.Replace(result, " $1=$2#blocked:");
            result = DangerousUrlsUnquoted.Replace(result, " $1='#blocked:'");
            result = StyleExpression.Replace(result, m =>
            {
                var v = m.Value;
                if (v.IndexOf("expression", StringComparison.OrdinalIgnoreCase) >= 0
                    || v.IndexOf("javascript", StringComparison.OrdinalIgnoreCase) >= 0)
                    return string.Empty;
                return v;
            });
            return result;
        }
    }
}
