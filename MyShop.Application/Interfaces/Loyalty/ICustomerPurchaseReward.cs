using System.Threading.Tasks;
using MyShop.Core.Models.Loyalty;

namespace MyShop.Application.Interfaces.Loyalty
{
    public interface ICustomerPurchaseRewardRepository
    {
        Task<CustomerPurchaseRewardSettings> GetSettingsAsync();
        Task<(bool ok, int resultCode)> UpdateSettingsAsync(CustomerPurchaseRewardSettings settings, int? updatedByUserId);
        Task SetOrderWalletAmountAppliedAsync(int orderId, long walletAmountApplied);
        Task<long?> GetOrderWalletAmountAppliedAsync(int orderId);
        Task<long> ResolveMinBuyAsync(byte orderType, int? couponUsageId, int? chanceBoxCodeUsageId);
        Task<bool> HasCashbackForOrderAsync(int orderId);
        Task<bool> HasWalletDebitForOrderAsync(int orderId);
        Task<bool> HasWalletRefundForOrderAsync(int orderId);
        Task<bool> HasPurchaseRewardPointsAsync(int orderId);
        Task<int?> GetPurchaseRewardPointTransactionIdAsync(int orderId);
    }

    public interface ICustomerPurchaseRewardService
    {
        Task<CustomerPurchaseRewardSettings> GetSettingsAsync();
        Task<(bool ok, string message)> SaveSettingsAsync(CustomerPurchaseRewardSettings settings, int? adminUserId);
        Task TryCreditAsync(int orderId);
        Task TryReverseAsync(int orderId);
        /// <summary>کسر کیف هنگام ثبت سفارش؛ پس از Insert موفق فراخوانی شود.</summary>
        Task<(bool ok, string message)> ApplyWalletDebitForOrderAsync(int orderId, int userId, long walletAmount);
        Task TryRefundWalletApplyAsync(int orderId);
    }
}
