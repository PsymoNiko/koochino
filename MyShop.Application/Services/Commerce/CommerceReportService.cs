using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceReportService : ICommerceReportService
    {
        private readonly ICommerceReportRepository _repo;
        private readonly ICommerceFeatureFlags _flags;
        private readonly IStoreService _stores;
        private readonly IDataScopeResolver _scope;

        public CommerceReportService(
            ICommerceReportRepository repo,
            ICommerceFeatureFlags flags = null,
            IStoreService stores = null,
            IDataScopeResolver scope = null)
        {
            _repo = repo;
            _flags = flags;
            _stores = stores;
            _scope = scope;
        }

        public bool IsEnabled =>
            _flags != null && _flags.IsEnabled(CommerceConstants.Features.Reports);

        public async Task<List<CommerceStoreSalesRow>> GetAdminStoreSalesAsync(
            int? siteId, DateTime fromInclusive, DateTime toExclusive)
        {
            if (!IsEnabled) return new List<CommerceStoreSalesRow>();
            try
            {
                return await _repo.GetStoreSalesAsync(siteId, fromInclusive, toExclusive, null)
                       ?? new List<CommerceStoreSalesRow>();
            }
            catch
            {
                return new List<CommerceStoreSalesRow>();
            }
        }

        public async Task<List<CommerceStoreSalesRow>> GetAdminStoreSalesForUserAsync(
            int userId, int? siteId, DateTime fromInclusive, DateTime toExclusive)
        {
            var rows = await GetAdminStoreSalesAsync(siteId, fromInclusive, toExclusive);
            if (_scope == null || _stores == null || userId <= 0 || rows.Count == 0)
                return rows;
            var query = await _scope.BuildQueryAsync(userId, "Reports.View", siteId);
            if (string.Equals(query.Kind, "All", StringComparison.OrdinalIgnoreCase))
                return rows;
            var allowed = (await _stores.GetAllForUserAsync(userId, siteId) ?? new List<Store>())
                .Select(s => s.StoreID > 0 ? s.StoreID : s.Id)
                .Where(id => id > 0)
                .ToHashSet();
            return rows.Where(r => allowed.Contains(r.StoreId)).ToList();
        }

        public async Task<List<CommerceStoreSalesRow>> GetSellerStoreSalesAsync(
            int sellerUserId, int? siteId, DateTime fromInclusive, DateTime toExclusive)
        {
            if (!IsEnabled || sellerUserId <= 0)
                return new List<CommerceStoreSalesRow>();
            var owned = await OwnedStoreIdsAsync(sellerUserId);
            if (owned.Count == 0)
                return new List<CommerceStoreSalesRow>();
            try
            {
                var rows = await _repo.GetStoreSalesAsync(siteId, fromInclusive, toExclusive, owned)
                           ?? new List<CommerceStoreSalesRow>();
                return CommerceReportMath.FilterOwned(rows, owned.ToHashSet());
            }
            catch
            {
                return new List<CommerceStoreSalesRow>();
            }
        }

        async Task<List<int>> OwnedStoreIdsAsync(int sellerUserId)
        {
            if (_stores == null)
                return new List<int>();
            var owned = await _stores.GetByUserIdAsync(sellerUserId) ?? new List<Store>();
            return owned
                .Select(s => s.StoreID != 0 ? s.StoreID : s.Id)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }
    }
}
