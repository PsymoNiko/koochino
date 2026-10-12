using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Finance
{
    public static class StoreVoucherTxType
    {
        public const string CommissionRebate = "CommissionRebate";
        public const string TopUp = "TopUp";
        public const string Reserve = "Reserve";
        public const string Release = "Release";
        public const string RedeemSettle = "RedeemSettle";
        public const string ExpireBreakage = "ExpireBreakage";
        public const string Reverse = "Reverse";
    }

    public class StoreVoucherWalletDto
    {
        public int StoreId { get; set; }
        public long Available { get; set; }
        public long Reserved { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string StoreName { get; set; }
    }

    public class StoreVoucherLedgerDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string TxType { get; set; }
        public long Amount { get; set; }
        public long RemainingAmount { get; set; }
        public decimal? CommissionRateSnapshot { get; set; }
        public decimal? RebatePercentSnapshot { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? OrderId { get; set; }
        public int? CoponID { get; set; }
        public int? CalculationId { get; set; }
        public int? CouponUsageId { get; set; }
        public string Note { get; set; }
        public string StoreName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StoreVoucherSettlementDto
    {
        public int Id { get; set; }
        public int IssuerStoreId { get; set; }
        public int RedeemerStoreId { get; set; }
        public long Amount { get; set; }
        public int? CouponUsageId { get; set; }
        public int? OrderId { get; set; }
        public int? InvoiceId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AppliedAt { get; set; }
        public string IssuerStoreName { get; set; }
        public string RedeemerStoreName { get; set; }
    }

    public class StoreVoucherSummaryDto
    {
        public long TotalAvailable { get; set; }
        public long TotalReserved { get; set; }
        public long TotalBreakage { get; set; }
        public int BreakageLotCount { get; set; }
        public List<StoreVoucherWalletDto> TopWallets { get; set; } = new();
        public List<StoreVoucherLedgerDto> NearExpiry { get; set; } = new();
    }
}
