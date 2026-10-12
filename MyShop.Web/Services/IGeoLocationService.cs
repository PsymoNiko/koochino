using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// دریافت موقعیت جغرافیایی کاربر (GPS) از مرورگر.
    /// نیاز به HTTPS و اجازهٔ کاربر دارد.
    /// </summary>
    public interface IGeoLocationService
    {
        /// <summary>
        /// موقعیت فعلی را از مرورگر می‌گیرد.
        /// در صورت رد اجازه یا خطا null برمی‌گرداند.
        /// </summary>
        Task<GeoLocationResult?> GetCurrentPositionAsync(CancellationToken cancellationToken = default);

        /// <summary>تبدیل مختصات به آدرس متنی (نشان، در صورت نبود کلید از OSM).</summary>
        Task<string> ReverseGeocodeAsync(double lat, double lng, CancellationToken cancellationToken = default);

        /// <summary>تبدیل مختصات به جزئیات آدرس (استان/شهر/آدرس).</summary>
        Task<GeoAddressDetails> ReverseGeocodeDetailsAsync(double lat, double lng, CancellationToken cancellationToken = default);

        /// <summary>جستجوی متنی مکان و برگرداندن مختصات تقریبی.</summary>
        Task<GeoLocationResult?> GeocodeAsync(string query, CancellationToken cancellationToken = default);
    }

    public class GeoLocationResult
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double? Accuracy { get; set; }
    }

    public class GeoAddressDetails
    {
        public string FormattedAddress { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Neighbourhood { get; set; }
    }
}
