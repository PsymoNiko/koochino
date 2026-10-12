using Dapper;
using MyShop.Application.Interfaces.Finance;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Finance;
using MyShop.Core.Models.Security;
using MyShop.Core.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Finance
{
    public class StoreBillingRepository : IStoreBillingRepository
    {
        private readonly IDbConnectionFactory _db;
        public StoreBillingRepository(IDbConnectionFactory db) => _db = db;

        public async Task<StoreBillingSettingsDto> GetSettingsAsync()
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<StoreBillingSettingsDto>(
                "koch.spStoreBillingSettings_Get", commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateSettingsAsync(StoreBillingSettingsDto dto, int? userId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("IsEnabled", dto.IsEnabled);
            p.Add("PeriodKind", dto.PeriodKind);
            p.Add("BaseCommissionPercent", dto.BaseCommissionPercent);
            p.Add("PaymentDueDays", dto.PaymentDueDays);
            p.Add("MinInvoiceAmount", dto.MinInvoiceAmount);
            p.Add("Currency", dto.Currency);
            p.Add("CalculationMethod", dto.CalculationMethod);
            p.Add("FixedAmount", dto.FixedAmount);
            p.Add("VoucherEnabled", dto.VoucherEnabled);
            p.Add("VoucherRebatePercent", dto.VoucherRebatePercent);
            p.Add("AllowMerchantTopUp", dto.AllowMerchantTopUp);
            p.Add("UpdatedByUserId", userId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingSettings_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<StoreBillingServiceTypeDto>> ListServiceTypesAsync()
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingServiceTypeDto>(
                "koch.spStoreBillingServiceTypes_List", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingServiceTypeDto>();
        }

        public async Task<int> UpsertServiceTypeAsync(StoreBillingServiceTypeDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id > 0 ? dto.Id : (int?)null);
            p.Add("Code", dto.Code);
            p.Add("Name", dto.Name);
            p.Add("Description", dto.Description);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingServiceTypes_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<List<StoreBillingOverdueRuleDto>> ListOverdueRulesAsync()
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingOverdueRuleDto>(
                "koch.spStoreBillingOverdueRules_List", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingOverdueRuleDto>();
        }

        public async Task<int> UpsertOverdueRuleAsync(StoreBillingOverdueRuleDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id > 0 ? dto.Id : (int?)null);
            p.Add("MinOverdueDays", dto.MinOverdueDays);
            p.Add("MaxOverdueDays", dto.MaxOverdueDays);
            p.Add("ActionCode", dto.ActionCode);
            p.Add("Description", dto.Description);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingOverdueRules_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<bool> DeleteOverdueRuleAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingOverdueRules_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<StoreBillingPackageDto>> ListPackagesAsync()
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingPackageDto>(
                "koch.spStoreBillingPackages_List", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingPackageDto>();
        }

        public async Task<int> UpsertPackageAsync(StoreBillingPackageDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id > 0 ? dto.Id : (int?)null);
            p.Add("Name", dto.Name);
            p.Add("Description", dto.Description);
            p.Add("CommissionRate", dto.CommissionRate);
            p.Add("PaymentDueDays", dto.PaymentDueDays);
            p.Add("VendorPackageId", dto.VendorPackageId);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingPackages_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<List<StoreBillingContractDto>> ListContractsAsync(int? storeId = null)
        {
            return await ListContractsScopedAsync(DataScopeQuery.Unrestricted(), storeId);
        }

        public async Task<List<StoreBillingContractDto>> ListContractsScopedAsync(DataScopeQuery scope, int? storeId = null)
        {
            using var conn = _db.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT TOP 400 c.*, s.StoreName, p.Name AS PackageName,
                       ISNULL(c.CustomRate, p.CommissionRate) AS EffectiveRate,
                       ISNULL(c.CustomDueDays, p.PaymentDueDays) AS EffectiveDueDays
                FROM koch.StoreBillingContracts c
                INNER JOIN koch.Stores s ON s.StoreID = c.StoreId
                LEFT JOIN koch.StoreBillingPackages p ON p.Id = c.PackageId
                WHERE (@StoreId IS NULL OR c.StoreId = @StoreId)
                  AND {DataScopeSqlBuilder.StoreScopePredicate("s")}
                ORDER BY c.Id DESC";
            var list = await conn.QueryAsync<StoreBillingContractDto>(sql, new
            {
                StoreId = storeId,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            });
            return list?.ToList() ?? new List<StoreBillingContractDto>();
        }

        public async Task<StoreBillingContractDto> GetActiveContractAsync(int storeId, DateTime onDate)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<StoreBillingContractDto>(
                "koch.spStoreBillingContracts_GetActive",
                new { StoreId = storeId, OnDate = onDate.Date },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertContractAsync(StoreBillingContractDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id > 0 ? dto.Id : (int?)null);
            p.Add("StoreId", dto.StoreId);
            p.Add("PackageId", dto.PackageId);
            p.Add("CustomRate", dto.CustomRate);
            p.Add("CustomDueDays", dto.CustomDueDays);
            p.Add("StartDate", dto.StartDate.Date);
            p.Add("EndDate", dto.EndDate?.Date);
            p.Add("Status", dto.Status ?? StoreBillingConstants.ContractStatus.Active);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingContracts_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<StoreBillingEligibilityDto> ResolveEligibilityAsync(int orderId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<StoreBillingEligibilityDto>(
                "koch.spStoreBilling_ResolveOrderEligibility",
                new { OrderId = orderId },
                commandType: CommandType.StoredProcedure)
                ?? new StoreBillingEligibilityDto { IsEligible = false, Reason = "NotFound" };
        }

        public async Task<int> UpsertCalculationAsync(StoreBillingCalculationDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("OrderId", dto.OrderId);
            p.Add("StoreId", dto.StoreId);
            p.Add("PurchaseAmount", dto.PurchaseAmount);
            p.Add("SelectedServiceTypeId", dto.SelectedServiceTypeId);
            p.Add("SelectedServiceCode", dto.SelectedServiceCode);
            p.Add("BaseAmount", dto.BaseAmount);
            p.Add("CommissionRate", dto.CommissionRate);
            p.Add("CommissionAmount", dto.CommissionAmount);
            p.Add("CalculationRule", dto.CalculationRule);
            p.Add("CalculationMethod", dto.CalculationMethod);
            p.Add("FixedAmount", dto.FixedAmount);
            p.Add("PackageId", dto.PackageId);
            p.Add("ContractId", dto.ContractId);
            p.Add("PeriodKind", dto.PeriodKind);
            p.Add("PaymentDueDays", dto.PaymentDueDays);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingCalculations_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<StoreBillingCalculationDto> GetCalculationByOrderAsync(int orderId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<StoreBillingCalculationDto>(
                "koch.spStoreBillingCalculations_GetByOrder", new { OrderId = orderId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> SetCalculationStatusAsync(int? id, int? orderId, string status)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("OrderId", orderId);
            p.Add("Status", status);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingCalculations_SetStatus", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<StoreBillingCalculationDto>> ListCalculationsAsync(int? storeId = null, string status = null, DateTime? from = null, DateTime? to = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingCalculationDto>(
                "koch.spStoreBillingCalculations_List",
                new { StoreId = storeId, Status = status, From = from, To = to, Top = 300 },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingCalculationDto>();
        }

        public async Task<List<StoreBillingPeriodDto>> ListPeriodsAsync(int? storeId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingPeriodDto>(
                "koch.spStoreBillingPeriods_List", new { StoreId = storeId, Status = status }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingPeriodDto>();
        }

        public async Task<(int PeriodId, int InvoiceId)> ClosePeriodAsync(int storeId, DateTime start, DateTime end, long minInvoice, int dueDays, decimal? rateSnapshot)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", storeId);
            p.Add("PeriodStart", start.Date);
            p.Add("PeriodEnd", end.Date);
            p.Add("MinInvoiceAmount", minInvoice);
            p.Add("PaymentDueDays", dueDays);
            p.Add("CommissionRateSnapshot", rateSnapshot);
            p.Add("OutPeriodId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("OutInvoiceId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBilling_ClosePeriod", p, commandType: CommandType.StoredProcedure);
            return (p.Get<int>("OutPeriodId"), p.Get<int>("OutInvoiceId"));
        }

        public async Task<List<(int StoreId, string StoreName)>> StoresWithPendingCalcsAsync(DateTime start, DateTime end)
        {
            using var conn = _db.CreateConnection();
            var rows = await conn.QueryAsync<StoreBillingStoreRef>(
                "koch.spStoreBilling_StoresWithPendingCalcs",
                new { PeriodStart = start.Date, PeriodEnd = end.Date },
                commandType: CommandType.StoredProcedure);
            return rows?.Select(x => (x.StoreId, x.StoreName)).ToList() ?? new List<(int, string)>();
        }

        private class StoreBillingStoreRef
        {
            public int StoreId { get; set; }
            public string StoreName { get; set; }
        }

        public async Task<List<StoreBillingInvoiceDto>> ListInvoicesAsync(int? storeId = null, string status = null, int? ownerUserId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingInvoiceDto>(
                "koch.spStoreBillingInvoices_List",
                new { StoreId = storeId, Status = status, OwnerUserId = ownerUserId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingInvoiceDto>();
        }

        public async Task<List<StoreBillingInvoiceDto>> ListInvoicesScopedAsync(DataScopeQuery scope, int? storeId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT TOP 400 i.*, s.StoreName, p.PeriodStart, p.PeriodEnd
                FROM koch.StoreBillingInvoices i
                LEFT JOIN koch.Stores s ON s.StoreID = i.StoreId
                LEFT JOIN koch.StoreBillingPeriods p ON p.Id = i.BillingPeriodId
                WHERE (@StoreId IS NULL OR i.StoreId = @StoreId)
                  AND (@Status IS NULL OR i.Status = @Status)
                  AND {DataScopeSqlBuilder.StoreScopePredicate("s")}
                ORDER BY i.Id DESC";
            var list = await conn.QueryAsync<StoreBillingInvoiceDto>(sql, new
            {
                StoreId = storeId,
                Status = status,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            });
            return list?.ToList() ?? new List<StoreBillingInvoiceDto>();
        }

        public async Task<StoreBillingInvoiceDto> GetInvoiceAsync(int id)
        {
            using var conn = _db.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "koch.spStoreBillingInvoices_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
            var inv = await multi.ReadFirstOrDefaultAsync<StoreBillingInvoiceDto>();
            if (inv == null) return null;
            inv.Items = (await multi.ReadAsync<StoreBillingInvoiceItemDto>())?.ToList() ?? new List<StoreBillingInvoiceItemDto>();
            return inv;
        }

        public async Task<List<StoreBillingInvoiceItemDto>> GetInvoiceItemsAsync(int invoiceId)
        {
            var inv = await GetInvoiceAsync(invoiceId);
            return inv?.Items ?? new List<StoreBillingInvoiceItemDto>();
        }

        public async Task<List<StoreBillingPaymentDto>> ListPaymentsAsync(int? invoiceId = null, int? storeId = null)
        {
            return await ListPaymentsScopedAsync(DataScopeQuery.Unrestricted(), invoiceId, storeId);
        }

        public async Task<List<StoreBillingPaymentDto>> ListPaymentsScopedAsync(DataScopeQuery scope, int? invoiceId = null, int? storeId = null)
        {
            using var conn = _db.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT TOP 400 pay.*, i.InvoiceNumber, s.StoreName
                FROM koch.StoreBillingPayments pay
                INNER JOIN koch.StoreBillingInvoices i ON i.Id = pay.InvoiceId
                LEFT JOIN koch.Stores s ON s.StoreID = i.StoreId
                WHERE (@InvoiceId IS NULL OR pay.InvoiceId = @InvoiceId)
                  AND (@StoreId IS NULL OR i.StoreId = @StoreId)
                  AND {DataScopeSqlBuilder.StoreScopePredicate("s")}
                ORDER BY pay.Id DESC";
            var list = await conn.QueryAsync<StoreBillingPaymentDto>(sql, new
            {
                InvoiceId = invoiceId,
                StoreId = storeId,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            });
            return list?.ToList() ?? new List<StoreBillingPaymentDto>();
        }

        public async Task<int> InsertPaymentAsync(int invoiceId, long amount, DateTime? paidAt, string method, string reference, string notes, int? userId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("InvoiceId", invoiceId);
            p.Add("Amount", amount);
            p.Add("PaidAt", paidAt);
            p.Add("Method", method);
            p.Add("Reference", reference);
            p.Add("Notes", notes);
            p.Add("CreatedByUserId", userId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingPayments_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<List<StoreBillingDiscountDto>> ListDiscountsAsync(int? storeId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingDiscountDto>(
                "koch.spStoreBillingDiscounts_List", new { StoreId = storeId }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingDiscountDto>();
        }

        public async Task<int> UpsertDiscountAsync(StoreBillingDiscountDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id > 0 ? dto.Id : (int?)null);
            p.Add("StoreId", dto.StoreId);
            p.Add("DiscountType", dto.DiscountType);
            p.Add("DiscountValue", dto.DiscountValue);
            p.Add("StartDate", dto.StartDate.Date);
            p.Add("EndDate", dto.EndDate?.Date);
            p.Add("Reason", dto.Reason);
            p.Add("IsActive", dto.IsActive);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingDiscounts_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<List<StoreBillingDiscountDto>> ActiveDiscountsAsync(int storeId, DateTime onDate)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingDiscountDto>(
                "koch.spStoreBillingDiscounts_ActiveForStore",
                new { StoreId = storeId, OnDate = onDate.Date },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingDiscountDto>();
        }

        public async Task<List<StoreBillingAdjustmentDto>> ListAdjustmentsAsync(int? storeId = null, int? invoiceId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingAdjustmentDto>(
                "koch.spStoreBillingAdjustments_List", new { StoreId = storeId, InvoiceId = invoiceId }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingAdjustmentDto>();
        }

        public async Task<int> InsertAdjustmentAsync(int? invoiceId, int? calculationId, int storeId, long amount, string reason, int? userId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("InvoiceId", invoiceId);
            p.Add("CalculationId", calculationId);
            p.Add("StoreId", storeId);
            p.Add("Amount", amount);
            p.Add("Reason", reason);
            p.Add("CreatedByUserId", userId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreBillingAdjustments_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<StoreBillingDashboardDto> GetDashboardAsync()
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<StoreBillingDashboardDto>(
                "koch.spStoreBilling_Dashboard", commandType: CommandType.StoredProcedure)
                ?? new StoreBillingDashboardDto();
        }

        public async Task<List<StoreBillingStoreReportDto>> ReportByStoreAsync()
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingStoreReportDto>(
                "koch.spStoreBilling_ReportByStore", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingStoreReportDto>();
        }

        public async Task<List<StoreBillingOverdueInvoiceDto>> MarkOverdueAsync()
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<StoreBillingOverdueInvoiceDto>(
                "koch.spStoreBillingInvoices_MarkOverdue", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreBillingOverdueInvoiceDto>();
        }

        public async Task InsertAuditAsync(int? actorUserId, int? storeId, string entityType, int? entityId, string action, string details)
        {
            using var conn = _db.CreateConnection();
            await conn.ExecuteAsync("koch.spStoreBillingAudit_Insert",
                new { ActorUserId = actorUserId, StoreId = storeId, EntityType = entityType, EntityId = entityId, Action = action, Details = details },
                commandType: CommandType.StoredProcedure);
        }
    }
}
