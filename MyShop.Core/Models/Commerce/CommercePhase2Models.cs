using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.AdminAccess
{
    public class AdminAccessAuditEntry
    {
        public long Id { get; set; }
        public int? ActorUserId { get; set; }
        public int TargetUserId { get; set; }
        public string Action { get; set; }
        public string DetailJson { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ActorName { get; set; }
        public string TargetName { get; set; }
    }
}

namespace MyShop.Core.Models.Commerce
{
    public class UserAddress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string FullAddress { get; set; } = "";
        public string PostalCode { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommerceDashboardKpis
    {
        public int OrdersToday { get; set; }
        public int OrdersMonth { get; set; }
        public long RevenueToday { get; set; }
        public long RevenueMonth { get; set; }
        public int AwaitingPayment { get; set; }
        public int Processing { get; set; }
        public int Shipped { get; set; }
        public int ActiveCarts { get; set; }
        public int CartItemsTotal { get; set; }
    }

    /// <summary>One Commerce order line used to attribute revenue to a store.</summary>
    public class CommerceReportLine
    {
        public int OrderId { get; set; }
        public int? LineStoreId { get; set; }
        public int? HeaderStoreId { get; set; }
        public long LineAmount { get; set; }
        public byte PaymentStatus { get; set; }
        public byte FulfillmentStatus { get; set; }
    }

    public class CommerceStoreSalesRow
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public int OrderCount { get; set; }
        public int LineCount { get; set; }
        public long LineAmount { get; set; }
    }

    public class CommerceCommissionRate
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? StoreId { get; set; }
        public decimal RatePercent { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CommerceSettlement
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public long GrossAmount { get; set; }
        public decimal RatePercent { get; set; }
        public long CommissionAmount { get; set; }
        public long NetAmount { get; set; }
        public byte Status { get; set; }
        public int? SettledByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Notes { get; set; }
        public string StatusTitle => Status == CommerceConstants.SettlementStatus.Voided ? "باطل" : "تسویه‌شده";
    }

    public class CommerceSettlementPreview
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public long GrossAmount { get; set; }
        public decimal RatePercent { get; set; }
        public long CommissionAmount { get; set; }
        public long NetAmount { get; set; }
        public bool AlreadySettled { get; set; }
    }
}
