using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>بازیابی رمز عبور با کد پیامکی (اعتبار ۵ دقیقه).</summary>
    public interface IPasswordResetService
    {
        /// <summary>ارسال کد تعویض رمز به شماره موبایل کاربر با نقش مشخص.</summary>
        Task<(bool ok, string message)> SendResetCodeAsync(string phone, int roleId);

        /// <summary>تأیید کد و تنظیم رمز جدید.</summary>
        Task<(bool ok, string message)> ResetPasswordAsync(string phone, int roleId, string code, string newPassword);
    }
}
