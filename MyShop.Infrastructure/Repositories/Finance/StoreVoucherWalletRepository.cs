using Dapper;
using MyShop.Application.Interfaces.Finance;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Finance;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Finance
{
    public class StoreVoucherWalletRepository : IStoreVoucherWalletRepository
    {
        private readonly IDbConnectionFactory _db;
        public StoreVoucherWalletRepository(IDbConnectionFactory db) => _db = db;

        public async Task<StoreVoucherWalletDto> GetAsync(int storeId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<StoreVoucherWalletDto>(
                "koch.spStoreVoucherWallet_Get", new { StoreId = storeId }, commandType: CommandType.StoredProcedure)
                ?? new StoreVoucherWalletDto { StoreId = storeId };
        }

        public async Task<List<StoreVoucherLedgerDto>> ListLedgerAsync(int storeId, int top = 200)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreVoucherLedgerDto>(
                "koch.spStoreVoucherLedger_List", new { StoreId = storeId, Top = top }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreVoucherLedgerDto>();
        }

        public async Task<List<StoreVoucherSettlementDto>> ListSettlementsAsync(int? storeId, bool asRedeemer, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreVoucherSettlementDto>(
                "koch.spStoreVoucherSettlements_List",
                new { StoreId = storeId, AsRedeemer = asRedeemer, Status = status },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreVoucherSettlementDto>();
        }

        public async Task<StoreVoucherSummaryDto> GetSummaryAsync()
        {
            using var conn = _db.CreateConnection();
            using var multi = await conn.QueryMultipleAsync("koch.spStoreVoucherWallet_Summary", commandType: CommandType.StoredProcedure);
            var head = await multi.ReadFirstOrDefaultAsync<StoreVoucherSummaryDto>() ?? new StoreVoucherSummaryDto();
            head.TopWallets = (await multi.ReadAsync<StoreVoucherWalletDto>()).ToList();
            return head;
        }

        static int Out(DynamicParameters p) => p.Get<int>("OutId");

        public async Task<int> CreditRebateAsync(int storeId, long amount, decimal? commissionRate, decimal? rebatePercent, DateTime expiresAt, int? orderId, int? calculationId, int? actorUserId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", storeId);
            p.Add("Amount", amount);
            p.Add("CommissionRateSnapshot", commissionRate);
            p.Add("RebatePercentSnapshot", rebatePercent);
            p.Add("ExpiresAt", expiresAt);
            p.Add("OrderId", orderId);
            p.Add("CalculationId", calculationId);
            p.Add("ActorUserId", actorUserId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_CreditRebate", p, commandType: CommandType.StoredProcedure);
            return Out(p);
        }

        public async Task<int> TopUpAsync(int storeId, long amount, DateTime? expiresAt, int? actorUserId, string note)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", storeId);
            p.Add("Amount", amount);
            p.Add("ExpiresAt", expiresAt);
            p.Add("ActorUserId", actorUserId);
            p.Add("Note", note);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_TopUp", p, commandType: CommandType.StoredProcedure);
            return Out(p);
        }

        public async Task<int> ReserveAsync(int storeId, long amount, int? coponId, int? actorUserId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", storeId);
            p.Add("Amount", amount);
            p.Add("CoponID", coponId);
            p.Add("ActorUserId", actorUserId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_Reserve", p, commandType: CommandType.StoredProcedure);
            return Out(p);
        }

        public async Task<int> ReleaseAsync(int storeId, long amount, int? coponId, DateTime? expiresAt, int? actorUserId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", storeId);
            p.Add("Amount", amount);
            p.Add("CoponID", coponId);
            p.Add("ExpiresAt", expiresAt);
            p.Add("ActorUserId", actorUserId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_Release", p, commandType: CommandType.StoredProcedure);
            return Out(p);
        }

        public async Task<int> RedeemSettleAsync(int issuerStoreId, int redeemerStoreId, long amount, int? couponUsageId, int? orderId, int? coponId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("IssuerStoreId", issuerStoreId);
            p.Add("RedeemerStoreId", redeemerStoreId);
            p.Add("Amount", amount);
            p.Add("CouponUsageId", couponUsageId);
            p.Add("OrderId", orderId);
            p.Add("CoponID", coponId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_RedeemSettle", p, commandType: CommandType.StoredProcedure);
            return Out(p);
        }

        public async Task<int> ReverseByCalculationAsync(int calculationId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CalculationId", calculationId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_ReverseByCalculation", p, commandType: CommandType.StoredProcedure);
            return Out(p);
        }

        public async Task<(int Count, long Amount)> ExpireBreakageAsync()
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("OutCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("OutAmount", dbType: DbType.Int64, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_ExpireBreakage", p, commandType: CommandType.StoredProcedure);
            return (p.Get<int>("OutCount"), p.Get<long>("OutAmount"));
        }

        public async Task<(int Count, long Amount)> ExpireReservedCouponsAsync()
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("OutCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("OutAmount", dbType: DbType.Int64, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherWallet_ExpireReservedCoupons", p, commandType: CommandType.StoredProcedure);
            return (p.Get<int>("OutCount"), p.Get<long>("OutAmount"));
        }

        public async Task<(long RebateAmount, int TxnCount)> RebateInPeriodAsync(int storeId, DateTime start, DateTime end)
        {
            using var conn = _db.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync(
                "koch.spStoreVoucherWallet_RebateInPeriod",
                new { StoreId = storeId, PeriodStart = start.Date, PeriodEnd = end.Date },
                commandType: CommandType.StoredProcedure);
            if (row == null) return (0, 0);
            return ((long)row.RebateAmount, (int)row.TxnCount);
        }

        public async Task<int> InsertInvoiceItemAsync(int invoiceId, string description, int txnCount, long baseAmount, decimal? rate, long amount)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("InvoiceId", invoiceId);
            p.Add("Description", description);
            p.Add("TransactionCount", txnCount);
            p.Add("BaseAmount", baseAmount);
            p.Add("CommissionRate", rate);
            p.Add("Amount", amount);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingInvoiceItems_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<List<StoreVoucherLedgerDto>> ListNearExpiryAsync(int withinDays = 7)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreVoucherLedgerDto>(
                "koch.spStoreVoucherWallet_NearExpiry", new { WithinDays = withinDays }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreVoucherLedgerDto>();
        }

        public async Task<(long TotalBreakage, int LotCount)> GetBreakageSummaryAsync()
        {
            using var conn = _db.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync(
                "koch.spStoreVoucherWallet_BreakageSummary", commandType: CommandType.StoredProcedure);
            if (row == null) return (0, 0);
            return ((long)row.TotalBreakage, (int)row.LotCount);
        }

        public async Task<long> ApplySettlementsToInvoiceAsync(int redeemerStoreId, int invoiceId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("RedeemerStoreId", redeemerStoreId);
            p.Add("InvoiceId", invoiceId);
            p.Add("OutAmount", dbType: DbType.Int64, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreVoucherSettlements_ApplyToInvoice", p, commandType: CommandType.StoredProcedure);
            return p.Get<long>("OutAmount");
        }

        public async Task<long> NetReservedByCoponAsync(int coponId)
        {
            using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<long>(
                "koch.spStoreVoucherWallet_ReservedByCopon", new { CoponID = coponId }, commandType: CommandType.StoredProcedure);
        }
    }
}
