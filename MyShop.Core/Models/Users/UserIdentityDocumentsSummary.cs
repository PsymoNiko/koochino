namespace MyShop.Core.Models.Users
{
    /// <summary>خلاصه وضعیت دو مدرک هویتی یک کاربر (کارت ملی + سلفی) برای گیت‌ها و بنر داشبورد</summary>
    public class UserIdentityDocumentsSummary
    {
        public UserIdentityDocument NationalCard { get; set; }
        public UserIdentityDocument Selfie { get; set; }

        /// <summary>Approved / Rejected / Pending / null (هیچ مدرکی ارسال نشده)</summary>
        public string AggregatedStatus { get; set; }
        public string RejectionReason { get; set; }

        public bool BothSubmitted => NationalCard != null && Selfie != null;
    }
}
