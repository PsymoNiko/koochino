using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Cms
{
    public class SitePageContent
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string PageKey { get; set; } = "";
        public string Title { get; set; }
        public string ContentJson { get; set; } = "{}";
        public DateTime UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class SitePageInfo
    {
        public string PageKey { get; set; } = "";
        public string Title { get; set; } = "";
        public string Route { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasStructuredEditor { get; set; }
    }

    /// <summary>متن‌های قابل ویرایش لندینگ سبز (صفحه اصلی /).</summary>
    public class LandingGreenCopy
    {
        public string BrandName { get; set; } = "کوچینو";
        public string NavFeatures { get; set; } = "امکانات";
        public string NavProcess { get; set; } = "نحوه کار";
        public string NavStores { get; set; } = "فروشگاه‌ها";
        public string NavContact { get; set; } = "تماس";
        public string TabCustomer { get; set; } = "مشتری هستم";
        public string TabStore { get; set; } = "مغازه‌دار هستم";

        public string CustomerHeroTitle { get; set; } = "حس خوبِ خرید با کوچینو";
        public string CustomerHeroSubtitle { get; set; } = "خرید حضوری دیگر فقط یک خرید ساده نیست. تخفیف، امتیاز و جایزه از جعبه جادویی در انتظار شماست. کوچینو سبکی نو در خرید حضوری.";
        public string CustomerHeroBtnCustomer { get; set; } = "بزن بریم جایزه";
        public string CustomerHeroBtnStore { get; set; } = "ورود مغازه‌داران";

        public string FeaturesTitle { get; set; } = "امکانات ویژه کوچینو";
        public List<LandingCard> FeatureCards { get; set; } = new();

        public string ProcessTitle { get; set; } = "چطور جایزه بگیرم؟";
        public List<LandingCard> ProcessSteps { get; set; } = new();

        public string StoresTitle { get; set; } = "فروشگاه‌ها و مغازه‌ها";

        public string CustomerFaqTitle { get; set; } = "سؤالات متداول مشتریان";
        public List<LandingFaq> CustomerFaqs { get; set; } = new();

        public string CustomerCtaTitle { get; set; } = "همین حالا شروع کنید!";
        public string CustomerCtaSubtitle { get; set; } = "به جمع کاربران کوچینو بپیوندید و از هر خرید خود جایزه بگیرید.";
        public string CustomerCtaButton { get; set; } = "شروع به کار";

        public string StoreHeroTitle { get; set; } = "مشتری وفادار بساز، فروش را بیشتر کن";
        public string StoreHeroSubtitle { get; set; } = "کوچینو ابزار ساده وفادارسازی مشتری برای مغازه‌های حضوری.";
        public string StoreHeroButton { get; set; } = "مغازه‌ام را به کوچینو وصل می‌کنم";

        public string StoreBenefitsTitle { get; set; } = "مزایای کوچینو برای مغازه‌دار";
        public List<LandingCard> StoreBenefitCards { get; set; } = new();

        public string StoreFaqTitle { get; set; } = "سؤالات متداول مغازه‌داران";
        public List<LandingFaq> StoreFaqs { get; set; } = new();

        public string StoreCtaTitle { get; set; } = "مغازه‌تان را به کوچینو بیاورید";
        public string StoreCtaSubtitle { get; set; } = "همین حالا ثبت‌نام کنید و فروش خود را با وفادارسازی هوشمند بالا ببرید.";
        public string StoreCtaButton { get; set; } = "ثبت‌نام مغازه‌دار";

        public string FooterAboutTitle { get; set; } = "کوچینو";
        public string FooterAboutText { get; set; } = "پلتفرم هوشمند ایجاد حس خوب در خریدهای حضوری. با کوچینو، هر خرید یک تجربه شگفت‌انگیز است!";
        public string FooterContactTitle { get; set; } = "تماس با ما";
        public string FooterContactText { get; set; } = "";

        // ===== فیلدهای اختصاصی طراحی جدید صفحه اصلی (KoochinoHome / index3) =====
        public string HeroEyebrow { get; set; } = "کوچینو، اکوسیستم وفاداری خرید حضوری";
        public string HeroTitleLine1 { get; set; } = "اینجا بعد از خرید حضوری،";
        public string HeroTitleAccent { get; set; } = "امتیاز و جایزه هم داری!";
        public string HeroNote { get; set; } = "همین حالا وارد کوچینو شو و از مزایا بهره‌مند شو";
        public LandingButton HeaderLoginButton { get; set; } = new("ورود / ثبت‌نام", "/Account/LoginSelect", "#6D28D9");
        public LandingButton HeroBtnStore { get; set; } = new("من مغازه‌دار هستم", "/Account/login-store", "#F97316");
        public LandingButton HeroBtnCustomer { get; set; } = new("من مشتری هستم", "/Account/login-customer", "#6D28D9");

        public string BubbleText { get; set; } = "از این به بعد وقتی خرید حضوری می‌کنی، فروشنده می‌پرسه: «کوچینو داری؟»";

        public string WhatCustomerTitle { get; set; } = "مشتری";
        public string WhatCustomerText { get; set; } = "با هر خرید حضوری امتیاز می‌گیره و از جعبه شانس و تخفیف‌های عضو کوچینو بهره‌مند می‌شه.";
        public string WhatStoreTitle { get; set; } = "مغازه‌دار";
        public string WhatStoreText { get; set; } = "مشتری جدید جذب می‌کنه، فروش بیشتر و مشتری وفادار می‌سازه و کسب‌وکارش رشد می‌کنه.";

        public string CycleTitle { get; set; } = "یک چرخه تکرارشونده که همه در آن سود می‌برند";
        public string CycleSubtitle { get; set; } = "مشتری لذت می‌برد، مغازه‌دار رشد می‌کند، چرخه کوچینو بزرگ‌تر می‌شود...";
        public List<LandingCard> CycleSteps { get; set; } = new();

        public List<LandingCard> TrustPoints { get; set; } = new();

        public string CompareTitle { get; set; } = "فروشگاه‌های آنلاین سال‌هاست امتیاز و جایزه می‌دهند";
        public string CompareSubtitle { get; set; } = "حالا نوبت خرید حضوری است؛ کوچینو ابزار لازم را در اختیار کسب‌وکارهای حضوری قرار می‌دهد";
        public string CompareLeftTitle { get; set; } = "فروشگاه‌های آنلاین";
        public string CompareLeftText { get; set; } = "امتیاز، تخفیف و ارسال رایگان سال‌هاست به‌صورت استاندارد ارائه می‌شود.";
        public string CompareRightTitle { get; set; } = "کسب‌وکارهای عضو کوچینو";
        public string CompareRightText { get; set; } = "امتیاز، جعبه شانس و وفاداری واقعی، دقیقاً همان لحظه‌ی خرید حضوری.";

        public string PanelsTitle { get; set; } = "برای مغازه‌دار یا مشتری، یک قدم تا شروع";
        public string CustomerPanelHeading { get; set; } = "هر خرید تو یه چیزی داره!";
        public string CustomerPanelText { get; set; } = "جمع‌آوری امتیاز روزهای خریدت، دریافت جایزه و تخفیف‌های جذاب، و تجربه‌ی خرید حضوری خوشایندتر با کوچینو.";
        public List<LandingCard> CustomerBenefits { get; set; } = new();
        public LandingButton CustomerPanelButton { get; set; } = new("ورود به کوچینو", "/Account/login-customer", "#6D28D9");

        public string StorePanelHeading { get; set; } = "مشتری فقط یک‌بار خرید نکنه!";
        public string StorePanelText { get; set; } = "با کوچینو، مغازه شما یک برنامه وفاداری هوشمند دارد که مشتری را برای خرید بعدی هم برمی‌گرداند.";
        public LandingButton StorePanelButton { get; set; } = new("ثبت‌نام رایگان مغازه", "/Account/StoreRegister", "#F97316");

        public LandingButton FinalCtaBtnStore { get; set; } = new("من مغازه‌دار هستم", "/Account/login-store", "#F97316");
        public LandingButton FinalCtaBtnCustomer { get; set; } = new("من مشتری هستم", "/Account/login-customer", "#6D28D9");

        public static LandingGreenCopy GetDefault()
        {
            return new LandingGreenCopy
            {
                FeatureCards = new List<LandingCard>
                {
                    new("🎁", "جعبه شانس کوچینو", "بعد از هر خرید حضوری جعبه شانس باز کنید؛ تخفیف نقدی، کالای رایگان یا امتیاز ویژه!"),
                    new("🤝", "ویژه مغازه‌داران", "وفادارسازی و جوایز هوشمند؛ مشتریان و فروش خود را چند برابر کنید."),
                    new("🎟️", "قرعه‌کشی‌های بزرگ", "هر خرید یک شانس برای قرعه‌کشی‌های ماهیانه و جوایز نقدی و کالایی."),
                    new("💎", "باشگاه مشتریان", "با هر خرید امتیاز جمع کنید و از تخفیف‌های ویژه استفاده کنید. امتیازات منقضی نمی‌شوند!")
                },
                ProcessSteps = new List<LandingCard>
                {
                    new("۱", "خرید حضوری", "از مغازه‌های عضو کوچینو خرید کنید و از کیفیت محصولات لذت ببرید."),
                    new("۲", "ثبت خرید", "شماره موبایل خود را به فروشنده بگویید تا خرید ثبت شود و جعبه شانس فعال گردد."),
                    new("۳", "باز کردن جعبه", "وارد سایت شوید، جعبه شانس را باز کنید و جایزه خود را بردارید!")
                },
                CustomerFaqs = new List<LandingFaq>
                {
                    new("آیا استفاده از کوچینو هزینه دارد؟", "خیر، استفاده برای مشتریان کاملاً رایگان است."),
                    new("چطور امتیاز می‌گیرم؟", "با هر خرید از مغازه‌های عضو کوچینو امتیاز و جعبه شانس دریافت می‌کنید."),
                    new("امتیازها منقضی می‌شوند؟", "امتیازات کوچینو منقضی نمی‌شوند؛ بسته به قوانین هر مغازه می‌توانید از تخفیف استفاده کنید.")
                },
                StoreBenefitCards = new List<LandingCard>
                {
                    new("👥", "بازگشت مشتری", "مشتری انگیزه برای خرید دوباره دارد"),
                    new("📈", "افزایش فروش", "خرید تکراری بیشتر"),
                    new("📱", "مدیریت ساده", "همه‌چیز با موبایل")
                },
                StoreFaqs = new List<LandingFaq>
                {
                    new("راه‌اندازی کوچینو چقدر زمان می‌برد؟", "کمتر از ۱۰ دقیقه."),
                    new("نیاز به دستگاه خاصی هست؟", "خیر، فقط موبایل کافی است."),
                    new("هزینه استفاده چقدر است؟", "پلن‌های متنوع متناسب با نوع مغازه.")
                },
                CycleSteps = new List<LandingCard>
                {
                    new("۱", "خرید کن", "از مغازه‌های عضو کوچینو خرید کن"),
                    new("۲", "ثبت کن", "شماره موبایلت رو به فروشنده بگو"),
                    new("۳", "امتیاز بگیر", "جعبه شانس برات فعال می‌شه"),
                    new("۴", "جایزه بگیر", "جعبه رو باز کن و جایزه بردار"),
                    new("۵", "استفاده کن", "تخفیفت رو در خرید بعدی خرج کن")
                },
                TrustPoints = new List<LandingCard>
                {
                    new("✅", "رایگان برای مشتریان", "هیچ هزینه‌ای برای استفاده مشتریان از کوچینو وجود ندارد."),
                    new("⏱️", "راه‌اندازی زیر ۱۰ دقیقه", "مغازه‌داران بدون پیچیدگی، سریع عضو کوچینو می‌شوند."),
                    new("📱", "فقط با موبایل", "بدون نیاز به دستگاه یا سخت‌افزار خاص، همه‌چیز روی گوشی."),
                    new("♾️", "امتیاز بدون انقضا", "امتیازهای کوچینو هرگز منقضی نمی‌شوند.")
                },
                CustomerBenefits = new List<LandingCard>
                {
                    new("✅", "استفاده کاملاً رایگان", "هیچ هزینه‌ای برای عضویت و استفاده مشتریان وجود ندارد."),
                    new("✅", "امتیازها هرگز منقضی نمی‌شوند", "با هر خرید حضوری از مغازه‌های عضو، امتیاز و جعبه شانس بگیر."),
                    new("✅", "فقط با شماره موبایل", "نیازی به کارت یا اپلیکیشن جدا نیست؛ شماره‌ات رو به فروشنده بگو.")
                }
            };
        }
    }

    /// <summary>دکمهٔ قابل‌ویرایش لندینگ (متن + لینک + رنگ) — برای CTAهای صفحه اصلی جدید.</summary>
    public class LandingButton
    {
        public string Text { get; set; } = "";
        public string Link { get; set; } = "";
        public string Color { get; set; } = "#6D28D9";
        public LandingButton() { }
        public LandingButton(string text, string link, string color) { Text = text; Link = link; Color = color; }
    }

    public class LandingCard
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public LandingCard() { }
        public LandingCard(string icon, string title, string text) { Icon = icon; Title = title; Text = text; }
    }

    public class LandingFaq
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public LandingFaq() { }
        public LandingFaq(string q, string a) { Question = q; Answer = a; }
    }

    public class MissionPageCopy
    {
        public string Title { get; set; } = "ماموریت ما";
        public string Body { get; set; } = "کوچینو ابزار وفادارسازی هوشمند برای خرید حضوری است؛ هر خرید یک تجربه‌ی خوب و یک جایزه دارد.";

        public static MissionPageCopy GetDefault() => new();
    }

}
