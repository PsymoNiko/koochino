using System;
using MyShop.Core.Models.Loyalty;

namespace MyShop.Core.Helpers
{
    /// <summary>فرمول‌های خالص پاداش خرید (بدون وابستگی به DB).</summary>
    public static class CustomerPurchaseRewardMath
    {
        public static long EligibleAmount(byte orderType, long totalAmount, long finalAmount, long walletAmountApplied, long minBuy)
        {
            if (orderType == 1 || orderType == 2)
            {
                var eligible = totalAmount - minBuy;
                return eligible > 0 ? eligible : 0;
            }

            // خرید عادی: کل مبلغ فاکتور قبل از کسر کیف
            var baseAmt = finalAmount + (walletAmountApplied > 0 ? walletAmountApplied : 0);
            if (baseAmt <= 0 && totalAmount > 0) baseAmt = totalAmount;
            return baseAmt > 0 ? baseAmt : 0;
        }

        public static int ComputePoints(long eligibleAmount, long thresholdAmount, int pointsPerThreshold)
        {
            if (eligibleAmount <= 0 || thresholdAmount <= 0 || pointsPerThreshold <= 0) return 0;
            return (int)(eligibleAmount / thresholdAmount) * pointsPerThreshold;
        }

        public static long ComputeCashback(long eligibleAmount, decimal cashbackPercent)
        {
            if (eligibleAmount <= 0 || cashbackPercent <= 0) return 0;
            return (long)Math.Round((double)eligibleAmount * (double)cashbackPercent / 100.0, MidpointRounding.AwayFromZero);
        }

        public static (int points, long cashbackToman) ComputeReward(CustomerPurchaseRewardSettings settings, long eligibleAmount)
        {
            if (settings == null || !settings.IsEnabled || eligibleAmount <= 0)
                return (0, 0);

            if (string.Equals(settings.Mode, CustomerPurchaseRewardModes.CashbackPercent, StringComparison.OrdinalIgnoreCase))
                return (0, ComputeCashback(eligibleAmount, settings.CashbackPercent));

            return (ComputePoints(eligibleAmount, settings.PointsThresholdAmount, settings.PointsPerThreshold), 0);
        }

        public static string CashbackReference(int orderId) => $"Order:{orderId}";
        public static string WalletDebitReference(int orderId) => $"OrderWallet:{orderId}";
        public static string WalletRefundReference(int orderId) => $"OrderWalletRefund:{orderId}";
    }
}
