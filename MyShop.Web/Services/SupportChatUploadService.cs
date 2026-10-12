using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using MyShop.Core.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class SupportChatUploadService
    {
        private readonly IWebHostEnvironment _env;

        public SupportChatUploadService(IWebHostEnvironment env) => _env = env;

        public async Task<(bool success, string relativeUrl, string fileName, int fileSize, string messageType, string message)> UploadAsync(
            Guid chatId, IBrowserFile file, int maxUploadMb = 2, string allowedExtensionsCsv = null)
        {
            if (file == null)
                return (false, null, null, 0, null, "فایلی انتخاب نشده است.");

            var allowed = UploadAllowList.IntersectWithHardList(allowedExtensionsCsv, UploadAllowList.Documents);
            var ext = UploadAllowList.Normalize(Path.GetExtension(file.Name));
            if (!UploadAllowList.IsAllowed(ext, allowed))
                return (false, null, null, 0, null, "فرمت فایل مجاز نیست.");

            var maxBytes = Math.Max(1, maxUploadMb) * 1024L * 1024L;
            if (file.Size > maxBytes)
                return (false, null, null, 0, null, $"حداکثر حجم هر فایل {maxUploadMb} مگابایت است.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "support", chatId.ToString("N"));
            Directory.CreateDirectory(dir);
            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, uniqueName);
            var safeOriginal = Path.GetFileName(file.Name);

            try
            {
                await using var stream = file.OpenReadStream(maxBytes);
                var header = new byte[16];
                var read = await stream.ReadAsync(header.AsMemory(0, 16));
                if (!FileMagicBytes.MatchesExtension(ext, header.AsSpan(0, read)))
                    return (false, null, null, 0, null, "محتوای فایل با پسوند آن هم‌خوان نیست.");

                await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fileStream.WriteAsync(header.AsMemory(0, read));
                await stream.CopyToAsync(fileStream);
            }
            catch
            {
                return (false, null, null, 0, null, "خطا در ذخیره فایل.");
            }

            var relativeUrl = $"/uploads/support/{chatId:N}/{uniqueName}";
            var isImage = UploadAllowList.Images.Contains(ext);
            return (true, relativeUrl, safeOriginal, (int)file.Size, isImage ? "Image" : "File", "ok");
        }
    }
}
