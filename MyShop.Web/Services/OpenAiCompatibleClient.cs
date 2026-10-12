using Microsoft.Extensions.Configuration;
using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services.Ai;
using MyShop.Application.Services.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Ai;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// Talks to any OpenAI-compatible chat completions endpoint (OpenAI, GapGPT, Liara, ...).
    /// Per-site key, base url and model come from encrypted module settings with appsettings fallback.
    /// </summary>
    public class OpenAiCompatibleClient : IAiCompletionClient, IAiEmbeddingClient
    {
        const string ModuleKey = "AiAssistant";

        static readonly object DimensionGate = new();
        static bool _omitDimensions;

        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly IModuleConfigService _moduleConfig;
        private readonly ICurrentTenant _tenant;

        public OpenAiCompatibleClient(
            HttpClient http,
            IConfiguration configuration,
            IModuleConfigService moduleConfig = null,
            ICurrentTenant tenant = null)
        {
            _http = http;
            _configuration = configuration;
            _moduleConfig = moduleConfig;
            _tenant = tenant;
        }

        public async Task<bool> IsConfiguredAsync()
            => !string.IsNullOrWhiteSpace(await ResolveApiKeyAsync(null));

        public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
                return new AiCompletionResult(false, null, "پیام خالی است.");

            var apiKey = await ResolveApiKeyAsync(null);
            if (string.IsNullOrWhiteSpace(apiKey))
                return new AiCompletionResult(false, null, "کلید هوش مصنوعی تنظیم نشده است.");

            var baseUrl = await ResolveBaseUrlAsync(null);
            var model = await ResolveModelAsync(null);
            var payload = BuildPayload(request, model);
            var sw = Stopwatch.StartNew();

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/chat/completions");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(httpRequest);
                var body = await response.Content.ReadAsStringAsync();
                sw.Stop();

                if (!response.IsSuccessStatusCode)
                    return new AiCompletionResult(false, null,
                        $"HTTP {(int)response.StatusCode}: {Shorten(ExtractProviderError(body) ?? body, 400)}",
                        Model: model, LatencyMs: (int)sw.ElapsedMilliseconds);

                return Parse(body, model, (int)sw.ElapsedMilliseconds);
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                return new AiCompletionResult(false, null,
                    "پاسخ سرویس هوش مصنوعی در زمان مجاز نرسید (timeout).",
                    Model: model, LatencyMs: (int)sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new AiCompletionResult(false, null, Shorten(ex.Message, 400),
                    Model: model, LatencyMs: (int)sw.ElapsedMilliseconds);
            }
        }

        public Task<AiEmbeddingResult> EmbedAsync(string input, int? siteId = null)
            => EmbedOneAsync(input, siteId);

        public async Task<IReadOnlyList<AiEmbeddingResult>> EmbedBatchAsync(IReadOnlyList<string> inputs, int? siteId = null)
        {
            var list = (inputs ?? Array.Empty<string>())
                .Select(s => (s ?? "").Trim())
                .Where(s => s.Length > 0)
                .ToList();
            if (list.Count == 0)
                return Array.Empty<AiEmbeddingResult>();
            if (list.Count == 1)
                return new[] { await EmbedOneAsync(list[0], siteId) };

            var apiKey = await ResolveApiKeyAsync(siteId);
            var model = await ResolveEmbeddingModelAsync(siteId);
            var baseUrl = await ResolveEmbeddingBaseUrlAsync(siteId);
            if (AiGatewayProfile.IsChatOnlyGateway(baseUrl))
                return list.Select(_ => FailEmbed(AiGatewayProfile.ChatOnlyEmbedMessage, model)).ToList();
            if (string.IsNullOrWhiteSpace(apiKey) && !AiGatewayProfile.TryParseArvan(baseUrl, out _, out _))
                return list.Select(_ => FailEmbed("کلید هوش مصنوعی تنظیم نشده است.", model)).ToList();
            var withDimensions = !_omitDimensions;
            var result = await PostEmbeddingsAsync(baseUrl, apiKey, model, list, withDimensions);
            if (!result.Ok && withDimensions && LooksLikeDimensionRejection(result.Error))
            {
                lock (DimensionGate) _omitDimensions = true;
                result = await PostEmbeddingsAsync(baseUrl, apiKey, model, list, includeDimensions: false);
            }

            if (!result.Ok)
                return list.Select(_ => FailEmbed(result.Error, model, result.LatencyMs)).ToList();

            return result.Items;
        }

        async Task<AiEmbeddingResult> EmbedOneAsync(string input, int? siteId)
        {
            if (string.IsNullOrWhiteSpace(input))
                return FailEmbed("متن خالی است.", await ResolveEmbeddingModelAsync(siteId));

            var apiKey = await ResolveApiKeyAsync(siteId);
            var model = await ResolveEmbeddingModelAsync(siteId);
            var baseUrl = await ResolveEmbeddingBaseUrlAsync(siteId);
            if (AiGatewayProfile.IsChatOnlyGateway(baseUrl))
                return FailEmbed(AiGatewayProfile.ChatOnlyEmbedMessage, model);
            if (string.IsNullOrWhiteSpace(apiKey) && !AiGatewayProfile.TryParseArvan(baseUrl, out _, out _))
                return FailEmbed("کلید هوش مصنوعی تنظیم نشده است.", model);
            var inputs = new List<string> { input.Trim() };
            var withDimensions = !_omitDimensions;
            var result = await PostEmbeddingsAsync(baseUrl, apiKey, model, inputs, withDimensions);
            if (!result.Ok && withDimensions && LooksLikeDimensionRejection(result.Error))
            {
                lock (DimensionGate) _omitDimensions = true;
                result = await PostEmbeddingsAsync(baseUrl, apiKey, model, inputs, includeDimensions: false);
            }

            if (!result.Ok)
                return FailEmbed(result.Error, model, result.LatencyMs);
            return result.Items.Count > 0
                ? result.Items[0]
                : FailEmbed("پاسخ خالی از سرویس دریافت شد.", model, result.LatencyMs);
        }

        async Task<(bool Ok, string Error, int LatencyMs, List<AiEmbeddingResult> Items)> PostEmbeddingsAsync(
            string baseUrl, string apiKey, string model, List<string> inputs, bool includeDimensions)
        {
            if (AiGatewayProfile.OmitEmbeddingDimensions(baseUrl))
                includeDimensions = false;

            object payload = includeDimensions
                ? new { model, input = inputs, dimensions = AiEmbeddingDefaults.Dimensions }
                : new { model, input = inputs };

            var json = JsonSerializer.Serialize(payload);
            var sw = Stopwatch.StartNew();
            string lastError = null;
            try
            {
                foreach (var bearer in AiGatewayProfile.EmbeddingAuthAttempts(baseUrl, apiKey))
                {
                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/embeddings");
                    if (!string.IsNullOrWhiteSpace(bearer))
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                    httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var response = await _http.SendAsync(httpRequest);
                    var body = await response.Content.ReadAsStringAsync();
                    var latency = (int)sw.ElapsedMilliseconds;

                    if (response.IsSuccessStatusCode)
                    {
                        sw.Stop();
                        var parsed = ParseEmbeddings(body, model, latency);
                        if (parsed.Count == 0)
                            return (false, "پاسخ سرویس بردار ساختار مورد انتظار را ندارد: " + Shorten(body, 300), latency, null);
                        return (true, null, latency, parsed);
                    }

                    lastError = $"HTTP {(int)response.StatusCode}: {Shorten(ExtractProviderError(body) ?? body, 400)}";
                    var status = (int)response.StatusCode;
                    if (status != 401 && status != 403)
                    {
                        sw.Stop();
                        return (false, AiGatewayProfile.ExplainEmbedError(lastError, baseUrl), latency, null);
                    }
                }

                sw.Stop();
                return (false, AiGatewayProfile.ExplainEmbedError(lastError ?? "HTTP 401: unauthorized", baseUrl),
                    (int)sw.ElapsedMilliseconds, null);
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                return (false, "پاسخ سرویس بردار در زمان مجاز نرسید (timeout).", (int)sw.ElapsedMilliseconds, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, Shorten(ex.Message, 400), (int)sw.ElapsedMilliseconds, null);
            }
        }

        static List<AiEmbeddingResult> ParseEmbeddings(string body, string model, int latencyMs)
        {
            var items = new List<AiEmbeddingResult>();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return items;

            var tokens = 0;
            var usedModel = root.TryGetProperty("model", out var modelEl) ? modelEl.GetString() : model;
            if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
            {
                tokens = ReadInt(usage, "prompt_tokens");
                if (tokens <= 0) tokens = ReadInt(usage, "total_tokens");
            }

            var byIndex = new SortedDictionary<int, float[]>();
            foreach (var row in data.EnumerateArray())
            {
                if (!row.TryGetProperty("embedding", out var embEl)) continue;
                var vector = ReadVector(embEl);
                if (vector.Length == 0) continue;
                var index = row.TryGetProperty("index", out var idxEl) && idxEl.TryGetInt32(out var i) ? i : byIndex.Count;
                byIndex[index] = vector;
            }

            var first = true;
            foreach (var pair in byIndex)
            {
                items.Add(new AiEmbeddingResult(true, pair.Value, pair.Value.Length,
                    first ? tokens : 0, usedModel ?? model, null, latencyMs));
                first = false;
            }
            return items;
        }

        static float[] ReadVector(JsonElement embEl)
        {
            if (embEl.ValueKind == JsonValueKind.Array)
            {
                var list = new List<float>(embEl.GetArrayLength());
                foreach (var n in embEl.EnumerateArray())
                {
                    if (n.TryGetSingle(out var f)) list.Add(f);
                    else if (n.TryGetDouble(out var d)) list.Add((float)d);
                }
                return list.ToArray();
            }

            if (embEl.ValueKind == JsonValueKind.String)
            {
                try
                {
                    var raw = Convert.FromBase64String(embEl.GetString() ?? "");
                    return EmbeddingMath.FromBytes(raw);
                }
                catch
                {
                    return Array.Empty<float>();
                }
            }

            return Array.Empty<float>();
        }

        static bool LooksLikeDimensionRejection(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return false;
            return error.IndexOf("dimension", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("400", StringComparison.Ordinal) >= 0;
        }

        static AiEmbeddingResult FailEmbed(string error, string model, int latencyMs = 0)
            => new(false, Array.Empty<float>(), 0, 0, model, error, latencyMs);

        static string BuildPayload(AiCompletionRequest request, string model)
        {
            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
                messages.Add(new { role = "system", content = request.SystemPrompt });

            foreach (var turn in request.History ?? Array.Empty<AiChatMessage>())
            {
                if (turn == null || string.IsNullOrWhiteSpace(turn.Content)) continue;
                var role = string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? "assistant"
                    : "user";
                messages.Add(new { role, content = turn.Content });
            }

            messages.Add(new { role = "user", content = request.UserMessage });

            return JsonSerializer.Serialize(new
            {
                model,
                messages,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens <= 0 ? 700 : request.MaxTokens
            });
        }

        static AiCompletionResult Parse(string body, string model, int latencyMs)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (!root.TryGetProperty("choices", out var choices) ||
                    choices.ValueKind != JsonValueKind.Array ||
                    choices.GetArrayLength() == 0)
                {
                    return new AiCompletionResult(false, null,
                        "پاسخ سرویس ساختار مورد انتظار را ندارد: " + Shorten(body, 300),
                        Model: model, LatencyMs: latencyMs);
                }

                var text = choices[0].TryGetProperty("message", out var message) &&
                           message.TryGetProperty("content", out var content)
                    ? content.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(text))
                    return new AiCompletionResult(false, null, "پاسخ خالی از سرویس دریافت شد.",
                        Model: model, LatencyMs: latencyMs);

                var promptTokens = 0;
                var completionTokens = 0;
                var usedModel = root.TryGetProperty("model", out var modelEl) ? modelEl.GetString() : model;
                if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                {
                    promptTokens = ReadInt(usage, "prompt_tokens");
                    completionTokens = ReadInt(usage, "completion_tokens");
                }

                return new AiCompletionResult(true, text.Trim(), null,
                    promptTokens, completionTokens, usedModel ?? model, latencyMs);
            }
            catch (Exception ex)
            {
                return new AiCompletionResult(false, null,
                    "خواندن پاسخ سرویس ناموفق بود: " + Shorten(ex.Message, 200),
                    Model: model, LatencyMs: latencyMs);
            }
        }

        static int ReadInt(JsonElement parent, string name)
            => parent.TryGetProperty(name, out var el) && el.TryGetInt32(out var v) ? v : 0;

        static string ExtractProviderError(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.ValueKind == JsonValueKind.String) return error.GetString();
                    if (error.TryGetProperty("message", out var msg)) return msg.GetString();
                }
            }
            catch { }
            return null;
        }

        static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        async Task<string> ResolveApiKeyAsync(int? siteId)
        {
            var resolved = siteId is > 0 ? siteId : ModuleConfigRead.SiteIdOf(_tenant);
            return await ModuleConfigRead.TryGetAsync(_moduleConfig, resolved, ModuleKey, "ApiKey", true)
                   ?? NullIfBlank(_configuration["Ai:ApiKey"]);
        }

        async Task<string> ResolveBaseUrlAsync(int? siteId)
        {
            var resolved = siteId is > 0 ? siteId : ModuleConfigRead.SiteIdOf(_tenant);
            var url = await ModuleConfigRead.TryGetAsync(_moduleConfig, resolved, ModuleKey, "BaseUrl", false)
                      ?? NullIfBlank(_configuration["Ai:BaseUrl"])
                      ?? "https://api.openai.com/v1";
            return url.TrimEnd('/');
        }

        async Task<string> ResolveEmbeddingBaseUrlAsync(int? siteId)
        {
            var resolved = siteId is > 0 ? siteId : ModuleConfigRead.SiteIdOf(_tenant);
            var url = await ModuleConfigRead.TryGetAsync(_moduleConfig, resolved, ModuleKey, "EmbeddingBaseUrl", false)
                      ?? NullIfBlank(_configuration["Ai:EmbeddingBaseUrl"]);
            if (!string.IsNullOrWhiteSpace(url))
                return url.Trim().TrimEnd('/');
            return await ResolveBaseUrlAsync(siteId);
        }

        async Task<string> ResolveModelAsync(int? siteId)
        {
            var resolved = siteId is > 0 ? siteId : ModuleConfigRead.SiteIdOf(_tenant);
            return await ModuleConfigRead.TryGetAsync(_moduleConfig, resolved, ModuleKey, "Model", false)
                   ?? NullIfBlank(_configuration["Ai:Model"])
                   ?? "gpt-4o-mini";
        }

        public async Task<string> ResolveEmbeddingModelAsync(int? siteId = null)
        {
            var resolved = siteId is > 0 ? siteId : ModuleConfigRead.SiteIdOf(_tenant);
            return await ModuleConfigRead.TryGetAsync(_moduleConfig, resolved, ModuleKey, "EmbeddingModel", false)
                   ?? NullIfBlank(_configuration["Ai:EmbeddingModel"])
                   ?? AiEmbeddingDefaults.Model;
        }

        /// <summary>Base url and models without the secret, for the admin diagnostics page.</summary>
        public async Task<(string BaseUrl, string Model, string EmbeddingModel, bool HasKey, string EmbeddingBaseUrl)> DescribeAsync()
        {
            var chat = await ResolveBaseUrlAsync(null);
            var embed = await ResolveEmbeddingBaseUrlAsync(null);
            return (AiGatewayProfile.Redact(chat), await ResolveModelAsync(null),
                await ResolveEmbeddingModelAsync(null), await IsConfiguredAsync(),
                AiGatewayProfile.Redact(embed));
        }

        static string NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
