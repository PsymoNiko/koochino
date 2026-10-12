using QRCoder;

namespace MyShop.Web.Services
{
    /// <summary>
    /// تولید QR کد به صورت تصویر Base64 برای نمایش در صفحه.
    /// </summary>
    public static class QrCodeService
    {
        /// <summary>
        /// متن یا URL را به QR کد تبدیل می‌کند و خروجی را به صورت Data URL برمی‌گرداند (برای استفاده در src تصویر).
        /// </summary>
        public static string ToDataUrl(string content, int pixelsPerModule = 4)
        {
            if (string.IsNullOrEmpty(content))
                return "";
            try
            {
                using var generator = new QRCodeGenerator();
                using var qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                var qr = new Base64QRCode(qrData);
                var base64 = qr.GetGraphic(pixelsPerModule);
                return "data:image/png;base64," + base64;
            }
            catch
            {
                return "";
            }
        }
    }
}
