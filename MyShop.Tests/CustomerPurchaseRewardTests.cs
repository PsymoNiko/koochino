using MyShop.Core.Helpers;
using MyShop.Core.Models.Loyalty;

namespace MyShop.Tests
{
    public class CustomerPurchaseRewardTests
    {
        [Fact]
        public void Eligible_Normal_UsesFullInvoiceIncludingWallet()
        {
            // فاکتور ۷۰۰، کیف ۱۰۰ → Final=۶۰۰؛ Eligible باید ۷۰۰ بماند
            var eligible = CustomerPurchaseRewardMath.EligibleAmount(
                orderType: 0, totalAmount: 700_000, finalAmount: 600_000, walletAmountApplied: 100_000, minBuy: 0);
            Assert.Equal(700_000, eligible);
        }

        [Fact]
        public void Eligible_Coupon_ExcludesMinBuy_Example700Minus500()
        {
            var eligible = CustomerPurchaseRewardMath.EligibleAmount(
                orderType: 1, totalAmount: 700_000, finalAmount: 600_000, walletAmountApplied: 0, minBuy: 500_000);
            Assert.Equal(200_000, eligible);
        }

        [Fact]
        public void Eligible_ChanceBox_SameAsCouponRule()
        {
            var eligible = CustomerPurchaseRewardMath.EligibleAmount(
                orderType: 2, totalAmount: 700_000, finalAmount: 700_000, walletAmountApplied: 50_000, minBuy: 500_000);
            Assert.Equal(200_000, eligible);
        }

        [Fact]
        public void Eligible_BelowMinBuy_IsZero()
        {
            var eligible = CustomerPurchaseRewardMath.EligibleAmount(
                orderType: 1, totalAmount: 400_000, finalAmount: 350_000, walletAmountApplied: 0, minBuy: 500_000);
            Assert.Equal(0, eligible);
        }

        [Fact]
        public void Points_Example_200k_Gives_2()
        {
            var points = CustomerPurchaseRewardMath.ComputePoints(200_000, 100_000, 1);
            Assert.Equal(2, points);
        }

        [Theory]
        [InlineData(199_999, 100_000, 1, 1)]
        [InlineData(99_999, 100_000, 1, 0)]
        [InlineData(350_000, 100_000, 2, 6)]
        public void Points_FloorDivision(long eligible, long threshold, int per, int expected)
        {
            Assert.Equal(expected, CustomerPurchaseRewardMath.ComputePoints(eligible, threshold, per));
        }

        [Fact]
        public void Cashback_3Percent_Of_200k()
        {
            var cb = CustomerPurchaseRewardMath.ComputeCashback(200_000, 3m);
            Assert.Equal(6_000, cb);
        }

        [Fact]
        public void ComputeReward_PointsMode_IgnoresCashbackPercent()
        {
            var settings = new CustomerPurchaseRewardSettings
            {
                IsEnabled = true,
                Mode = CustomerPurchaseRewardModes.Points,
                PointsThresholdAmount = 100_000,
                PointsPerThreshold = 1,
                CashbackPercent = 3
            };
            var (points, cash) = CustomerPurchaseRewardMath.ComputeReward(settings, 200_000);
            Assert.Equal(2, points);
            Assert.Equal(0, cash);
        }

        [Fact]
        public void ComputeReward_CashbackMode()
        {
            var settings = new CustomerPurchaseRewardSettings
            {
                IsEnabled = true,
                Mode = CustomerPurchaseRewardModes.CashbackPercent,
                PointsThresholdAmount = 100_000,
                PointsPerThreshold = 1,
                CashbackPercent = 3
            };
            var (points, cash) = CustomerPurchaseRewardMath.ComputeReward(settings, 200_000);
            Assert.Equal(0, points);
            Assert.Equal(6_000, cash);
        }

        [Fact]
        public void ComputeReward_Disabled_ReturnsZero()
        {
            var settings = new CustomerPurchaseRewardSettings { IsEnabled = false, Mode = CustomerPurchaseRewardModes.Points };
            var (points, cash) = CustomerPurchaseRewardMath.ComputeReward(settings, 500_000);
            Assert.Equal(0, points);
            Assert.Equal(0, cash);
        }

        [Fact]
        public void ReferenceIds_AreStable()
        {
            Assert.Equal("Order:42", CustomerPurchaseRewardMath.CashbackReference(42));
            Assert.Equal("OrderWallet:42", CustomerPurchaseRewardMath.WalletDebitReference(42));
            Assert.Equal("OrderWalletRefund:42", CustomerPurchaseRewardMath.WalletRefundReference(42));
        }
    }
}
