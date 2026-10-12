using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Finance;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models.Finance;

namespace MyShop.Application.Services.Finance
{
    public class StoreBillingEventSink : IStoreBillingEventSink
    {
        private readonly IStoreBillingRepository _repo;
        private readonly ISmsSender _sms;

        public StoreBillingEventSink(IStoreBillingRepository repo, ISmsSender sms = null)
        {
            _repo = repo;
            _sms = sms;
        }

        public async Task PublishAsync(string eventCode, int storeId, int? invoiceId, string details)
        {
            await _repo.InsertAuditAsync(null, storeId, "Event", invoiceId, eventCode, details);
            // رتبه/دیده‌شدن فروشگاه اینجا تغییر نمی‌کند؛ ماژول Store بعداً از همین لاگ/رویداد استفاده می‌کند.
            try
            {
                if (_sms == null) return;
                if (eventCode == StoreBillingConstants.EventCode.InvoiceOverdue
                    || eventCode == StoreBillingConstants.EventCode.StoreSuspended)
                {
                    /* گیرنده از لاگ قابل استخراج است؛ ارسال SMS اختیاری و بدون شماره اجباری */
                }
            }
            catch { }
        }
    }

    public class StoreBillingOverdueService : IStoreBillingOverdueService
    {
        private readonly IStoreBillingRepository _repo;
        private readonly IStoreBillingEventSink _events;

        public StoreBillingOverdueService(IStoreBillingRepository repo, IStoreBillingEventSink events)
        {
            _repo = repo;
            _events = events;
        }

        public async Task<int> ProcessAsync()
        {
            var invoices = await _repo.MarkOverdueAsync();
            var rules = (await _repo.ListOverdueRulesAsync())?.Where(r => r.IsActive).OrderBy(r => r.SortOrder).ToList()
                        ?? new System.Collections.Generic.List<StoreBillingOverdueRuleDto>();
            var n = 0;
            foreach (var inv in invoices)
            {
                var rule = rules.FirstOrDefault(r =>
                    inv.OverdueDays >= r.MinOverdueDays
                    && (!r.MaxOverdueDays.HasValue || inv.OverdueDays <= r.MaxOverdueDays.Value));
                var action = rule?.ActionCode ?? StoreBillingConstants.OverdueAction.Warning;
                var evt = action == StoreBillingConstants.OverdueAction.Suspend
                    ? StoreBillingConstants.EventCode.StoreSuspended
                    : StoreBillingConstants.EventCode.InvoiceOverdue;
                await _events.PublishAsync(evt, inv.StoreId, inv.Id,
                    $"Invoice={inv.InvoiceNumber};Days={inv.OverdueDays};Action={action}");
                n++;
            }
            return n;
        }
    }
}
