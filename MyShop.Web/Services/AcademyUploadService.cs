using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using MyShop.Core.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class AcademyUploadService
    {
        private const long MaxSizeBytes = 900 * 1024;
        private readonly IWebHostEnvironment _env;

        public AcademyUploadService(IWebHostEnvironment env) => _env = env;

        public Task<(bool success, string relativeUrl, string message)> UploadLocationAsync(int locationId, IBrowserFile file)
            => UploadAsync("locations", locationId, file);

        public Task<(bool success, string relativeUrl, string message)> UploadCategoryAsync(int categoryId, IBrowserFile file)
            => UploadAsync("categories", categoryId, file);

        public Task<(bool success, string relativeUrl, string message)> UploadActivityAsync(int activityId, IBrowserFile file)
            => UploadAsync("activities", activityId, file);

        public Task<(bool success, string relativeUrl, string message)> UploadCourseAsync(int courseId, IBrowserFile file)
            => UploadAsync("courses", courseId, file);

        private async Task<(bool success, string relativeUrl, string message)> UploadAsync(string folder, int entityId, IBrowserFile file)
        {
            var ext = UploadAllowList.Normalize(Path.GetExtension(file.Name));
            if (!UploadAllowList.IsAllowed(ext, UploadAllowList.Images))
                return (false, null, "فقط فایل‌های تصویری (JPG, PNG, GIF, WebP) مجاز هستند.");
            if (file.Size > MaxSizeBytes)
                return (false, null, "حداکثر حجم فایل ۹۰۰ کیلوبایت است.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "academy", folder, entityId.ToString());
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

                await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fs.WriteAsync(header.AsMemory(0, read));
                await stream.CopyToAsync(fs);
            }
            catch
            {
                return (false, null, "خطا در ذخیره فایل.");
            }

            return (true, $"/uploads/academy/{folder}/{entityId}/{uniqueName}", "فایل با موفقیت آپلود شد.");
        }
    }
}
