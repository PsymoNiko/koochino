using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using MyShop.Core.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// آپلود تصویر کارت ملی و سلفی برای فرآیند احراز هویت مغازه‌دار.
    /// </summary>
    public class IdentityDocumentUploadService
    {
        private const long DefaultMaxSizeBytes = 5 * 1024 * 1024; // 5 MB — پیش‌فرض وقتی سقفی از تنظیمات داده نشود

        private readonly IWebHostEnvironment _env;

        public IdentityDocumentUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<(bool success, string relativeUrl, string message)> UploadAsync(int userId, string documentType, IBrowserFile file, int? maxSizeMB = null)
        {
            var maxSizeBytes = maxSizeMB.HasValue && maxSizeMB.Value > 0 ? (long)maxSizeMB.Value * 1024 * 1024 : DefaultMaxSizeBytes;

            if (file.Size > maxSizeBytes)
                return (false, null, $"حداکثر حجم فایل {maxSizeBytes / (1024 * 1024)} مگابایت است.");

            byte[] bytes;
            try
            {
                await using var stream = file.OpenReadStream(maxSizeBytes);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            catch (Exception ex)
            {
                return (false, null, $"خطا در خواندن فایل ({ex.GetType().Name}): {ex.Message}");
            }

            return await UploadBytesAsync(userId, documentType, bytes, file.Name, maxSizeMB);
        }

        /// <summary>
        /// آپلود از روی بایت‌های از قبل خوانده‌شده (بدون نیاز به IBrowserFile) — برای جلوگیری از خطای
        /// «ارجاع فایل نامعتبر شده» وقتی بین انتخاب عکس و تایید نهایی، مدار Blazor قطع/وصل شده باشد.
        /// </summary>
        public async Task<(bool success, string relativeUrl, string message)> UploadBytesAsync(int userId, string documentType, byte[] bytes, string fileName, int? maxSizeMB = null)
        {
            var maxSizeBytes = maxSizeMB.HasValue && maxSizeMB.Value > 0 ? (long)maxSizeMB.Value * 1024 * 1024 : DefaultMaxSizeBytes;

            var ext = UploadAllowList.Normalize(Path.GetExtension(fileName));
            if (!UploadAllowList.IsAllowed(ext, UploadAllowList.Images))
                return (false, null, "فقط فایل‌های JPG و PNG مجاز هستند.");

            if (bytes == null || bytes.Length == 0)
                return (false, null, "فایل انتخابی خالی است.");

            if (bytes.Length > maxSizeBytes)
                return (false, null, $"حداکثر حجم فایل {maxSizeBytes / (1024 * 1024)} مگابایت است.");

            if (!FileMagicBytes.MatchesExtension(ext, bytes.AsSpan(0, Math.Min(16, bytes.Length))))
                return (false, null, "محتوای فایل با پسوند آن هم‌خوانی ندارد.");

            if (string.IsNullOrEmpty(_env.WebRootPath))
                return (false, null, "خطا در ذخیره فایل: مسیر wwwroot روی سرور تنظیم نشده است.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "identity", userId.ToString());
            var uniqueName = $"{SanitizeDocType(documentType)}-{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, uniqueName);

            try
            {
                Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(fullPath, bytes);
            }
            catch (Exception ex)
            {
                return (false, null, $"خطا در ذخیره فایل ({ex.GetType().Name}): {ex.Message}");
            }

            var relativeUrl = $"/uploads/identity/{userId}/{uniqueName}";
            return (true, relativeUrl, "فایل با موفقیت آپلود شد.");
        }

        static string SanitizeDocType(string documentType)
        {
            var t = (documentType ?? "doc").Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                t = t.Replace(c, '_');
            return string.IsNullOrWhiteSpace(t) ? "doc" : t;
        }
    }
}
