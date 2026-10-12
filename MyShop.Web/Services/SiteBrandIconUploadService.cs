using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using MyShop.Application.Interfaces.Sites;
using MyShop.Core.Helpers;
using MyShop.Core.Models.Sites;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// آپلود لوگو/آیکون برند سایت و تولید خودکار سایزهای فاوآیکون، PWA و apple-touch.
    /// </summary>
    public class SiteBrandIconUploadService
    {
        public const long MaxSizeBytes = 2 * 1024 * 1024;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IWebHostEnvironment _env;
        private readonly ISiteService _sites;

        public SiteBrandIconUploadService(IWebHostEnvironment env, ISiteService sites)
        {
            _env = env;
            _sites = sites;
        }

        public async Task<(bool ok, SiteBrandCopy brand, string message)> UploadAndApplyAsync(int siteId, IBrowserFile file)
        {
            if (siteId <= 0)
                return (false, null, "ابتدا سایت را ذخیره کنید، سپس آیکون آپلود کنید.");

            var site = await _sites.GetByIdAsync(siteId);
            if (site == null)
                return (false, null, "سایت یافت نشد.");

            var ext = UploadAllowList.Normalize(Path.GetExtension(file.Name));
            if (!UploadAllowList.IsAllowed(ext, UploadAllowList.Images))
                return (false, null, "فقط تصویر PNG، JPG یا WebP مجاز است.");

            if (file.Size > MaxSizeBytes)
                return (false, null, "حداکثر حجم فایل ۲ مگابایت است.");

            var code = string.IsNullOrWhiteSpace(site.Code) ? siteId.ToString() : site.Code.Trim().ToLowerInvariant();
            var relDir = Path.Combine("uploads", "sites", code).Replace('\\', '/');
            var absDir = Path.Combine(_env.WebRootPath, "uploads", "sites", code);
            Directory.CreateDirectory(absDir);

            await using var input = file.OpenReadStream(MaxSizeBytes);
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var headerLen = Math.Min(16, bytes.Length);
            if (!FileMagicBytes.MatchesExtension(ext, bytes.AsSpan(0, headerLen)))
                return (false, null, "محتوای فایل با پسوند آن هم‌خوانی ندارد.");

            try
            {
                using var image = Image.Load(bytes);

                // لوگو اصلی (حداکثر ۵۱۲ در بزرگ‌ترین ضلع، حفظ نسبت)
                var logoPath = Path.Combine(absDir, "logo.png");
                await SaveResizedSquareOrFitAsync(image, logoPath, 512, fitContain: true);

                var favPath = Path.Combine(absDir, "favicon-32.png");
                await SaveResizedSquareAsync(image, favPath, 32);

                var applePath = Path.Combine(absDir, "apple-touch-180.png");
                await SaveResizedSquareAsync(image, applePath, 180);

                var pwa192 = Path.Combine(absDir, "pwa-192.png");
                await SaveResizedSquareAsync(image, pwa192, 192);

                var pwa512 = Path.Combine(absDir, "pwa-512.png");
                await SaveResizedSquareAsync(image, pwa512, 512);

                // نسخه cache-bust با timestamp
                var v = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string Url(string fileName) => $"/{relDir}/{fileName}?v={v}";

                var brand = DeserializeBrand(site.BrandJson) ?? new SiteBrandCopy();
                if (string.IsNullOrWhiteSpace(brand.SiteName))
                    brand.SiteName = site.Name;
                if (string.IsNullOrWhiteSpace(brand.PageTitle))
                    brand.PageTitle = site.Name;

                brand.LogoUrl = Url("logo.png");
                brand.FaviconUrl = Url("favicon-32.png");
                brand.AppleTouchIconUrl = Url("apple-touch-180.png");
                brand.PwaIcon192Url = Url("pwa-192.png");
                brand.PwaIcon512Url = Url("pwa-512.png");

                site.BrandJson = JsonSerializer.Serialize(brand, JsonOpts);
                await _sites.UpsertAsync(site);

                return (true, brand, "آیکون‌ها با موفقیت ساخته و ذخیره شدند.");
            }
            catch (Exception ex)
            {
                return (false, null, "خطا در پردازش تصویر: " + ex.Message);
            }
        }

        static SiteBrandCopy DeserializeBrand(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<SiteBrandCopy>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        static async Task SaveResizedSquareAsync(Image source, string path, int size)
        {
            using var clone = source.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(size, size),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center
                });
            });
            await clone.SaveAsPngAsync(path, new PngEncoder { CompressionLevel = PngCompressionLevel.BestSpeed });
        }

        static async Task SaveResizedSquareOrFitAsync(Image source, string path, int maxSide, bool fitContain)
        {
            using var clone = source.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxSide, maxSide),
                    Mode = fitContain ? ResizeMode.Max : ResizeMode.Crop,
                    Position = AnchorPositionMode.Center
                });
            });
            await clone.SaveAsPngAsync(path, new PngEncoder { CompressionLevel = PngCompressionLevel.BestSpeed });
        }
    }
}
