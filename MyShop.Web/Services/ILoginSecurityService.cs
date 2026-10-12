namespace MyShop.Web.Services
{
    /// <summary>
    /// امنیت ورود: ردیابی تلاش‌های ناموفق و نمایش کپچا بعد از ۲ بار خطا.
    /// </summary>
    public interface ILoginSecurityService
    {
        /// <summary>
        /// آیا برای این نوع ورود (Admin/Customer/StoreOwner) باید کپچا نمایش داده شود؟
        /// بعد از ۲ بار ورود ناموفق در بازهٔ زمانی (مثلاً ۱۵ دقیقه) true برمی‌گرداند.
        /// </summary>
        bool IsTemporarilyBlocked(string loginType);
        bool RequiresCaptcha(string loginType);

        /// <summary>
        /// ثبت یک ورود ناموفق برای نوع فعلی (با IP فعلی).
        /// </summary>
        void RecordFailedLogin(string loginType);

        /// <summary>
        /// پاک کردن شمارشگر تلاش‌های ناموفق بعد از ورود موفق.
        /// </summary>
        void ClearFailedLogins(string loginType);

        /// <summary>
        /// ساخت یک سوال کپچا (ریاضی ساده). مقدار برگشتی را در ValidateCaptcha استفاده کنید.
        /// </summary>
        (string questionText, string captchaId) GenerateCaptcha();

        /// <summary>
        /// بررسی پاسخ کپچا. در صورت معتبر بودن، رکورد کپچا مصرف می‌شود.
        /// </summary>
        bool ValidateCaptcha(string captchaId, string userAnswer);
    }
}
