using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using MyShop.Core.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class TicketAttachmentUploadService
    {
        private const long MaxSizeBytes = 2 * 1024 * 1024; // 2 MB

        private readonly IWebHostEnvironment _env;

        public TicketAttachmentUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<(bool success, string relativeUrl, string fileName, int fileSize, string message)> UploadAsync(int ticketId, IBrowserFile file)
        {
            if (file == null)
                return (false, null, null, 0, "فایلی انتخاب نشده است.");

            var ext = UploadAllowList.Normalize(Path.GetExtension(file.Name));
            if (!UploadAllowList.IsAllowed(ext, UploadAllowList.Documents))
                return (false, null, null, 0, "فرمت فایل مجاز نیست (تصویر، PDF، Word، ZIP).");

            if (file.Size > MaxSizeBytes)
                return (false, null, null, 0, "حداکثر حجم هر فایل ۲ مگابایت است.");

            var safeOriginal = Path.GetFileName(file.Name);
            var dir = Path.Combine(_env.WebRootPath, "uploads", "tickets", ticketId.ToString());
            Directory.CreateDirectory(dir);

            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, uniqueName);

            try
            {
                await using var stream = file.OpenReadStream(MaxSizeBytes);
                var header = new byte[16];
                var read = await stream.ReadAsync(header.AsMemory(0, 16));
                if (!FileMagicBytes.MatchesExtension(ext, header.AsSpan(0, read)))
                    return (false, null, null, 0, "محتوای فایل با پسوند آن هم‌خوانی ندارد.");

                await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fileStream.WriteAsync(header.AsMemory(0, read));
                await stream.CopyToAsync(fileStream);
            }
            catch (Exception)
            {
                return (false, null, null, 0, "خطا در ذخیره فایل.");
            }

            var relativeUrl = $"/uploads/tickets/{ticketId}/{uniqueName}";
            return (true, relativeUrl, safeOriginal, (int)file.Size, "آپلود شد.");
        }
    }
}
