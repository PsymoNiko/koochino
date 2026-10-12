using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Finance;
using MyShop.Core.Models.Security;

namespace MyShop.Application.Interfaces.Finance
{
    public interface IStoreBillingRepository
    {
        Task<StoreBillingSettingsDto> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(StoreBillingSettingsDto dto, int? userId);

        Task<List<StoreBillingServiceTypeDto>> ListServiceTypesAsync();
        Task<int> UpsertServiceTypeAsync(StoreBillingServiceTypeDto dto);

        Task<List<StoreBillingOverdueRuleDto>> ListOverdueRulesAsync();
        Task<int> UpsertOverdueRuleAsync(StoreBillingOverdueRuleDto dto);
        Task<bool> DeleteOverdueRuleAsync(int id);

        Task<List<StoreBillingPackageDto>> ListPackagesAsync();
        Task<int> UpsertPackageAsync(StoreBillingPackageDto dto);

        Task<List<StoreBillingContractDto>> ListContractsAsync(int? storeId = null);
        Task<List<StoreBillingContractDto>> ListContractsScopedAsync(DataScopeQuery scope, int? storeId = null);
        Task<StoreBillingContractDto> GetActiveContractAsync(int storeId, DateTime onDate);
        Task<int> UpsertContractAsync(StoreBillingContractDto dto);

        Task<StoreBillingEligibilityDto> ResolveEligibilityAsync(int orderId);
        Task<int> UpsertCalculationAsync(StoreBillingCalculationDto dto);
        Task<StoreBillingCalculationDto> GetCalculationByOrderAsync(int orderId);
        Task<bool> SetCalculationStatusAsync(int? id, int? orderId, string status);
        Task<List<StoreBillingCalculationDto>> ListCalculationsAsync(int? storeId = null, string status = null, DateTime? from = null, DateTime? to = null);

        Task<List<StoreBillingPeriodDto>> ListPeriodsAsync(int? storeId = null, string status = null);
        Task<(int PeriodId, int InvoiceId)> ClosePeriodAsync(int storeId, DateTime start, DateTime end, long minInvoice, int dueDays, decimal? rateSnapshot);
        Task<List<(int StoreId, string StoreName)>> StoresWithPendingCalcsAsync(DateTime start, DateTime end);

        Task<List<StoreBillingInvoiceDto>> ListInvoicesAsync(int? storeId = null, string status = null, int? ownerUserId = null);
        Task<List<StoreBillingInvoiceDto>> ListInvoicesScopedAsync(DataScopeQuery scope, int? storeId = null, string status = null);
        Task<StoreBillingInvoiceDto> GetInvoiceAsync(int id);
        Task<List<StoreBillingInvoiceItemDto>> GetInvoiceItemsAsync(int invoiceId);

        Task<List<StoreBillingPaymentDto>> ListPaymentsAsync(int? invoiceId = null, int? storeId = null);
        Task<List<StoreBillingPaymentDto>> ListPaymentsScopedAsync(DataScopeQuery scope, int? invoiceId = null, int? storeId = null);
        Task<int> InsertPaymentAsync(int invoiceId, long amount, DateTime? paidAt, string method, string reference, string notes, int? userId);

        Task<List<StoreBillingDiscountDto>> ListDiscountsAsync(int? storeId = null);
        Task<int> UpsertDiscountAsync(StoreBillingDiscountDto dto);
        Task<List<StoreBillingDiscountDto>> ActiveDiscountsAsync(int storeId, DateTime onDate);

        Task<List<StoreBillingAdjustmentDto>> ListAdjustmentsAsync(int? storeId = null, int? invoiceId = null);
        Task<int> InsertAdjustmentAsync(int? invoiceId, int? calculationId, int storeId, long amount, string reason, int? userId);

