using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Finance;

namespace MyShop.Application.Interfaces.Finance
{
    public interface IStoreVoucherWalletRepository
    {
        Task<StoreVoucherWalletDto> GetAsync(int storeId);
        Task<List<StoreVoucherLedgerDto>> ListLedgerAsync(int storeId, int top = 200);
        Task<List<StoreVoucherSettlementDto>> ListSettlementsAsync(int? storeId, bool asRedeemer, string status = null);
        Task<StoreVoucherSummaryDto> GetSummaryAsync();
        Task<int> CreditRebateAsync(int storeId, long amount, decimal? commissionRate, decimal? rebatePercent, DateTime expiresAt, int? orderId, int? calculationId, int? actorUserId);
        Task<int> TopUpAsync(int storeId, long amount, DateTime? expiresAt, int? actorUserId, string note);
        Task<int> ReserveAsync(int storeId, long amount, int? coponId, int? actorUserId);
        Task<int> ReleaseAsync(int storeId, long amount, int? coponId, DateTime? expiresAt, int? actorUserId);
        Task<int> RedeemSettleAsync(int issuerStoreId, int redeemerStoreId, long amount, int? couponUsageId, int? orderId, int? coponId);
        Task<int> ReverseByCalculationAsync(int calculationId);
        Task<(int Count, long Amount)> ExpireBreakageAsync();
        Task<(int Count, long Amount)> ExpireReservedCouponsAsync();
        Task<long> ApplySettlementsToInvoiceAsync(int redeemerStoreId, int invoiceId);
        Task<long> NetReservedByCoponAsync(int coponId);
        Task<(long RebateAmount, int TxnCount)> RebateInPeriodAsync(int storeId, DateTime start, DateTime end);
        Task<int> InsertInvoiceItemAsync(int invoiceId, string description, int txnCount, long baseAmount, decimal? rate, long amount);
        Task<List<StoreVoucherLedgerDto>> ListNearExpiryAsync(int withinDays = 7);
        Task<(long TotalBreakage, int LotCount)> GetBreakageSummaryAsync();
    }

    public interface IStoreVoucherWalletService
    {
        Task<StoreVoucherWalletDto> GetAsync(int storeId);
        Task<List<StoreVoucherLedgerDto>> ListLedgerAsync(int storeId, int top = 200);
        Task<List<StoreVoucherSettlementDto>> ListSettlementsAsync(int? storeId, bool asRedeemer, string status = null);
        Task<StoreVoucherSummaryDto> GetSummaryAsync();
        Task<StoreBillingActionResult> TopUpAsync(int storeId, long amount, int? actorUserId, string note, int? expireDays = null, bool asAdmin = false);
        Task<(int Count, long Amount)> ExpireBreakageAsync();
        Task<StoreBillingActionResult> TryReserveForCouponAsync(int storeId, long faceValue, int? coponId, int? actorUserId);
        Task TryReleaseForCouponAsync(int storeId, long faceValue, int? coponId, int? actorUserId);
    }
}
