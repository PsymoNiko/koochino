using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommercePromotionRepository
    {
        Task<List<CommercePromotion>> ListAllAsync(int? siteId);
        Task<List<CommercePromotion>> ListActiveAsync(int? siteId, int? storeId);
        Task<CommercePromotion> GetByCodeAsync(string code, int? siteId);
        Task<CommercePromotion> GetAsync(int id);
        Task<int> InsertAsync(CommercePromotion m);
        Task UpdateAsync(CommercePromotion m);
        Task DeleteAsync(int id);
        Task IncrementUsageAsync(int promotionId);
        Task InsertRedemptionAsync(int promotionId, int orderId, int userId, long amount);
    }

    public interface ICommercePromotionService
    {
        Task<List<CommercePromotion>> ListAsync(int? siteId);
        Task<int> SaveAsync(CommercePromotion m, int? siteId);
        Task DeleteAsync(int id, int? siteId);
        Task<CommercePromotionQuote> QuoteAsync(int? siteId, int? storeId, long subtotal, string promoCode);
        Task RedeemAsync(int promotionId, int orderId, int userId, long amount);
    }
}
