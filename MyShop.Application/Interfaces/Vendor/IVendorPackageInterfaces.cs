using MyShop.Core.Models.Vendor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Vendor
{
    public interface IVendorPackageRepository
    {
        Task<List<VendorPackage>> ListAsync(bool activeOnly = false);
        Task<VendorPackage> GetAsync(int id);
        Task<int> UpsertAsync(VendorPackage package);
        Task DeleteAsync(int id);
        Task<long> CreateSubscriptionAsync(VendorSubscription subscription);
        Task UpdateSubscriptionPaymentAsync(long id, string status, string paymentRef, string gateway);
    }

    public interface IVendorPackageService
    {
        Task<List<VendorPackage>> ListAsync(bool activeOnly = false);
        Task<VendorPackage> GetAsync(int id);
        Task<int> SaveAsync(VendorPackage package);
        Task DeleteAsync(int id);
        Task<VendorSubscription> StartPurchaseAsync(int userId, int packageId, string gateway);
    }
}
