using MyShop.Core.Models.Commerce;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommerceSettlementRepository
    {
        Task<List<CommerceCommissionRate>> ListRatesAsync();
        Task UpsertRateAsync(int? siteId, int? storeId, decimal ratePercent);
        Task<List<CommerceSettlement>> ListSettlementsAsync(IReadOnlyList<int> storeIds);
        Task<CommerceSettlement> GetByPeriodAsync(int storeId, DateTime periodFrom, DateTime periodTo);
        Task<int> InsertAsync(CommerceSettlement row);
        Task VoidAsync(int id);
    }

    public interface ICommerceSettlementService
    {
        bool IsEnabled { get; }
        Task<List<CommerceCommissionRate>> ListRatesAsync();
        Task SaveRateAsync(int? siteId, int? storeId, decimal ratePercent);
        Task<List<CommerceSettlementPreview>> PreviewAsync(int? siteId, DateTime fromInclusive, DateTime toExclusive);
        Task<int> SettleStoreAsync(int storeId, int? siteId, DateTime fromInclusive, DateTime toExclusive, int? userId, string notes);
        Task VoidAsync(int settlementId);
        Task<List<CommerceSettlement>> ListAdminAsync();
        Task<List<CommerceSettlement>> ListSellerAsync(int sellerUserId);
    }
}
