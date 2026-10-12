using System;

namespace MyShop.Core.Models.Lucky
{
    public class LuckyLeague
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ThemeKey { get; set; } = "wooden";
        public int MinPoints { get; set; }
        public int? MaxPoints { get; set; }
        public int PointsPerBox { get; set; }
        public int TotalBoxesPerRound { get; set; } = 10;
        public int LosingBoxesPerRound { get; set; } = 6;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>یک آیتم کاتالوگ جایزهٔ یک لیگ. «پوچ» ردیف این جدول نیست؛ نتیجهٔ الگوریتم ساخت دور است.</summary>
    public class LuckyPrize
    {
        public int Id { get; set; }
        public int LeagueId { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>Points | DiscountCode | ExtraChance</summary>
        public string PrizeType { get; set; } = string.Empty;
        public byte? DiscountType { get; set; }
        public long? DiscountValue { get; set; }
        public string MinBuy { get; set; }
        public int? ExpireDays { get; set; }
        public int? PointsValue { get; set; }
        public int Weight { get; set; } = 1;
        public int? Stock { get; set; }
        public string IconKey { get; set; }
        /// <summary>0 = همهٔ فروشگاه‌ها، 1 = فروشگاه مشخص</summary>
        public byte Scope { get; set; }
        public int? StoreId { get; set; }
        public int? CreatedByUserId { get; set; }
        /// <summary>Admin | StoreOwner</summary>
        public string CreatedByRole { get; set; }
        /// <summary>0=Pending, 1=Approved, 2=Rejected</summary>
        public byte Status { get; set; } = 1;
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string AdminNote { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string StoreName { get; set; }
        public string LeagueName { get; set; }
    }

    public class LuckyBoxRound
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int LeagueId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LuckyBox
    {
        public int Id { get; set; }
        public int RoundId { get; set; }
        public int BoxIndex { get; set; }
        public bool IsLosing { get; set; }
        public int? PrizeId { get; set; }
        public bool IsOpened { get; set; }
        public DateTime? OpenedAt { get; set; }
        /// <summary>Points | Chance</summary>
        public string PaymentMethod { get; set; }
        public int? PointsSpent { get; set; }
        public int? ResultCouponId { get; set; }
        public int? ResultPointsCredited { get; set; }

        // فقط برای جعبه‌های بازشده پر می‌شود
        public string PrizeTitle { get; set; }
        public string PrizeType { get; set; }
        public string PrizeIconKey { get; set; }
        public string ResultCouponCode { get; set; }
    }

    public class LuckyChanceBalance
    {
        public int UserId { get; set; }
        public int LeagueId { get; set; }
        public int Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LuckyLeagueViewDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ThemeKey { get; set; }
        public int MinPoints { get; set; }
        public int? MaxPoints { get; set; }
        public int PointsPerBox { get; set; }
        public int TotalBoxesPerRound { get; set; }
        public int LosingBoxesPerRound { get; set; }
        public bool IsUnlocked { get; set; }
        public int ChanceBalance { get; set; }
    }

    public class LuckyBoxOpenResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public bool IsLosing { get; set; }
        public string PrizeType { get; set; }
        public string PrizeTitle { get; set; }
        public int? PointsCredited { get; set; }
        public string CouponCode { get; set; }
        public int RemainingAvailablePoints { get; set; }
        public int RemainingChance { get; set; }
        public bool RoundCompleted { get; set; }
        public int NewRoundId { get; set; }
    }

    /// <summary>ردیف گزارش باز کردن جعبه در پنل ادمین.</summary>
    public class LuckyBoxOpenReportRow
    {
        public int BoxId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string ThemeKey { get; set; }
        public DateTime OpenedAt { get; set; }
        public bool IsLosing { get; set; }
        public string PaymentMethod { get; set; }
        public int? PointsSpent { get; set; }
        public string PrizeType { get; set; }
        public string PrizeTitle { get; set; }
        public int? ResultPointsCredited { get; set; }
        public string CouponCode { get; set; }

        public string OutcomeLabel => IsLosing ? "پوچ" : "برنده";
        public string PrizeDisplay
        {
            get
            {
                if (IsLosing) return "—";
                if (!string.IsNullOrWhiteSpace(CouponCode))
                    return $"{PrizeTitle ?? "کد تخفیف"} ({CouponCode})";
                if (ResultPointsCredited.HasValue && ResultPointsCredited > 0)
                    return $"{ResultPointsCredited:N0} امتیاز";
                return PrizeTitle ?? PrizeType ?? "—";
            }
        }
    }

    public class LuckyBoxOpenReportStats
    {
        public int TotalOpened { get; set; }
        public int WinnersCount { get; set; }
        public int LosersCount { get; set; }
        public decimal WinRatePercent => TotalOpened > 0
            ? Math.Round(100m * WinnersCount / TotalOpened, 1)
            : 0;
    }
}
