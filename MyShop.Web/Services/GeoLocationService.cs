using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services.Security;
using MyShop.Core.Interfaces;

namespace MyShop.Web.Services
{
    public class GeoLocationService : IGeoLocationService
    {
        private readonly IJSRuntime _js;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;
        private readonly IModuleConfigService _moduleConfig;
        private readonly ICurrentTenant _tenant;

        public GeoLocationService(
            IJSRuntime js,
            IHttpClientFactory httpFactory,
            IConfiguration config,
            IModuleConfigService moduleConfig = null,
            ICurrentTenant tenant = null)
        {
            _js = js;
            _httpFactory = httpFactory;
            _config = config;
            _moduleConfig = moduleConfig;
            _tenant = tenant;
        }

        public async Task<GeoLocationResult?> GetCurrentPositionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var obj = await _js.InvokeAsync<GeoLocationJsResult>("koochGetUserLocation");
                if (obj == null) return null;
                return new GeoLocationResult
                {
                    Lat = obj.Lat,
                    Lng = obj.Lng,
                    Accuracy = obj.Accuracy
                };
            }
            catch (JSException)
            {
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        public async Task<string> ReverseGeocodeAsync(double lat, double lng, CancellationToken cancellationToken = default)
        {
            var details = await ReverseGeocodeDetailsAsync(lat, lng, cancellationToken);
            return string.IsNullOrWhiteSpace(details?.FormattedAddress) ? null : details.FormattedAddress;
        }

        public async Task<GeoAddressDetails> ReverseGeocodeDetailsAsync(double lat, double lng, CancellationToken cancellationToken = default)
        {
            var latS = lat.ToString(CultureInfo.InvariantCulture);
            var lngS = lng.ToString(CultureInfo.InvariantCulture);
            var key = await GetNeshanKeyAsync();

            if (!string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    var client = _httpFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(8);
                    using var req = new HttpRequestMessage(HttpMethod.Get,
                        $"https://api.neshan.org/v5/reverse?lat={latS}&lng={lngS}");
                    req.Headers.TryAddWithoutValidation("Api-Key", key);
                    using var resp = await client.SendAsync(req, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                        var root = doc.RootElement;
                        var details = new GeoAddressDetails
                        {
                            FormattedAddress = GetStr(root, "formatted_address"),
                            City = FirstNonEmpty(GetStr(root, "city"), GetStr(root, "county"), GetStr(root, "village")),
                            Province = NormalizeProvinceName(FirstNonEmpty(GetStr(root, "state"), GetStr(root, "province"))),
                            Neighbourhood = GetStr(root, "neighbourhood")
                        };
                        if (!string.IsNullOrWhiteSpace(details.FormattedAddress) ||
                            !string.IsNullOrWhiteSpace(details.City) ||
                            !string.IsNullOrWhiteSpace(details.Province))
                            return details;
                    }
                }
                catch { /* fallback */ }
            }

            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(8);
                using var req = new HttpRequestMessage(HttpMethod.Get,
                    $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latS}&lon={lngS}&accept-language=fa");
                req.Headers.TryAddWithoutValidation("User-Agent", "Koochino/1.0 (store-locator)");
                using var resp = await client.SendAsync(req, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    var root = doc.RootElement;
                    JsonElement addr = default;
                    var hasAddr = root.TryGetProperty("address", out addr);
                    return new GeoAddressDetails
                    {
                        FormattedAddress = GetStr(root, "display_name"),
                        City = hasAddr
                            ? FirstNonEmpty(GetStr(addr, "city"), GetStr(addr, "town"), GetStr(addr, "village"), GetStr(addr, "county"))
                            : null,
                        Province = hasAddr
                            ? NormalizeProvinceName(FirstNonEmpty(GetStr(addr, "state"), GetStr(addr, "province"), GetStr(addr, "region")))
                            : null,
                        Neighbourhood = hasAddr ? GetStr(addr, "suburb") : null
                    };
                }
            }
            catch { }

            return new GeoAddressDetails();
        }

        public async Task<GeoLocationResult?> GeocodeAsync(string query, CancellationToken cancellationToken = default)
        {
            var q = (query ?? "").Trim();
            if (string.IsNullOrEmpty(q)) return null;

            var key = await GetNeshanKeyAsync();
            if (!string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    var client = _httpFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(8);
                    var url = $"https://api.neshan.org/v4/geocoding?address={Uri.EscapeDataString(q)}";
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.TryAddWithoutValidation("Api-Key", key);
                    using var resp = await client.SendAsync(req, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("location", out var loc) &&
                            loc.TryGetProperty("y", out var y) &&
                            loc.TryGetProperty("x", out var x) &&
                            y.TryGetDouble(out var lat) &&
                            x.TryGetDouble(out var lng))
                        {
                            return new GeoLocationResult { Lat = lat, Lng = lng };
                        }
                    }
                }
                catch { /* fallback */ }

                try
                {
                    var client = _httpFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(8);
                    // جستجو با مرکز تقریبی ایران
                    var url = $"https://api.neshan.org/v1/search?term={Uri.EscapeDataString(q)}&lat=32.4279&lng=53.6880";
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.TryAddWithoutValidation("Api-Key", key);
                    using var resp = await client.SendAsync(req, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                        if (doc.RootElement.TryGetProperty("items", out var items) &&
                            items.ValueKind == JsonValueKind.Array &&
                            items.GetArrayLength() > 0)
                        {
                            var first = items[0];
                            if (first.TryGetProperty("location", out var loc) &&
                                loc.TryGetProperty("y", out var y) &&
                                loc.TryGetProperty("x", out var x) &&
                                y.TryGetDouble(out var lat) &&
                                x.TryGetDouble(out var lng))
                            {
                                return new GeoLocationResult { Lat = lat, Lng = lng };
                            }
                        }
                    }
                }
                catch { /* fallback */ }
            }

            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(8);
                using var req = new HttpRequestMessage(HttpMethod.Get,
                    $"https://nominatim.openstreetmap.org/search?format=jsonv2&q={Uri.EscapeDataString(q + ", ایران")}&limit=1&accept-language=fa");
                req.Headers.TryAddWithoutValidation("User-Agent", "Koochino/1.0 (store-locator)");
                using var resp = await client.SendAsync(req, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var first = doc.RootElement[0];
                        if (first.TryGetProperty("lat", out var latEl) &&
                            first.TryGetProperty("lon", out var lonEl) &&
                            double.TryParse(latEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                            double.TryParse(lonEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                        {
                            return new GeoLocationResult { Lat = lat, Lng = lng };
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private async Task<string> GetNeshanKeyAsync()
            => await ModuleConfigRead.TryGetAsync(_moduleConfig, ModuleConfigRead.SiteIdOf(_tenant), "Maps", "NeshanApiKey", true)
               ?? _config["Maps:NeshanApiKey"];

        private static string GetStr(JsonElement el, string name)
            => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()?.Trim() : null;

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var v in values)
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            return null;
        }

        private static string NormalizeProvinceName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var n = name.Trim()
                .Replace("ي", "ی")
                .Replace("ك", "ک")
                .Replace("استان ", "")
                .Replace("استان", "")
                .Trim();
            return string.IsNullOrWhiteSpace(n) ? null : n;
        }

        private class GeoLocationJsResult
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
            public double? Accuracy { get; set; }
        }
    }
}
