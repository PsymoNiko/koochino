using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Finance;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Finance;

namespace MyShop.Application.Services.Finance
{
    public class StoreBillingInvoiceService : IStoreBillingInvoiceService
    {
        private readonly IStoreBillingRepository _repo;
        private readonly IStoreBillingEventSink _events;
        private readonly IStoreVoucherWalletRepository _voucher;
        private readonly IDataScopeResolver _scope;

        public StoreBillingInvoiceService(IStoreBillingRepository repo, IStoreBillingEventSink events = null, IStoreVoucherWalletRepository voucher = null, IDataScopeResolver scope = null)
        {
            _repo = repo;
            _events = events;
            _voucher = voucher;
            _scope = scope;
        }

        public async Task<StoreBillingActionResult> ClosePeriodAsync(int storeId, DateTime start, DateTime end, int? userId)
        {
            if (storeId <= 0) return StoreBillingActionResult.Fail("فروشگاه الزامی است.");
            if (end.Date < start.Date) return StoreBillingActionResult.Fail("بازه دوره نامعتبر است.");

            var settings = await _repo.GetSettingsAsync() ?? new StoreBillingSettingsDto();
            var contract = await _repo.GetActiveContractAsync(storeId, end.Date);
            var dueDays = contract != null ? contract.EffectiveDueDays : settings.PaymentDueDays;
            var rate = contract != null ? contract.EffectiveRate : settings.BaseCommissionPercent;

            var (periodId, invoiceId) = await _repo.ClosePeriodAsync(storeId, start.Date, end.Date, settings.MinInvoiceAmount, dueDays, rate);
            if (periodId == -1) return StoreBillingActionResult.Fail("این دوره قبلاً بسته شده است.");
            if (periodId <= 0) return StoreBillingActionResult.Fail("بستن دوره ناموفق بود.");

            if (invoiceId > 0)
            {
                await ApplyDiscountsAsync(storeId, invoiceId, end.Date, userId);
                try
                {
                    if (_voucher != null)
                    {
                        var credit = await _voucher.ApplySettlementsToInvoiceAsync(storeId, invoiceId);
                        if (credit > 0)
                            await _repo.InsertAdjustmentAsync(invoiceId, null, storeId, -credit, "تهاتر کیف پول (طلب فروشگاه مصرف‌کننده)", userId);
                        try
                        {
                            var (rebate, txn) = await _voucher.RebateInPeriodAsync(storeId, start.Date, end.Date);
                            if (rebate > 0)
                                await _voucher.InsertInvoiceItemAsync(invoiceId,
                                    $"از این کارمزد {rebate:N0} تومان به کیف پول رفت (فاکتور همان کل کارمزد است)",
                                    txn, 0, settings.VoucherRebatePercent, 0);
                        }
                        catch { }
                    }
                }
                catch { }
                await _repo.InsertAuditAsync(userId, storeId, "Invoice", invoiceId, "Issue", $"Period={periodId}");
            }

            return StoreBillingActionResult.Success(invoiceId > 0 ? invoiceId : periodId,
                invoiceId > 0 ? "فاکتور صادر شد." : "دوره بسته شد؛ مبلغ به حداقل فاکتور نرسید.");
        }

        public async Task<StoreBillingActionResult> CloseAllPendingAsync(DateTime start, DateTime end, int? userId)
        {
            var stores = await _repo.StoresWithPendingCalcsAsync(start, end);
            var ok = 0;
            foreach (var s in stores)
            {
                var r = await ClosePeriodAsync(s.StoreId, start, end, userId);
                if (r.Ok) ok++;
            }
            return ok > 0
                ? StoreBillingActionResult.Success(ok, $"{ok} دوره بسته شد.")
                : StoreBillingActionResult.Fail("محاسبهٔ بازی برای این بازه نیست.");
        }

        public Task<List<StoreBillingPeriodDto>> ListPeriodsAsync(int? storeId = null) => _repo.ListPeriodsAsync(storeId);
        public Task<List<StoreBillingInvoiceDto>> ListInvoicesAsync(int? storeId = null, string status = null, int? ownerUserId = null)
            => _repo.ListInvoicesAsync(storeId, status, ownerUserId);

        public async Task<List<StoreBillingInvoiceDto>> ListInvoicesForUserAsync(int userId, string status = null, int? storeId = null, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _repo.ListInvoicesAsync(storeId, status);
            var query = await _scope.BuildQueryAsync(userId, "Finance.View", siteId);
            return await _repo.ListInvoicesScopedAsync(query, storeId, status);
        }

        public Task<StoreBillingInvoiceDto> GetInvoiceAsync(int id) => _repo.GetInvoiceAsync(id);

        public async Task<StoreBillingActionResult> RecordPaymentAsync(int invoiceId, long amount, string method, string reference, string notes, int? userId)
        {
            var id = await _repo.InsertPaymentAsync(invoiceId, amount, DateTime.Now, method, reference, notes, userId);
            if (id == -3) return StoreBillingActionResult.Fail("فاکتور باطل شده است.");
            if (id == -5) return StoreBillingActionResult.Fail("مانده‌ای برای پرداخت نیست.");
            if (id <= 0) return StoreBillingActionResult.Fail("ثبت پرداخت ناموفق بود.");

            var inv = await _repo.GetInvoiceAsync(invoiceId);
            if (inv != null && inv.RemainingAmount <= 0 && _events != null)
                await _events.PublishAsync(StoreBillingConstants.EventCode.PaymentCompleted, inv.StoreId, inv.Id, inv.InvoiceNumber);

            return StoreBillingActionResult.Success(id, "پرداخت ثبت شد.");
        }

        public Task<List<StoreBillingPaymentDto>> ListPaymentsAsync(int? invoiceId = null, int? storeId = null)
            => _repo.ListPaymentsAsync(invoiceId, storeId);

        public async Task<List<StoreBillingPaymentDto>> ListPaymentsForUserAsync(int userId, int? invoiceId = null, int? storeId = null, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _repo.ListPaymentsAsync(invoiceId, storeId);
            var query = await _scope.BuildQueryAsync(userId, "Finance.View", siteId);
            return await _repo.ListPaymentsScopedAsync(query, invoiceId, storeId);
        }

        public Task<StoreBillingDashboardDto> GetDashboardAsync() => _repo.GetDashboardAsync();
        public Task<List<StoreBillingStoreReportDto>> ReportByStoreAsync() => _repo.ReportByStoreAsync();

        private async Task ApplyDiscountsAsync(int storeId, int invoiceId, DateTime onDate, int? userId)
        {
            var inv = await _repo.GetInvoiceAsync(invoiceId);
            if (inv == null || inv.Subtotal <= 0) return;
            var discounts = await _repo.ActiveDiscountsAsync(storeId, onDate);
            long totalDisc = 0;
            foreach (var d in discounts)
            {
                long amt = 0;
                if (string.Equals(d.DiscountType, StoreBillingConstants.DiscountType.Percent, StringComparison.OrdinalIgnoreCase))
                    amt = (long)Math.Round((double)inv.Subtotal * (double)d.DiscountValue / 100.0, MidpointRounding.AwayFromZero);
                else
                    amt = (long)d.DiscountValue;
                if (amt > 0) totalDisc += amt;
            }
            if (totalDisc <= 0) return;
            if (totalDisc > inv.Subtotal) totalDisc = inv.Subtotal;
            await _repo.InsertAdjustmentAsync(invoiceId, null, storeId, -totalDisc, "تخفیف کارمزد دوره", userId);
        }
    }
}
