using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Finance;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Finance;

namespace MyShop.Application.Services.Finance
{
    public class StoreBillingSettingsService : IStoreBillingSettingsService
    {
        private readonly IStoreBillingRepository _repo;
        public StoreBillingSettingsService(IStoreBillingRepository repo) => _repo = repo;

        public async Task<StoreBillingSettingsDto> GetAsync()
            => await _repo.GetSettingsAsync() ?? new StoreBillingSettingsDto();

        public async Task<StoreBillingActionResult> UpdateAsync(StoreBillingSettingsDto dto, int? userId)
        {
            if (dto == null) return StoreBillingActionResult.Fail("تنظیمات نامعتبر است.");
            if (dto.BaseCommissionPercent < 0 || dto.BaseCommissionPercent > 100)
                return StoreBillingActionResult.Fail("نرخ باید بین ۰ تا ۱۰۰ باشد.");
            if (dto.PaymentDueDays < 0 || dto.PaymentDueDays > 365)
                return StoreBillingActionResult.Fail("مهلت پرداخت نامعتبر است.");
            if (dto.VoucherRebatePercent < 0 || dto.VoucherRebatePercent > 100)
                return StoreBillingActionResult.Fail("سهم بن از کارمزد باید بین ۰ تا ۱۰۰ باشد.");
            var ok = await _repo.UpdateSettingsAsync(dto, userId);
            if (!ok) return StoreBillingActionResult.Fail("ذخیره تنظیمات ناموفق بود.");
            await _repo.InsertAuditAsync(userId, null, "Settings", 1, "Update",
                $"Rate={dto.BaseCommissionPercent};Rebate={dto.VoucherRebatePercent};Voucher={dto.VoucherEnabled};Due={dto.PaymentDueDays}");
            return StoreBillingActionResult.Success(1, "تنظیمات ذخیره شد.");
        }

        public Task<List<StoreBillingServiceTypeDto>> ListServiceTypesAsync() => _repo.ListServiceTypesAsync();

        public async Task<StoreBillingActionResult> UpsertServiceTypeAsync(StoreBillingServiceTypeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Code) || string.IsNullOrWhiteSpace(dto.Name))
                return StoreBillingActionResult.Fail("کد و نام خدمت الزامی است.");
            var id = await _repo.UpsertServiceTypeAsync(dto);
            if (id == -1) return StoreBillingActionResult.Fail("کد خدمت تکراری است.");
            if (id <= 0) return StoreBillingActionResult.Fail("ذخیره خدمت ناموفق بود.");
            return StoreBillingActionResult.Success(id);
        }

        public Task<List<StoreBillingOverdueRuleDto>> ListOverdueRulesAsync() => _repo.ListOverdueRulesAsync();

        public async Task<StoreBillingActionResult> UpsertOverdueRuleAsync(StoreBillingOverdueRuleDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ActionCode))
                return StoreBillingActionResult.Fail("کد اقدام الزامی است.");
            var id = await _repo.UpsertOverdueRuleAsync(dto);
            return id > 0 ? StoreBillingActionResult.Success(id) : StoreBillingActionResult.Fail("ذخیره قانون ناموفق بود.");
        }

        public Task<bool> DeleteOverdueRuleAsync(int id) => _repo.DeleteOverdueRuleAsync(id);
    }

    public class StoreBillingCatalogService : IStoreBillingCatalogService
    {
        private readonly IStoreBillingRepository _repo;
        private readonly IDataScopeResolver _scope;

        public StoreBillingCatalogService(IStoreBillingRepository repo, IDataScopeResolver scope = null)
        {
            _repo = repo;
            _scope = scope;
        }

        public Task<List<StoreBillingPackageDto>> ListPackagesAsync() => _repo.ListPackagesAsync();

        public async Task<StoreBillingActionResult> UpsertPackageAsync(StoreBillingPackageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name)) return StoreBillingActionResult.Fail("نام پکیج الزامی است.");
            var id = await _repo.UpsertPackageAsync(dto);
            return id > 0 ? StoreBillingActionResult.Success(id) : StoreBillingActionResult.Fail("ذخیره پکیج ناموفق بود.");
        }

        public Task<List<StoreBillingContractDto>> ListContractsAsync(int? storeId = null) => _repo.ListContractsAsync(storeId);

        public async Task<List<StoreBillingContractDto>> ListContractsForUserAsync(int userId, int? storeId = null, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _repo.ListContractsAsync(storeId);
            var query = await _scope.BuildQueryAsync(userId, "Finance.View", siteId);
            return await _repo.ListContractsScopedAsync(query, storeId);
        }

        public async Task<StoreBillingActionResult> UpsertContractAsync(StoreBillingContractDto dto)
        {
            if (dto == null || dto.StoreId <= 0 || dto.PackageId <= 0)
                return StoreBillingActionResult.Fail("فروشگاه و پکیج الزامی است.");
            var id = await _repo.UpsertContractAsync(dto);
            return id > 0 ? StoreBillingActionResult.Success(id) : StoreBillingActionResult.Fail("ذخیره قرارداد ناموفق بود.");
        }

        public Task<List<StoreBillingDiscountDto>> ListDiscountsAsync(int? storeId = null) => _repo.ListDiscountsAsync(storeId);

        public async Task<StoreBillingActionResult> UpsertDiscountAsync(StoreBillingDiscountDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.DiscountType))
                return StoreBillingActionResult.Fail("نوع تخفیف الزامی است.");
            var id = await _repo.UpsertDiscountAsync(dto);
            return id > 0 ? StoreBillingActionResult.Success(id) : StoreBillingActionResult.Fail("ذخیره تخفیف ناموفق بود.");
        }

        public Task<List<StoreBillingAdjustmentDto>> ListAdjustmentsAsync(int? storeId = null, int? invoiceId = null)
            => _repo.ListAdjustmentsAsync(storeId, invoiceId);

        public async Task<StoreBillingActionResult> InsertAdjustmentAsync(int? invoiceId, int? calculationId, int storeId, long amount, string reason, int? userId)
        {
            var id = await _repo.InsertAdjustmentAsync(invoiceId, calculationId, storeId, amount, reason, userId);
            if (id == -3) return StoreBillingActionResult.Fail("مبلغ تعدیل صفر است.");
            return id > 0 ? StoreBillingActionResult.Success(id) : StoreBillingActionResult.Fail("ثبت تعدیل ناموفق بود.");
        }
    }
}
