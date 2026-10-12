using System;

namespace MyShop.Core.Models.Loyalty
{
    public static class CustomerPurchaseRewardModes
    {
        public const string Points = "Points";
        public const string CashbackPercent = "CashbackPercent";
    }

    public class CustomerPurchaseRewardSettings
    {
        public int Id { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;
        public string Mode { get; set; } = CustomerPurchaseRewardModes.Points;
        public long PointsThresholdAmount { get; set; } = 100_000;
        public int PointsPerThreshold { get; set; } = 1;
        public decimal CashbackPercent { get; set; } = 3;
        public DateTime UpdatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}
