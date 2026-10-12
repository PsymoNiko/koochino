using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// آپلود عکس پروفایل و مدارک کاربر (کارت ملی رو/پشت) با اعتبارسنجی حجم و نوع فایل.
    /// </summary>
    public class UserDocumentUploadService
    {
        private const long MinSizeBytes = 50 * 1024;   // 50 KB
        private const long MaxSizeBytes = 1024 * 1024; // 1 MB

        private readonly IWebHostEnvironment _env;
        private readonly IUserService _userService;

        public UserDocumentUploadService(IWebHostEnvironment env, IUserService userService)
        {
            _env = env;
            _userService = userService;
        }

        public async Task<(bool success, string message)> UploadAsync(int userId, string documentType, IBrowserFile file)
        {
            var ext = UploadAllowList.Normalize(Path.GetExtension(file.Name));
            if (!UploadAllowList.IsAllowed(ext, UploadAllowList.Images))
                return (false, "فقط فایل‌های JPG و PNG مجاز هستند.");

            if (file.Size < MinSizeBytes)
                return (false, "حداقل حجم فایل ۵۰ کیلوبایت است.");
            if (file.Size > MaxSizeBytes)
                return (false, "حداکثر حجم فایل ۱ مگابایت است.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "users", userId.ToString());
            Directory.CreateDirectory(dir);

            var uniqueName = $"{SanitizeDocType(documentType)}-{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, uniqueName);

            try
            {
                await using var stream = file.OpenReadStream(MaxSizeBytes);
                var header = new byte[16];
                var read = await stream.ReadAsync(header.AsMemory(0, 16));
                if (!FileMagicBytes.MatchesExtension(ext, header.AsSpan(0, read)))
                    return (false, "محتوای فایل با پسوند آن هم‌خوانی ندارد.");

                await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fileStream.WriteAsync(header.AsMemory(0, read));
                await stream.CopyToAsync(fileStream);
            }
            catch (Exception)
            {
                return (false, "خطا در ذخیره فایل.");
            }

            var relativeUrl = $"/uploads/users/{userId}/{uniqueName}";
            var updated = await _userService.UpdateUserDocumentUrlAsync(userId, documentType, relativeUrl);
            if (!updated)
                return (false, "فایل ذخیره شد ولی به‌روزرسانی پروفایل انجام نشد.");

            return (true, "مدرک با موفقیت ارسال و ذخیره شد.");
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
