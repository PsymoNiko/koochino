using System.ComponentModel.DataAnnotations;

namespace MyShop.Core.Models
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(256)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "نام الزامی است")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام خانوادگی الزامی است")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(10)]
        public string NationalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد")]
        public string Password { get; set; } = string.Empty;

        /// <summary>1=Admin, 2=StoreOwner, 3=Customer — ثبت‌نام عمومی: 2 یا 3؛ Admin فقط توسط ادمین.</summary>
        public int RoleId { get; set; }

        /// <summary>کد دعوت معرف (اختیاری) — از query ?ref=</summary>
        [StringLength(32)]
        public string InviteCode { get; set; }

        /// <summary>سایت مبدأ — فقط از سرور (Host) ست می‌شود؛ کلاینت را نادیده بگیرید.</summary>
        public int? SiteId { get; set; }
    }
}
