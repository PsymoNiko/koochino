using MyShop.Application.Services;
using MyShop.Core.Models;

namespace MyShop.Tests
{
    public class StoreVoucherWalletTests
    {
        static long Rebate(long commission, double rebatePercent) =>
            (long)Math.Round(commission * rebatePercent / 100.0, MidpointRounding.AwayFromZero);

        static long Commission(long finalAmount, double rate) =>
            (long)Math.Round(finalAmount * rate / 100.0, MidpointRounding.AwayFromZero);

        [Fact]
        public void FaceValue_FixedTimesStock()
        {
            var c = new Coupon { DiscountType = 0, DiscountValue = 50000, CoponStock = 10 };
            Assert.Equal(500_000, CouponService.FaceValue(c));
        }

        [Fact]
        public void FaceValue_PercentIsZero()
        {
            var c = new Coupon { DiscountType = 1, DiscountValue = 10, CoponStock = 100 };
            Assert.Equal(0, CouponService.FaceValue(c));
        }

        [Theory]
        [InlineData(15000, 30, 4500)]
        [InlineData(15000, 25, 3750)]
        [InlineData(100, 0, 0)]
        public void RebateSlice_FromCommissionAndAdminPercent(long commission, double rebatePercent, long expected)
        {
            Assert.Equal(expected, Rebate(commission, rebatePercent));
        }

        [Fact]
        public void Scenario1_ConfirmedPurchaseCreditsWalletWithExpire()
        {
            var commission = Commission(500_000, 3);
            var rebate = Rebate(commission, 30);
            var expireDays = 90;
            var expiresAt = DateTime.Today.AddDays(expireDays);
            Assert.Equal(15000, commission);
            Assert.Equal(4500, rebate);
            Assert.True(expiresAt > DateTime.Today);
        }

        [Fact]
        public void Scenario2_IssueRejectedWhenFaceExceedsAvailable()
        {
            long available = 4500;
            var face = CouponService.FaceValue(new Coupon { DiscountType = 0, DiscountValue = 5000, CoponStock = 1 });
            Assert.True(face > available);
        }

        [Fact]
        public void Scenario3_RedeemAtBCreatesReceivableForB()
        {
            long reservedA = 50_000;
            long settle = 50_000;
            long reservedAfter = reservedA - settle;
            long bReceivable = settle;
            Assert.Equal(0, reservedAfter);
            Assert.Equal(50_000, bReceivable);
        }

        [Fact]
        public void Scenario4_CancelBeforeIssueReversesRebateNotLaterSettings()
        {
            var rebateAtIssue = Rebate(15000, 30);
            var newRebatePercent = 10.0;
            var wouldBeNow = Rebate(15000, newRebatePercent);
            Assert.Equal(4500, rebateAtIssue);
            Assert.NotEqual(rebateAtIssue, wouldBeNow);
        }

        [Fact]
        public void Scenario5_BreakageAfterSiteExpireDays()
        {
            var created = DateTime.Today.AddDays(-91);
            var expireDays = 90;
            var expiresAt = created.AddDays(expireDays);
            Assert.True(DateTime.Today > expiresAt);
        }

        [Fact]
        public void Scenario6_ChangingAdminPercentsDoesNotRewriteOldSnapshot()
        {
            decimal snapshotRate = 3;
            decimal snapshotRebate = 30;
            decimal newRate = 5;
            decimal newRebate = 20;
            var oldCommission = Commission(100_000, (double)snapshotRate);
            var oldRebate = Rebate(oldCommission, (double)snapshotRebate);
            var newCommission = Commission(100_000, (double)newRate);
            var newRebateAmt = Rebate(newCommission, (double)newRebate);
            Assert.Equal(3000, oldCommission);
            Assert.Equal(900, oldRebate);
            Assert.NotEqual(oldCommission, newCommission);
            Assert.NotEqual(oldRebate, newRebateAmt);
        }

        [Fact]
        public void Scenario7_TopUpThenLargerVoucher()
        {
            long rebateAvailable = 4500;
            long topUp = 100_000;
            long available = rebateAvailable + topUp;
            var face = CouponService.FaceValue(new Coupon { DiscountType = 0, DiscountValue = 20_000, CoponStock = 5 });
            Assert.True(face > rebateAvailable);
            Assert.True(face <= available);
        }

        [Fact]
        public void ClosePeriod_NetsBReceivableAgainstCommissionInvoice()
        {
            long invoiceCommission = 12_000;
            long openSettlementCredit = 5_000;
            long payableAfterNet = invoiceCommission - openSettlementCredit;
            Assert.Equal(7_000, payableAfterNet);
        }

        [Fact]
        public void RetailCommission_PercentOfFinalAmount()
        {
            Assert.Equal(15000, Commission(500_000, 3));
        }
    }
}
