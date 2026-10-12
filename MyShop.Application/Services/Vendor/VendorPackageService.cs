using MyShop.Application.Interfaces.Vendor;
using MyShop.Core.Models.Vendor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Vendor
{
    public class VendorPackageService : IVendorPackageService
    {
        private readonly IVendorPackageRepository _repository;
        public VendorPackageService(IVendorPackageRepository repository) => _repository = repository;

        public Task<List<VendorPackage>> ListAsync(bool activeOnly = false) => _repository.ListAsync(activeOnly);
        public Task<VendorPackage> GetAsync(int id) => _repository.GetAsync(id);

        public Task<int> SaveAsync(VendorPackage package)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (string.IsNullOrWhiteSpace(package.Name)) throw new ArgumentException("نام پکیج الزامی است.");
            if (package.Price < 0) throw new ArgumentException("قیمت نمی‌تواند منفی باشد.");
            if (package.DurationDays < 1) throw new ArgumentException("مدت پکیج باید حداقل یک روز باشد.");
            return _repository.UpsertAsync(package);
        }

        public Task DeleteAsync(int id) => _repository.DeleteAsync(id);

        public async Task<VendorSubscription> StartPurchaseAsync(int userId, int packageId, string gateway)
        {
            var package = await _repository.GetAsync(packageId)
                ?? throw new InvalidOperationException("پکیج یافت نشد.");
            if (!package.IsActive) throw new InvalidOperationException("این پکیج غیرفعال است.");
            var subscription = new VendorSubscription
            {
                UserId = userId,
                PackageId = packageId,
                Status = VendorSubscriptionStatuses.Pending,
                PaymentGateway = string.IsNullOrWhiteSpace(gateway) ? "Mock" : gateway,
                PackageName = package.Name,
                PackagePrice = package.Price
            };
            subscription.Id = await _repository.CreateSubscriptionAsync(subscription);
            return subscription;
        }
    }
}
