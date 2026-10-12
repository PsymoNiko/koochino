using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using MyShop.Core.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// آپلود فایل مدارک و عکس‌های گالری فروشگاه روی سرور.
    /// </summary>
    public class StoreDocumentUploadService
    {
        private const long MaxSizeBytes = 900 * 1024; // 900 KB

        private readonly IWebHostEnvironment _env;

        public StoreDocumentUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<(bool success, string relativeUrl, string message)> UploadAsync(int storeId, IBrowserFile file, bool isGallery)
        {
            var allowed = isGallery ? UploadAllowList.Images : UploadAllowList.ImagesAndPdf;
            var ext = UploadAllowList.Normalize(Path.GetExtension(file.Name));
            if (!UploadAllowList.IsAllowed(ext, allowed))
                return (false, null, isGallery ? "فقط فایل‌های تصویری (JPG, PNG, GIF, WebP) مجاز هستند." : "فقط تصویر یا PDF مجاز است.");

            if (file.Size > MaxSizeBytes)
                return (false, null, "حداکثر حجم فایل ۹۰۰ کیلوبایت است.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "stores", storeId.ToString());
            Directory.CreateDirectory(dir);

            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, uniqueName);

            try
            {
                await using var stream = file.OpenReadStream(MaxSizeBytes);
                var header = new byte[16];
                var read = await stream.ReadAsync(header.AsMemory(0, 16));
                if (!FileMagicBytes.MatchesExtension(ext, header.AsSpan(0, read)))
                    return (false, null, "محتوای فایل با پسوند آن هم‌خوانی ندارد.");

                await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fileStream.WriteAsync(header.AsMemory(0, read));
                await stream.CopyToAsync(fileStream);
            }
            catch (Exception)
            {
                return (false, null, "خطا در ذخیره فایل.");
            }

            var relativeUrl = $"/uploads/stores/{storeId}/{uniqueName}";
            return (true, relativeUrl, "فایل با موفقیت آپلود شد.");
        }

        public bool TryDeleteFile(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads/stores/", StringComparison.OrdinalIgnoreCase))
                return false;

            var path = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, path);
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }
}
