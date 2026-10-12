using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommercePricingService
    {
        CommercePriceQuote QuoteProduct(ProductDto product);
        CommercePriceQuote QuoteVariation(ProductDto product, CommerceProductVariation variation);
    }

    public interface ICommerceVariationCartRepository
    {
        Task<CommerceVariationCartItem> GetAsync(int cartId, int variationId);
        Task<List<CommerceVariationCartItem>> ListAsync(int cartId);
        Task UpsertAsync(CommerceVariationCartItem item);
        Task RemoveAsync(int cartId, int variationId);
        Task ClearAsync(int cartId);
    }

    public interface ICommerceVariationCartService
    {
        Task<List<CommerceVariationCartItem>> ListAsync(int? siteId, int? userId, string guestKey);
        Task<CommerceVariationCartItem> AddAsync(int? siteId, int? userId, string guestKey, int variationId, int quantity);
        Task SetQuantityAsync(int? siteId, int? userId, string guestKey, int variationId, int quantity);
        Task RemoveAsync(int? siteId, int? userId, string guestKey, int variationId);
    }
}
