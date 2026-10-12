using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Finance
{
    public class StoreBillingSettingsDto
    {
        public int Id { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string PeriodKind { get; set; } = StoreBillingConstants.PeriodKind.Monthly;
        public decimal BaseCommissionPercent { get; set; } = 3;
        public int PaymentDueDays { get; set; } = 10;
        public long MinInvoiceAmount { get; set; }
        public string Currency { get; set; } = "تومان";
        public string CalculationMethod { get; set; } = StoreBillingConstants.CalculationMethod.Percent;
        public long FixedAmount { get; set; }
        public bool VoucherEnabled { get; set; }
        public decimal VoucherRebatePercent { get; set; } = 30;
        public bool AllowMerchantTopUp { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
    }

    public class StoreBillingServiceTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StoreBillingOverdueRuleDto
    {
        public int Id { get; set; }
        public int MinOverdueDays { get; set; }
        public int? MaxOverdueDays { get; set; }
        public string ActionCode { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StoreBillingPackageDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal CommissionRate { get; set; }
        public int PaymentDueDays { get; set; }
        public int? VendorPackageId { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StoreBillingContractDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public int PackageId { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal? CustomRate { get; set; }
        public int PaymentDueDays { get; set; }
        public int? CustomDueDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = StoreBillingConstants.ContractStatus.Active;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string StoreName { get; set; }
        public string PackageName { get; set; }

        public decimal EffectiveRate => CustomRate ?? CommissionRate;
        public int EffectiveDueDays => CustomDueDays ?? PaymentDueDays;
    }

    public class StoreBillingEligibilityDto
    {
        public bool IsEligible { get; set; }
        public int? StoreId { get; set; }
        public long PurchaseAmount { get; set; }
        public int? SelectedServiceTypeId { get; set; }
        public string SelectedServiceCode { get; set; }
        public long BaseAmount { get; set; }
        public string Reason { get; set; }
    }

    public class StoreBillingCalculationDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int StoreId { get; set; }
        public long PurchaseAmount { get; set; }
        public int? SelectedServiceTypeId { get; set; }
        public string SelectedServiceCode { get; set; }
        public long BaseAmount { get; set; }
        public decimal CommissionRate { get; set; }
        public long CommissionAmount { get; set; }
        public string CalculationRule { get; set; }
        public string CalculationMethod { get; set; }
        public long FixedAmount { get; set; }
        public int? PackageId { get; set; }
        public int? ContractId { get; set; }
        public string PeriodKind { get; set; }
        public int? PaymentDueDays { get; set; }
        public string Status { get; set; }
        public int? BillingPeriodId { get; set; }
        public int? InvoiceId { get; set; }
        public DateTime CalculatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string StoreName { get; set; }
        public string OrderNumber { get; set; }
    }

    public class StoreBillingPeriodDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Status { get; set; }
        public int TotalTransactions { get; set; }
        public long TotalSales { get; set; }
        public long TotalCommission { get; set; }
        public int? InvoiceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string StoreName { get; set; }
    }

    public class StoreBillingInvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public int StoreId { get; set; }
        public int BillingPeriodId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public long Subtotal { get; set; }
        public long Discount { get; set; }
        public long Adjustment { get; set; }
        public long Tax { get; set; }
        public long TotalAmount { get; set; }
        public long PaidAmount { get; set; }
        public long RemainingAmount { get; set; }
        public string Status { get; set; }
        public int PaymentDueDaysSnapshot { get; set; }
        public decimal? CommissionRateSnapshot { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StoreName { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public List<StoreBillingInvoiceItemDto> Items { get; set; } = new();
    }

    public class StoreBillingInvoiceItemDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string Description { get; set; }
        public int TransactionCount { get; set; }
        public long BaseAmount { get; set; }
        public decimal? CommissionRate { get; set; }
        public long Amount { get; set; }
    }

    public class StoreBillingPaymentDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int StoreId { get; set; }
        public long Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string Method { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string InvoiceNumber { get; set; }
        public string StoreName { get; set; }
    }

    public class StoreBillingDiscountDto
    {
        public int Id { get; set; }
        public int? StoreId { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Reason { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string StoreName { get; set; }
    }

    public class StoreBillingAdjustmentDto
    {
        public int Id { get; set; }
        public int? InvoiceId { get; set; }
        public int? CalculationId { get; set; }
        public int StoreId { get; set; }
        public long Amount { get; set; }
        public string Reason { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StoreName { get; set; }
        public string InvoiceNumber { get; set; }
    }

    public class StoreBillingDashboardDto
    {
        public int PendingCalculations { get; set; }
        public int OpenInvoices { get; set; }
        public long OutstandingAmount { get; set; }
        public int OverdueCount { get; set; }
        public long CollectedAmount { get; set; }
    }

    public class StoreBillingStoreReportDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public int InvoiceCount { get; set; }
        public long TotalBilled { get; set; }
        public long TotalPaid { get; set; }
        public long Remaining { get; set; }
    }

    public class StoreBillingOverdueInvoiceDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime DueDate { get; set; }
        public long RemainingAmount { get; set; }
        public string StoreName { get; set; }
        public int? OwnerUserId { get; set; }
        public int OverdueDays { get; set; }
    }
}
