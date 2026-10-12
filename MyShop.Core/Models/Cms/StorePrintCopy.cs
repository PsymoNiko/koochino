using System.Collections.Generic;

namespace MyShop.Core.Models.Cms
{
    public class StorePrintCopy
    {
        public StorePrintDesign1Copy Design1 { get; set; } = new();
        public StorePrintDesign2Copy Design2 { get; set; } = new();
        public StorePrintDesign3Copy Design3 { get; set; } = new();

        public static StorePrintCopy GetDefault()
        {
            return new StorePrintCopy
            {
                Design1 = StorePrintDesign1Copy.GetDefault(),
                Design2 = StorePrintDesign2Copy.GetDefault(),
                Design3 = StorePrintDesign3Copy.GetDefault()
            };
        }

        public StorePrintCopy CoalesceDefaults()
        {
            var d = GetDefault();
            Design1 ??= new StorePrintDesign1Copy();
            Design2 ??= new StorePrintDesign2Copy();
            Design3 ??= new StorePrintDesign3Copy();
            Design1.Coalesce(d.Design1);
            Design2.Coalesce(d.Design2);
            Design3.Coalesce(d.Design3);
            return this;
        }
    }

    public class StorePrintDesign1Copy
    {
        public string HeaderTitle { get; set; }
        public string HeaderSubtitle { get; set; }
        public string LogoText { get; set; }
        public string StoreNameLabel { get; set; }
        public string PhoneLabel { get; set; }
        public string StoreCodeLabel { get; set; }
        public string QrAppTitle { get; set; }
        public string QrAppHint { get; set; }
        public string QrStoreTitle { get; set; }
        public string QrStoreHint { get; set; }
        public string BenefitsTitle { get; set; }
        public List<string> Benefits { get; set; }
        public string Highlight { get; set; }
        public string FooterUrl { get; set; }
        public string FooterTagline { get; set; }

        public static StorePrintDesign1Copy GetDefault() => new()
        {
            HeaderTitle = "با کوچینو، خریدت جایزه داره!",
            HeaderSubtitle = "خرید حضوری فقط خرید نیست",
            LogoText = "کوچینو",
            StoreNameLabel = "نام مغازه:",
            PhoneLabel = "شماره تماس:",
            StoreCodeLabel = "کد فروشگاه:",
            QrAppTitle = "دانلود اپ کوچینو",
            QrAppHint = "نصب کن و امتیاز بگیر",
            QrStoreTitle = "QR اختصاصی این مغازه",
            QrStoreHint = "بعد از هر خرید اسکن کن",
            BenefitsTitle = "بعد از خرید چی گیرت میاد؟",
            Benefits = new List<string>
            {
                "⭐ امتیاز در اپ کوچینو",
                "💸 تبدیل امتیاز به تخفیف",
                "🎁 شانس دریافت جایزه",
                "🎉 شرکت در قرعه‌کشی‌ها"
            },
            Highlight = "فاکتورت رو ثبت کن و از خریدت لذت ببر",
            FooterUrl = "www.koochino.shop",
            FooterTagline = "کوچینو | امتیاز • تخفیف • جایزه"
        };

        public void Coalesce(StorePrintDesign1Copy d)
        {
            if (d == null) return;
            HeaderTitle = Nz(HeaderTitle, d.HeaderTitle);
            HeaderSubtitle = Nz(HeaderSubtitle, d.HeaderSubtitle);
            LogoText = Nz(LogoText, d.LogoText);
            StoreNameLabel = Nz(StoreNameLabel, d.StoreNameLabel);
            PhoneLabel = Nz(PhoneLabel, d.PhoneLabel);
            StoreCodeLabel = Nz(StoreCodeLabel, d.StoreCodeLabel);
            QrAppTitle = Nz(QrAppTitle, d.QrAppTitle);
            QrAppHint = Nz(QrAppHint, d.QrAppHint);
            QrStoreTitle = Nz(QrStoreTitle, d.QrStoreTitle);
            QrStoreHint = Nz(QrStoreHint, d.QrStoreHint);
            BenefitsTitle = Nz(BenefitsTitle, d.BenefitsTitle);
            Highlight = Nz(Highlight, d.Highlight);
            FooterUrl = Nz(FooterUrl, d.FooterUrl);
            FooterTagline = Nz(FooterTagline, d.FooterTagline);
            if (Benefits == null || Benefits.Count == 0)
                Benefits = d.Benefits ?? new List<string>();
            while (Benefits.Count < 4) Benefits.Add("");
        }