        Task<StoreBillingDashboardDto> GetDashboardAsync();
        Task<List<StoreBillingStoreReportDto>> ReportByStoreAsync();
        Task<List<StoreBillingOverdueInvoiceDto>> MarkOverdueAsync();
        Task InsertAuditAsync(int? actorUserId, int? storeId, string entityType, int? entityId, string action, string details);
    }

    public interface IStoreBillingSettingsService
    {
        Task<StoreBillingSettingsDto> GetAsync();
        Task<StoreBillingActionResult> UpdateAsync(StoreBillingSettingsDto dto, int? userId);
        Task<List<StoreBillingServiceTypeDto>> ListServiceTypesAsync();
        Task<StoreBillingActionResult> UpsertServiceTypeAsync(StoreBillingServiceTypeDto dto);
        Task<List<StoreBillingOverdueRuleDto>> ListOverdueRulesAsync();
        Task<StoreBillingActionResult> UpsertOverdueRuleAsync(StoreBillingOverdueRuleDto dto);
        Task<bool> DeleteOverdueRuleAsync(int id);
    }

    public interface IStoreBillingCatalogService
    {
        Task<List<StoreBillingPackageDto>> ListPackagesAsync();
        Task<StoreBillingActionResult> UpsertPackageAsync(StoreBillingPackageDto dto);
        Task<List<StoreBillingContractDto>> ListContractsAsync(int? storeId = null);
        Task<List<StoreBillingContractDto>> ListContractsForUserAsync(int userId, int? storeId = null, int? siteId = null);
        Task<StoreBillingActionResult> UpsertContractAsync(StoreBillingContractDto dto);
        Task<List<StoreBillingDiscountDto>> ListDiscountsAsync(int? storeId = null);
        Task<StoreBillingActionResult> UpsertDiscountAsync(StoreBillingDiscountDto dto);
        Task<List<StoreBillingAdjustmentDto>> ListAdjustmentsAsync(int? storeId = null, int? invoiceId = null);
        Task<StoreBillingActionResult> InsertAdjustmentAsync(int? invoiceId, int? calculationId, int storeId, long amount, string reason, int? userId);
    }

    public interface IStoreBillingCommissionService
    {
        Task TryCalculateForOrderAsync(int orderId);
        Task TryCancelForOrderAsync(int orderId);
        Task<List<StoreBillingCalculationDto>> ListAsync(int? storeId = null, string status = null);
    }

    public interface IStoreBillingInvoiceService
    {
        Task<StoreBillingActionResult> ClosePeriodAsync(int storeId, DateTime start, DateTime end, int? userId);
        Task<StoreBillingActionResult> CloseAllPendingAsync(DateTime start, DateTime end, int? userId);
        Task<List<StoreBillingPeriodDto>> ListPeriodsAsync(int? storeId = null);
        Task<List<StoreBillingInvoiceDto>> ListInvoicesAsync(int? storeId = null, string status = null, int? ownerUserId = null);
        Task<List<StoreBillingInvoiceDto>> ListInvoicesForUserAsync(int userId, string status = null, int? storeId = null, int? siteId = null);
        Task<StoreBillingInvoiceDto> GetInvoiceAsync(int id);
        Task<StoreBillingActionResult> RecordPaymentAsync(int invoiceId, long amount, string method, string reference, string notes, int? userId);
        Task<List<StoreBillingPaymentDto>> ListPaymentsAsync(int? invoiceId = null, int? storeId = null);
        Task<List<StoreBillingPaymentDto>> ListPaymentsForUserAsync(int userId, int? invoiceId = null, int? storeId = null, int? siteId = null);
        Task<StoreBillingDashboardDto> GetDashboardAsync();
        Task<List<StoreBillingStoreReportDto>> ReportByStoreAsync();
    }

    public interface IStoreBillingEventSink
    {
        Task PublishAsync(string eventCode, int storeId, int? invoiceId, string details);
    }

    public interface IStoreBillingOverdueService
    {
        Task<int> ProcessAsync();
    }
}
