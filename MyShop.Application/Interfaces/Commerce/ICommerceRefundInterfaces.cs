using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommerceRefundRepository
    {
        Task<List<CommerceRefund>> ListByOrderAsync(int orderId);
        Task<CommerceRefund> GetAsync(int id);
        Task<int> InsertAsync(CommerceRefund m);
        Task UpdateStatusAsync(int id, byte status);
        Task<CommercePayment> GetLatestPaymentAsync(int orderId);
    }

    public interface ICommerceRefundService
    {
        Task<List<CommerceRefund>> ListByOrderAsync(int orderId);
        Task<int> RequestAsync(int orderId, long amount, string reason, int? userId);
        Task CompleteAsync(int refundId, int? userId);
        Task RejectAsync(int refundId, int? userId);
    }
}
