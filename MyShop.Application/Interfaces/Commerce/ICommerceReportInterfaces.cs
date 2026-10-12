using MyShop.Core.Models.Commerce;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommerceReportRepository
    {
        Task<List<CommerceStoreSalesRow>> GetStoreSalesAsync(
            int? siteId, DateTime fromInclusive, DateTime toExclusive, IReadOnlyList<int> storeIds);
    }

    public interface ICommerceReportService
    {
        bool IsEnabled { get; }
        Task<List<CommerceStoreSalesRow>> GetAdminStoreSalesAsync(int? siteId, DateTime fromInclusive, DateTime toExclusive);
        Task<List<CommerceStoreSalesRow>> GetAdminStoreSalesForUserAsync(int userId, int? siteId, DateTime fromInclusive, DateTime toExclusive);
        Task<List<CommerceStoreSalesRow>> GetSellerStoreSalesAsync(int sellerUserId, int? siteId, DateTime fromInclusive, DateTime toExclusive);
    }
}
