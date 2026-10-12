using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceRefundService : ICommerceRefundService
    {
        private readonly ICommerceRefundRepository _repo;
        private readonly ICommerceOrderRepository _orders;
        private readonly ICommerceFeatureFlags _flags;

        public CommerceRefundService(
            ICommerceRefundRepository repo,
            ICommerceOrderRepository orders,
            ICommerceFeatureFlags flags = null)
        {
            _repo = repo;
            _orders = orders;
            _flags = flags;
        }

        bool On() => _flags != null && _flags.IsEnabled(CommerceConstants.Features.Refunds);

        public async Task<List<CommerceRefund>> ListByOrderAsync(int orderId)
        {
            if (!On()) return new List<CommerceRefund>();
            try { return await _repo.ListByOrderAsync(orderId) ?? new List<CommerceRefund>(); }
            catch { return new List<CommerceRefund>(); }
        }

        public async Task<int> RequestAsync(int orderId, long amount, string reason, int? userId)
        {
            if (!On()) throw new InvalidOperationException("استرداد فعال نیست.");
            if (amount <= 0) throw new InvalidOperationException("مبلغ استرداد نامعتبر است.");
            var order = await _orders.GetDetailAsync(orderId);
            if (order == null || !order.IsCommerce)
                throw new InvalidOperationException("سفارش یافت نشد.");
            if (order.FulfillmentStatus == CommerceFulfillmentStatus.Cancelled)
                throw new InvalidOperationException("سفارش لغوشده قابل استرداد نیست.");
            if (order.PaymentStatus != CommercePaymentStatus.Paid
                && order.PaymentStatus != CommercePaymentStatus.Unpaid)
                throw new InvalidOperationException("این سفارش برای استرداد واجد شرایط نیست.");

            System.Collections.Generic.List<CommerceRefund> existing;
            try { existing = await _repo.ListByOrderAsync(orderId) ?? new(); }
            catch { throw new InvalidOperationException("استرداد روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 10 را اعمال کنید."); }

            var held = existing
                .Where(r => r.Status == CommerceRefundStatus.Requested || r.Status == CommerceRefundStatus.Completed)
                .Sum(r => r.Amount);
            var remaining = order.FinalAmount - held;
            if (amount > remaining)
                throw new InvalidOperationException($"مبلغ استرداد از مانده ({remaining:N0} ریال) بیشتر است.");

            CommercePayment pay = null;
            try { pay = await _repo.GetLatestPaymentAsync(orderId); } catch { }

            return await _repo.InsertAsync(new CommerceRefund
            {
                OrderId = orderId,
                PaymentId = pay?.Id,
                Amount = amount,
                Reason = (reason ?? "").Trim(),
                Status = CommerceRefundStatus.Requested,
                CreatedByUserId = userId
            });
        }

        public async Task CompleteAsync(int refundId, int? userId)
        {
            if (!On()) throw new InvalidOperationException("استرداد فعال نیست.");
            var refund = await _repo.GetAsync(refundId);
            if (refund == null) throw new InvalidOperationException("درخواست استرداد یافت نشد.");
            if (refund.Status == CommerceRefundStatus.Completed)
                throw new InvalidOperationException("این استرداد قبلاً انجام شده است.");
            if (refund.Status != CommerceRefundStatus.Requested)
                throw new InvalidOperationException("این درخواست قابل تکمیل نیست.");

            await _repo.UpdateStatusAsync(refundId, CommerceRefundStatus.Completed);

            var order = await _orders.GetDetailAsync(refund.OrderId);
            if (order == null) return;
            var all = await _repo.ListByOrderAsync(refund.OrderId) ?? new();
            var completed = all.Where(r => r.Status == CommerceRefundStatus.Completed).Sum(r => r.Amount);
            if (completed < order.FinalAmount) return;

            await _orders.UpdatePaymentStatusAsync(order.Id, CommercePaymentStatus.Refunded, CommerceFulfillmentStatus.Refunded);
            if (refund.PaymentId.HasValue)
            {
                var pay = await _orders.GetPaymentByIdAsync(refund.PaymentId.Value);
                if (pay != null && pay.Status != CommercePaymentStatus.Refunded)
                {
                    pay.Status = CommercePaymentStatus.Refunded;
                    await _orders.UpdatePaymentAsync(pay);
                }
            }
        }

        public async Task RejectAsync(int refundId, int? userId)
        {
            if (!On()) throw new InvalidOperationException("استرداد فعال نیست.");
            var refund = await _repo.GetAsync(refundId);
            if (refund == null) throw new InvalidOperationException("درخواست استرداد یافت نشد.");
            if (refund.Status != CommerceRefundStatus.Requested)
                throw new InvalidOperationException("فقط درخواست باز قابل رد است.");
            await _repo.UpdateStatusAsync(refundId, CommerceRefundStatus.Rejected);
        }
    }
}
