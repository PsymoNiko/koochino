using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Settings
{
    /// <summary>تنظیمات داینامیک پنل ادمین per-site (ذخیره در SitePageContents با کلید admin_settings).</summary>
    public class SiteAdminSettingsDto
    {
        public string SiteDisplayName { get; set; } = "";
        public string SupportPhone { get; set; } = "";
        public string SupportEmail { get; set; } = "";
        public string SupportAddress { get; set; } = "";
        public string SocialTelegram { get; set; } = "";
        public string SocialInstagram { get; set; } = "";
        public bool RegistrationRequireTerms { get; set; } = true;
        public string RegistrationTermsText { get; set; } = "در صورت ثبت‌نام، قوانین و مقررات را می‌پذیرم.";
        public string TermsPageRoute { get; set; } = "/terms";
        public string StoreTermsPageRoute { get; set; } = "/terms/store";
        public bool MaintenanceMode { get; set; }
        public string MaintenanceMessage { get; set; } = "سایت موقتاً در حال به‌روزرسانی است.";
        public string DefaultTimezone { get; set; } = "Asia/Tehran";
        public string AdminPanelFooterNote { get; set; } = "";
        /// <summary>مدت اعتبار بن اکوسیستم از لحظه نشستن کارمزد در کیف (روز). نسخه ۱: ۱ تا ۷۳۰.</summary>
        public int VoucherCreditExpireDays { get; set; } = 90;

        // --- احراز هویت مغازه‌دار ---
        /// <summary>آیا برای استفاده از فروشگاه/کد تخفیف، احراز هویت الزامی است</summary>
        public bool RequireIdentityVerificationForStoreFeatures { get; set; } = true;
        public string IdentityVerificationIntroText { get; set; } = "این مراحل ۳ دقیقه از وقت شما را می‌گیرد.";
        public string IdentityVerificationStep1Title { get; set; } = "اطلاعات شناسنامه‌ای";
        public string IdentityVerificationStep1Hint { get; set; } = "کد ملی، نام پدر، تاریخ تولد و شماره موبایل خود را وارد کنید.";
        public string IdentityVerificationStep2Title { get; set; } = "تصویر کارت ملی";
        public string IdentityVerificationStep2Hint { get; set; } = "تصویر واضح روی کارت ملی خود را آپلود یا با دوربین ثبت کنید.";
        public string IdentityVerificationStep3Title { get; set; } = "سلفی با کارت ملی";
        public string IdentityVerificationStep3Hint { get; set; } = "کد احراز هویت را روی کاغذ بنویسید، امضا و تاریخ بزنید و به همراه کارت ملی، کنار صورت خود عکس بگیرید.";
        public string IdentityVerificationInstructionsHtml { get; set; } = DefaultIdentityInstructionsHtml;
        public string IdentityVerificationPendingMessage { get; set; } = "مدارک ارسالی شما در حالت منتظر تایید می‌باشد. لطفاً تا زمان تایید مدارک توسط مدیر سامانه شکیبا باشید.";
        public string IdentityVerificationGateBannerText { get; set; } = "برای استفاده از فروشگاه و کد تخفیف، ابتدا احراز هویت خود را تکمیل کنید.";
        public int IdentityVerificationMaxFileSizeMB { get; set; } = 5;

        public static SiteAdminSettingsDto GetDefault(string brandName = "کوچینو") => new()
        {
            SiteDisplayName = brandName,
            RegistrationRequireTerms = true,
            RegistrationTermsText = "در صورت ثبت‌نام، قوانین و مقررات را می‌پذیرم.",
            TermsPageRoute = "/terms",
            StoreTermsPageRoute = "/terms/store",
            VoucherCreditExpireDays = 90
        };

        public const string DefaultIdentityInstructionsHtml = @"
<h3>ارسال مدارک</h3>
<p>مدارک ارسالی شما در حالت منتظر تایید می‌باشد لطفا تا زمان تایید مدارک توسط مدیر سامانه شکیبا باشید.</p>
<h3>نکته مهم</h3>
<p>از در اختیار قرار دادن پنل پیامک خود به افراد دیگر خودداری نمایید چراکه سودجویان قصد سوء استفاده از پنل شما برای ارسال‌های فیشینگ و کلاه‌برداری را دارند؛ در این‌صورت مسئولیت کلیه عواقب ناشی از این تخلف به عهده صاحب پنل پیامک می‌باشد.</p>
<h3>راهنما</h3>
<ul>
<li>فایل‌هایی با پسوند jpg, png, jpeg قابل قبول می‌باشند.</li>
<li>حجم فایل عکس نباید بیشتر از ۵ مگابایت باشد.</li>
<li>در صورت واضح نبودن عکس مدرک، تایید نخواهد شد.</li>
<li>تصاویر ارسالی قابل ویرایش یا حذف نیستند؛ قبل از ارسال دقت لازم را به عمل آورید.</li>
</ul>
<h3>نکات مهم برای تهیه تصویر احراز هویت</h3>
<ul>
<li>چهره باید به‌صورت واضح در تصویر مشخص باشد.</li>
<li>کد تایید را دقیقاً مطابق کدی که به شما نمایش داده شده روی برگه یادداشت نمایید.</li>
<li>درج امضا و تاریخ روز الزامی است.</li>
<li>وجود کارت ملی در کنار برگه الزامی است.</li>
<li>برگه باید به‌صورت دست‌نویس باشد.</li>
<li>تصویر را با دوربین پشت گوشی تهیه کنید؛ با دوربین جلو نوشته‌ها برعکس می‌شوند و تایید نخواهد شد.</li>
</ul>
<h3>سوالات متداول</h3>
<p><strong>کارت ملی را گم کرده‌ام، چکار کنم؟</strong><br />به‌جای کارت ملی می‌توانید از گواهینامه یا پاسپورت دارای تاریخ انقضا استفاده کنید.</p>
<p><strong>مدارکم چه مدت طول می‌کشد تا بررسی شود؟</strong><br />بررسی مدارک از ساعت ۹ الی ۲۲ انجام می‌شود؛ لطفاً در این مدت صبور باشید.</p>";
    }

    public class TermsPageDto
    {
        public string Title { get; set; } = "قوانین و مقررات";
        public string HtmlBody { get; set; } = "";

        public static TermsPageDto GetDefaultKoochino() => new()
        {
            Title = "قوانین و مقررات ثبت‌نام مشتریان",
            HtmlBody = DefaultCustomerHtml
        };

        public static TermsPageDto GetDefaultStore() => new()
        {
            Title = "قوانین و مقررات ثبت‌نام مغازه‌داران",
            HtmlBody = DefaultStoreHtml
        };

        public const string DefaultCustomerHtml = @"
<h2>۱. پذیرش قوانین</h2>
<p>با ثبت‌نام به‌عنوان مشتری در سامانه کوچینو، شما این قوانین را می‌پذیرید. در صورت عدم موافقت، لطفاً از خدمات استفاده نکنید.</p>
<h2>۲. حساب مشتری</h2>
<ul>
<li>ارائه شماره موبایل صحیح و قابل دسترس الزامی است.</li>
<li>حفظ امنیت رمز عبور بر عهده کاربر است.</li>
<li>هر شخص حق یک حساب مشتری فعال را دارد.</li>
</ul>
<h2>۳. امتیاز، کوپن و جوایز</h2>
<p>امتیازها، کوپن‌ها و جوایز طبق قوانین هر فروشگاه و سیاست‌های سامانه اعطا می‌شوند. سوءاستفاده یا ثبت خرید ساختگی منجر به تعلیق حساب می‌شود.</p>
<h2>۴. حریم خصوصی</h2>
<p>اطلاعات شخصی صرفاً برای ارائه خدمات باشگاه مشتریان، اطلاع‌رسانی و بهبود تجربه کاربری استفاده می‌شود.</p>
<h2>۵. تغییر قوانین</h2>
<p>این مقررات ممکن است به‌روزرسانی شود. ادامه استفاده پس از انتشار نسخه جدید به منزله پذیرش آن است.</p>
<p><em>آخرین به‌روزرسانی: ۱۴۰۵</em></p>";

        public const string DefaultStoreHtml = @"
<h2>۱. پذیرش قوانین</h2>
<p>با ثبت‌نام به‌عنوان مغازه‌دار در سامانه کوچینو، شما این قوانین را می‌پذیرید. در صورت عدم موافقت، لطفاً ثبت‌نام نکنید.</p>
<h2>۲. حساب فروشگاه</h2>
<ul>
<li>اطلاعات کسب‌وکار و شماره تماس باید صحیح و قابل راستی‌آزمایی باشد.</li>
<li>حفظ امنیت حساب و رمز عبور بر عهده مغازه‌دار است.</li>
<li>ثبت خرید ساختگی، امتیاز غیرواقعی یا تخلف از قوانین باشگاه مشتریان ممنوع است.</li>
</ul>
<h2>۳. مسئولیت فروشگاه</h2>
<p>کیفیت کالا و خدمات بر عهده همان فروشگاه است. کوچینو واسطه باشگاه مشتریان است و در قبال تعهدات فروشگاه مسئولیتی ندارد.</p>
<h2>۴. امتیاز و طرح‌های تشویقی</h2>
<p>تعریف جایزه، تخفیف و مأموریت باید شفاف و مطابق سیاست سامانه باشد. سوءاستفاده منجر به تعلیق پنل فروشگاه می‌شود.</p>
<h2>۵. حریم خصوصی مشتریان</h2>
<p>اطلاعات مشتریان فقط برای ارائه خدمات وفاداری همان فروشگاه استفاده می‌شود و نباید در اختیار اشخاص ثالث قرار گیرد.</p>
<h2>۶. تغییر قوانین</h2>
<p>این مقررات ممکن است به‌روزرسانی شود. ادامه استفاده پس از انتشار نسخه جدید به منزله پذیرش آن است.</p>
<p><em>آخرین به‌روزرسانی: ۱۴۰۵</em></p>";
    }

    public class AppVersionManifest
    {
        public string Version { get; set; } = "2.1.0";
        public string ProductName { get; set; } = "Kochino Platform";
        public string BuildDate { get; set; } = "";
    }

    public class PlatformReleaseNote
    {
        public int Id { get; set; }
        public string Version { get; set; } = "";
        public string Title { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime ReleasedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}
