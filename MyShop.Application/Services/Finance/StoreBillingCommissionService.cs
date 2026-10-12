using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Finance;
using MyShop.Application.Interfaces.Settings;
using MyShop.Core.Models.Finance;

namespace MyShop.Application.Services.Finance
{
    public class StoreBillingCommissionService : IStoreBillingCommissionService
    {
        private readonly IStoreBillingRepository _repo;
        private readonly IStoreVoucherWalletRepository _wallet;
        private readonly ISiteAdminSettingsService _siteSettings;
        private readonly IOrderRepository _orders;

        public StoreBillingCommissionService(
            IStoreBillingRepository repo,
            IStoreVoucherWalletRepository wallet = null,
            ISiteAdminSettingsService siteSettings = null,
            IOrderRepository orders = null)
        {
            _repo = repo;
            _wallet = wallet;
            _siteSettings = siteSettings;
            _orders = orders;
        }

        public async Task TryCalculateForOrderAsync(int orderId)
        {
            if (orderId <= 0) return;
            try
            {
                var settings = await _repo.GetSettingsAsync();
                if (settings == null || !settings.IsEnabled) return;

                var elig = await _repo.ResolveEligibilityAsync(orderId);
                if (elig == null || !elig.IsEligible || !elig.StoreId.HasValue) return;

                var contract = await _repo.GetActiveContractAsync(elig.StoreId.Value, DateTime.Today);
                var rate = contract != null ? contract.EffectiveRate : settings.BaseCommissionPercent;
                var due = contract != null ? contract.EffectiveDueDays : settings.PaymentDueDays;
                var method = settings.CalculationMethod ?? StoreBillingConstants.CalculationMethod.Percent;
                var fixedAmt = settings.FixedAmount;

                long commission = Compute(elig.BaseAmount, rate, method, fixedAmt);
                if (commission < 0) commission = 0;

                var dto = new StoreBillingCalculationDto
                {
                    OrderId = orderId,
                    StoreId = elig.StoreId.Value,
                    PurchaseAmount = elig.PurchaseAmount,
                    SelectedServiceTypeId = elig.SelectedServiceTypeId,
                    SelectedServiceCode = elig.SelectedServiceCode,
                    BaseAmount = elig.BaseAmount,
                    CommissionRate = rate,
                    CommissionAmount = commission,
                    CalculationRule = StoreBillingConstants.CalculationRuleHighestBase,
                    CalculationMethod = method,
                    FixedAmount = fixedAmt,
                    PackageId = contract?.PackageId,
                    ContractId = contract?.Id,
                    PeriodKind = settings.PeriodKind,
                    PaymentDueDays = due
                };
                var calcId = await _repo.UpsertCalculationAsync(dto);

                if (settings.VoucherEnabled && _wallet != null && commission > 0 && calcId > 0)
                {
                    var rebatePct = settings.VoucherRebatePercent;
                    if (rebatePct < 0) rebatePct = 0;
                    if (rebatePct > 100) rebatePct = 100;
                    var rebate = (long)Math.Round((double)commission * (double)rebatePct / 100.0, MidpointRounding.AwayFromZero);
                    if (rebate > 0)
                    {
                        var days = 90;
                        try
                        {
                            var order = _orders != null ? await _orders.GetByIdAsync(orderId) : null;
                            var siteId = order?.SiteId ?? 0;
                            if (_siteSettings != null && siteId > 0)
                            {
                                var site = await _siteSettings.GetSettingsAsync(siteId);
                                if (site != null && site.VoucherCreditExpireDays >= 1)
                                    days = site.VoucherCreditExpireDays;
                            }
                        }
                        catch { }
                        await _wallet.CreditRebateAsync(
                            elig.StoreId.Value, rebate, rate, rebatePct,
                            DateTime.Now.AddDays(days), orderId, calcId, null);
                    }
                }
            }
            catch
            {
                /* صورتحساب نباید تأیید سفارش را متوقف کند */
            }
        }

        public async Task TryCancelForOrderAsync(int orderId)
        {
            if (orderId <= 0) return;
            try
            {
                var existing = await _repo.GetCalculationByOrderAsync(orderId);
                if (existing == null) return;
                if (_wallet != null && existing.Id > 0)
                {
                    try { await _wallet.ReverseByCalculationAsync(existing.Id); } catch { }
                }
                if (string.Equals(existing.Status, StoreBillingConstants.CommissionStatus.IncludedInInvoice, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(existing.Status, StoreBillingConstants.CommissionStatus.Adjusted, StringComparison.OrdinalIgnoreCase))
                {
                    await _repo.SetCalculationStatusAsync(existing.Id, null, StoreBillingConstants.CommissionStatus.Adjusted);
                    if (existing.InvoiceId.HasValue)
                    {
                        await _repo.InsertAdjustmentAsync(
                            existing.InvoiceId, existing.Id, existing.StoreId,
                            -existing.CommissionAmount,
                            "ابطال سفارش پس از صدور فاکتور",
                            null);
                    }
                    return;
                }
                await _repo.SetCalculationStatusAsync(null, orderId, StoreBillingConstants.CommissionStatus.Cancelled);
            }
            catch { }
        }

        public Task<List<StoreBillingCalculationDto>> ListAsync(int? storeId = null, string status = null)
            => _repo.ListCalculationsAsync(storeId, status);

        internal static long Compute(long baseAmount, decimal rate, string method, long fixedAmount)
        {
            if (string.Equals(method, StoreBillingConstants.CalculationMethod.Fixed, StringComparison.OrdinalIgnoreCase))
                return fixedAmount;
            var pct = (long)Math.Round((double)baseAmount * (double)rate / 100.0, MidpointRounding.AwayFromZero);
            if (string.Equals(method, StoreBillingConstants.CalculationMethod.PercentPlusFixed, StringComparison.OrdinalIgnoreCase))
                return pct + fixedAmount;
            return pct;
        }
    }
}
