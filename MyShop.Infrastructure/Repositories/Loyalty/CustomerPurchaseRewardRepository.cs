using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MyShop.Application.Interfaces.Loyalty;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Loyalty;

namespace MyShop.Infrastructure.Repositories.Loyalty
{
    public class CustomerPurchaseRewardRepository : ICustomerPurchaseRewardRepository
    {
        private readonly IDbConnectionFactory _db;

        public CustomerPurchaseRewardRepository(IDbConnectionFactory db) => _db = db;

        public async Task<CustomerPurchaseRewardSettings> GetSettingsAsync()
        {
            using var c = _db.CreateConnection();
            try
            {
                return await c.QueryFirstOrDefaultAsync<CustomerPurchaseRewardSettings>(
                    "koch.spCustomerPurchaseRewardSettings_Get", commandType: CommandType.StoredProcedure)
                    ?? new CustomerPurchaseRewardSettings();
            }
            catch
            {
                return new CustomerPurchaseRewardSettings();
            }
        }

        public async Task<(bool ok, int resultCode)> UpdateSettingsAsync(CustomerPurchaseRewardSettings settings, int? updatedByUserId)
        {
            using var c = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("IsEnabled", settings.IsEnabled);
            p.Add("Mode", settings.Mode);
            p.Add("PointsThresholdAmount", settings.PointsThresholdAmount);
            p.Add("PointsPerThreshold", settings.PointsPerThreshold);
            p.Add("CashbackPercent", settings.CashbackPercent);
            p.Add("UpdatedByUserId", updatedByUserId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await c.ExecuteAsync("koch.spCustomerPurchaseRewardSettings_Update", p, commandType: CommandType.StoredProcedure);
            var code = p.Get<int>("Result");
            return (code == 1, code);
        }

        public async Task SetOrderWalletAmountAppliedAsync(int orderId, long walletAmountApplied)
        {
            using var c = _db.CreateConnection();
            try
            {
                var p = new DynamicParameters();
                p.Add("OrderId", orderId);
                p.Add("WalletAmountApplied", walletAmountApplied);
                p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await c.ExecuteAsync("koch.spOrders_SetWalletAmountApplied", p, commandType: CommandType.StoredProcedure);
            }
            catch
            {
                await c.ExecuteAsync(
                    "UPDATE koch.Orders SET WalletAmountApplied = @Amt WHERE Id = @Id",
                    new { Amt = walletAmountApplied, Id = orderId });
            }
        }

        public async Task<long?> GetOrderWalletAmountAppliedAsync(int orderId)
        {
            using var c = _db.CreateConnection();
            try
            {
                return await c.ExecuteScalarAsync<long?>(
                    "SELECT WalletAmountApplied FROM koch.Orders WHERE Id = @Id", new { Id = orderId });
            }
            catch { return null; }
        }

        public async Task<long> ResolveMinBuyAsync(byte orderType, int? couponUsageId, int? chanceBoxCodeUsageId)
        {
            using var c = _db.CreateConnection();
            if (orderType == 1 && couponUsageId.HasValue && couponUsageId.Value > 0)
            {
                var minBuy = await c.ExecuteScalarAsync<long?>(@"
SELECT TRY_CAST(c.CoponMinBuy AS BIGINT)
FROM koch.CouponUsage u
INNER JOIN koch.Copon c ON c.CoponID = u.CoponID
WHERE u.Id = @UsageId", new { UsageId = couponUsageId.Value });
                return minBuy ?? 0;
            }

            if (orderType == 2 && chanceBoxCodeUsageId.HasValue && chanceBoxCodeUsageId.Value > 0)
            {
                var minBuy = await c.ExecuteScalarAsync<long?>(@"
SELECT cb.MinPurchaseAmount
FROM koch.ChanceBoxCodeUsage u
INNER JOIN koch.ChanceBoxCode cb ON cb.ChanceBoxCodeId = u.ChanceBoxCodeId
WHERE u.Id = @UsageId", new { UsageId = chanceBoxCodeUsageId.Value });
                return minBuy ?? 0;
            }

            return 0;
        }

        public async Task<bool> HasCashbackForOrderAsync(int orderId)
        {
            using var c = _db.CreateConnection();
            var refId = CustomerPurchaseRewardMath.CashbackReference(orderId);
            var n = await c.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.WalletTransactions WHERE TxType = N'Cashback' AND ReferenceId = @Ref",
                new { Ref = refId });
            return n > 0;
        }

        public async Task<bool> HasWalletDebitForOrderAsync(int orderId)
        {
            using var c = _db.CreateConnection();
            var refId = CustomerPurchaseRewardMath.WalletDebitReference(orderId);
            var n = await c.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.WalletTransactions WHERE TxType = N'Debit' AND ReferenceId = @Ref",
                new { Ref = refId });
            return n > 0;
        }

        public async Task<bool> HasWalletRefundForOrderAsync(int orderId)
        {
            using var c = _db.CreateConnection();
            var refId = CustomerPurchaseRewardMath.WalletRefundReference(orderId);
            var n = await c.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.WalletTransactions WHERE ReferenceId = @Ref",
                new { Ref = refId });
            return n > 0;
        }

        public async Task<bool> HasPurchaseRewardPointsAsync(int orderId)
        {
            using var c = _db.CreateConnection();
            var n = await c.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM koch.PointTransactions
WHERE MissionType = N'PurchaseReward' AND ReferenceType = N'Order' AND ReferenceId = @Ref",
                new { Ref = orderId.ToString() });
            return n > 0;
        }

        public async Task<int?> GetPurchaseRewardPointTransactionIdAsync(int orderId)
        {
            using var c = _db.CreateConnection();
            return await c.ExecuteScalarAsync<int?>(@"
SELECT TOP 1 Id FROM koch.PointTransactions
WHERE MissionType = N'PurchaseReward' AND ReferenceType = N'Order' AND ReferenceId = @Ref
ORDER BY Id DESC",
                new { Ref = orderId.ToString() });
        }
    }
}