        static string Nz(string v, string d) => string.IsNullOrWhiteSpace(v) ? d : v;
    }

    public class StorePrintDesign2Copy
    {
        public string QrCaption { get; set; }

        public static StorePrintDesign2Copy GetDefault() => new()
        {
            QrCaption = "اسکن برای ثبت سفارش"
        };

        public void Coalesce(StorePrintDesign2Copy d)
        {
            if (d == null) return;
            QrCaption = string.IsNullOrWhiteSpace(QrCaption) ? d.QrCaption : QrCaption;
        }
    }

    public class StorePrintDesign3Copy
    {
        public string BrandName { get; set; }
        public string Tagline { get; set; }
        public string HeadlineLine1 { get; set; }
        public string HeadlineLine2 { get; set; }
        public string IntroText { get; set; }
        public string IntroHighlight { get; set; }
        public List<string> Benefits { get; set; }
        public string QrAppTitle { get; set; }
        public string QrStoreTitle { get; set; }
        public string OfficialBadge { get; set; }
        public string StoreCodeLabel { get; set; }
        public string StepsTitle { get; set; }
        public List<string> Steps { get; set; }
        public List<string> FooterTags { get; set; }
        public string FooterUrl { get; set; }

        public static StorePrintDesign3Copy GetDefault() => new()
        {
            BrandName = "کوچینو",
            Tagline = "سبکی نو در خریدهای حضوری",
            HeadlineLine1 = "اینجا خرید کردن",
            HeadlineLine2 = "متفاوت تره!",
            IntroText = "بعد از خرید در فروشگاه‌های عضو کوچینو",
            IntroHighlight = "امتیاز و جایزه و تخفیف بگیر!",
            Benefits = new List<string> { "جایزه ببر", "امتیاز جمع کن", "تخفیف بگیر", "جعبه شانس" },
            QrAppTitle = "دانلود اپ کوچینو",
            QrStoreTitle = "کد فروشگاه",
            OfficialBadge = "پذیرنده رسمی کوچینو",
            StoreCodeLabel = "شناسه فروشگاه:",
            StepsTitle = "در کمتر از ۳۰ ثانیه عضو کوچینو شو!",
            Steps = new List<string> { "جایزه ببر", "ثبت کن", "خرید کن" },
            FooterTags = new List<string> { "برای همه فروشگاه‌های عضو", "امن، سریع و هوشمند", "بدون هزینه برای مشتری" },
            FooterUrl = "www.koochino.shop"
        };

        public void Coalesce(StorePrintDesign3Copy d)
        {
            if (d == null) return;
            BrandName = Nz(BrandName, d.BrandName);
            Tagline = Nz(Tagline, d.Tagline);
            HeadlineLine1 = Nz(HeadlineLine1, d.HeadlineLine1);
            HeadlineLine2 = Nz(HeadlineLine2, d.HeadlineLine2);
            IntroText = Nz(IntroText, d.IntroText);
            IntroHighlight = Nz(IntroHighlight, d.IntroHighlight);
            QrAppTitle = Nz(QrAppTitle, d.QrAppTitle);
            QrStoreTitle = Nz(QrStoreTitle, d.QrStoreTitle);
            OfficialBadge = Nz(OfficialBadge, d.OfficialBadge);
            StoreCodeLabel = Nz(StoreCodeLabel, d.StoreCodeLabel);
            StepsTitle = Nz(StepsTitle, d.StepsTitle);
            FooterUrl = Nz(FooterUrl, d.FooterUrl);
            if (Benefits == null || Benefits.Count == 0) Benefits = d.Benefits ?? new List<string>();
            while (Benefits.Count < 4) Benefits.Add("");
            if (Steps == null || Steps.Count == 0) Steps = d.Steps ?? new List<string>();
            while (Steps.Count < 3) Steps.Add("");
            if (FooterTags == null || FooterTags.Count == 0) FooterTags = d.FooterTags ?? new List<string>();
            while (FooterTags.Count < 3) FooterTags.Add("");
        }

        static string Nz(string v, string d) => string.IsNullOrWhiteSpace(v) ? d : v;
    }
}
