using System;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Loyalty;
using MyShop.Core.Helpers;
using MyShop.Core.Models;
using MyShop.Core.Models.Loyalty;

namespace MyShop.Application.Services.Loyalty
{
    public class CustomerPurchaseRewardService : ICustomerPurchaseRewardService
    {
        private readonly ICustomerPurchaseRewardRepository _repo;
        private readonly IOrderRepository _orders;
        private readonly ILoyaltyRoadmapRepository _wallet;
        private readonly IPointRepository _points;

        public CustomerPurchaseRewardService(
            ICustomerPurchaseRewardRepository repo,
            IOrderRepository orders,
            ILoyaltyRoadmapRepository wallet,
            IPointRepository points)
        {
            _repo = repo;
            _orders = orders;
            _wallet = wallet;
            _points = points;
        }

        public Task<CustomerPurchaseRewardSettings> GetSettingsAsync() => _repo.GetSettingsAsync();

        public async Task<(bool ok, string message)> SaveSettingsAsync(CustomerPurchaseRewardSettings settings, int? adminUserId)
        {
            if (settings == null) return (false, "تنظیمات نامعتبر است.");
            if (string.IsNullOrWhiteSpace(settings.Mode))
                settings.Mode = CustomerPurchaseRewardModes.Points;
            if (!string.Equals(settings.Mode, CustomerPurchaseRewardModes.Points, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(settings.Mode, CustomerPurchaseRewardModes.CashbackPercent, StringComparison.OrdinalIgnoreCase))
                return (false, "حالت باید امتیاز یا درصد کش‌بک باشد.");
            if (settings.PointsThresholdAmount <= 0) return (false, "آستانه مبلغ امتیاز باید بزرگ‌تر از صفر باشد.");
            if (settings.PointsPerThreshold <= 0) return (false, "مقدار امتیاز باید بزرگ‌تر از صفر باشد.");
            if (settings.CashbackPercent < 0 || settings.CashbackPercent > 100)
                return (false, "درصد کش‌بک باید بین ۰ تا ۱۰۰ باشد.");

            try
            {
                var (ok, code) = await _repo.UpdateSettingsAsync(settings, adminUserId);
                if (!ok)
                    return (false, code switch
                    {
                        -2 => "حالت نامعتبر است.",
                        -3 => "آستانه مبلغ نامعتبر است.",
                        -4 => "امتیاز نامعتبر است.",
                        -5 => "درصد نامعتبر است.",
                        _ => "خطا در ذخیره تنظیمات. اسکریپت sql/CustomerPurchaseReward_Phase1.sql را اجرا کنید."
                    });
                return (true, "تنظیمات ذخیره شد.");
            }
            catch (Exception ex)
            {
                return (false, "خطا در ذخیره: " + ex.Message + " — اسکریپت sql/CustomerPurchaseReward_Phase1.sql را اجرا کنید.");
            }
        }

        public async Task TryCreditAsync(int orderId)
        {
            if (orderId <= 0) return;
            try
            {
                var order = await _orders.GetByIdAsync(orderId);
                if (order == null) return;

                var settings = await _repo.GetSettingsAsync();
                if (settings == null || !settings.IsEnabled) return;

                var walletApplied = order.WalletAmountApplied
                    ?? await _repo.GetOrderWalletAmountAppliedAsync(orderId)
                    ?? 0;
                var minBuy = await _repo.ResolveMinBuyAsync(order.OrderType, order.CouponUsageId, order.ChanceBoxCodeUsageId);
                var eligible = CustomerPurchaseRewardMath.EligibleAmount(
                    order.OrderType, order.TotalAmount, order.FinalAmount, walletApplied, minBuy);
                var (points, cashback) = CustomerPurchaseRewardMath.ComputeReward(settings, eligible);
                if (points <= 0 && cashback <= 0) return;

                if (string.Equals(settings.Mode, CustomerPurchaseRewardModes.CashbackPercent, StringComparison.OrdinalIgnoreCase))
                {
                    if (cashback <= 0) return;
                    if (await _repo.HasCashbackForOrderAsync(orderId)) return;
                    await _wallet.AdjustWalletAsync(
                        order.UserId,
                        cashback,
                        "Cashback",
                        $"کش‌بک خرید سفارش {order.OrderNumber}",
                        null,
                        CustomerPurchaseRewardMath.CashbackReference(orderId));
                    return;
                }

                if (points <= 0) return;
                if (await _repo.HasPurchaseRewardPointsAsync(orderId)) return;

                var tx = new PointTransaction
                {
                    UserId = order.UserId,
                    MissionId = null,
                    MissionType = "PurchaseReward",
                    Points = points,
                    Status = "Approved",
                    Reason = $"پاداش خرید سفارش {order.OrderNumber} (مبلغ واجد شرایط {eligible:N0})",
                    ReferenceId = orderId.ToString(),
                    ReferenceType = "Order",
                    CreatedAt = DateTime.Now,
                    ReviewedAt = DateTime.Now
                };
                await _points.CreateTransactionAsync(tx);
                await RecalcUserPointsAsync(order.UserId);
            }
            catch
            {
                /* پاداش نباید تأیید سفارش را متوقف کند */
            }
        }

        public async Task TryReverseAsync(int orderId)
        {
            if (orderId <= 0) return;
            try
            {
                var order = await _orders.GetByIdAsync(orderId);
                if (order == null) return;

                // reverse cashback
                if (await _repo.HasCashbackForOrderAsync(orderId))
                {
                    var txs = await _wallet.ListWalletTxAsync(order.UserId, 200);
                    var cash = txs?.FirstOrDefault(t =>
                        string.Equals(t.TxType, "Cashback", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(t.ReferenceId, CustomerPurchaseRewardMath.CashbackReference(orderId), StringComparison.Ordinal));
                    if (cash != null && cash.Amount > 0)
                    {
                        await _wallet.AdjustWalletAsync(
                            order.UserId,
                            -Math.Abs(cash.Amount),
                            "Adjust",
                            $"برگشت کش‌بک سفارش {order.OrderNumber}",
                            null,
                            CustomerPurchaseRewardMath.CashbackReference(orderId) + ":Rev");
                    }
                }

                // reverse points
                var ptId = await _repo.GetPurchaseRewardPointTransactionIdAsync(orderId);
                if (ptId.HasValue)
                {
                    var pt = await _points.GetTransactionByIdAsync(ptId.Value);
                    if (pt != null && string.Equals(pt.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        await _points.UpdateTransactionStatusAsync(ptId.Value, "Rejected", null, "برگشت به دلیل رد سفارش");
                        await RecalcUserPointsAsync(order.UserId);
                    }
                }

                await TryRefundWalletApplyAsync(orderId);
            }
            catch { }
        }

        public async Task<(bool ok, string message)> ApplyWalletDebitForOrderAsync(int orderId, int userId, long walletAmount)
        {
            if (orderId <= 0 || userId <= 0 || walletAmount <= 0)
                return (true, "");
            try
            {
                if (await _repo.HasWalletDebitForOrderAsync(orderId))
                    return (true, "");

                var balance = await _wallet.GetWalletBalanceAsync(userId);
                if (walletAmount > balance)
                    return (false, "موجودی کیف پول کافی نیست.");

                await _wallet.AdjustWalletAsync(
                    userId,
                    -walletAmount,
                    "Debit",
                    $"استفاده از کیف در سفارش #{orderId}",
                    null,
                    CustomerPurchaseRewardMath.WalletDebitReference(orderId));
                await _repo.SetOrderWalletAmountAppliedAsync(orderId, walletAmount);
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task TryRefundWalletApplyAsync(int orderId)
        {
            if (orderId <= 0) return;
            try
            {
                if (!await _repo.HasWalletDebitForOrderAsync(orderId)) return;
                if (await _repo.HasWalletRefundForOrderAsync(orderId)) return;

                var order = await _orders.GetByIdAsync(orderId);
                if (order == null) return;

                var amt = order.WalletAmountApplied
                    ?? await _repo.GetOrderWalletAmountAppliedAsync(orderId)
                    ?? 0;
                if (amt <= 0)
                {
                    var txs = await _wallet.ListWalletTxAsync(order.UserId, 100);
                    var debit = txs?.FirstOrDefault(t =>
                        string.Equals(t.TxType, "Debit", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(t.ReferenceId, CustomerPurchaseRewardMath.WalletDebitReference(orderId), StringComparison.Ordinal));
                    if (debit != null) amt = (long)Math.Abs(debit.Amount);
                }
                if (amt <= 0) return;

                await _wallet.AdjustWalletAsync(
                    order.UserId,
                    amt,
                    "Credit",
                    $"برگشت کیف سفارش {order.OrderNumber}",
                    null,
                    CustomerPurchaseRewardMath.WalletRefundReference(orderId));
            }
            catch { }
        }

        private async Task RecalcUserPointsAsync(int userId)
        {
            var transactions = await _points.GetUserTransactionsAsync(userId, 10000);
            var totalPoints = transactions.Sum(t => t.Points);
            var availablePoints = transactions.Where(t => t.Status == "Approved").Sum(t => t.Points);
            var pendingPoints = transactions.Where(t => t.Status == "Pending").Sum(t => t.Points);
            await _points.UpdateUserPointsAsync(userId, totalPoints, availablePoints, pendingPoints);
        }
    }
}
