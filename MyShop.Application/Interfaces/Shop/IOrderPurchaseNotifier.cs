using System.Threading.Tasks;
using MyShop.Core.Models;

namespace MyShop.Application.Interfaces
{
    /// <summary>اعلان خرید جدید (مثلاً پیام بله به ادمین/ناظر).</summary>
    public interface IOrderPurchaseNotifier
    {
        Task NotifyNewOrderAsync(Order order);
    }
}
