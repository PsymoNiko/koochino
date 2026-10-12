using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommerceTaxRepository
    {
        Task<List<CommerceTaxRate>> ListAsync(int? siteId);
        Task<CommerceTaxRate> GetActiveForSiteAsync(int? siteId);
        Task<int> UpsertAsync(CommerceTaxRate m);
        Task DeleteAsync(int id);
    }

    public interface ICommerceStoreShippingRepository
    {
        Task<List<CommerceStoreShippingRate>> ListByMethodAsync(int shippingMethodId);
        Task UpsertAsync(CommerceStoreShippingRate m);
        Task DeleteAsync(int id);
    }
}
