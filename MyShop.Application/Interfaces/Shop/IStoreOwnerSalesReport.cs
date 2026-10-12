using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Shop;

namespace MyShop.Application.Interfaces
{
    public interface IStoreOwnerSalesReportRepository
    {
        Task<StoreOwnerSalesSummaryDto> GetSummaryAsync(StoreOwnerSalesFilter filter);
        Task<List<StoreOwnerSalesDayDto>> GetByDayAsync(StoreOwnerSalesFilter filter);
        Task<List<StoreOwnerStoreSalesDto>> GetByStoreAsync(StoreOwnerSalesFilter filter);
        Task<List<StoreOwnerSalesCustomerDto>> SearchCustomersAsync(int storeOwnerUserId, int? storeId, string searchText);
    }

    public interface IStoreOwnerSalesReportService
    {
        Task<StoreOwnerSalesReportDto> GetAsync(StoreOwnerSalesFilter filter);
        Task<List<StoreOwnerSalesCustomerDto>> SearchCustomersAsync(int storeOwnerUserId, int? storeId, string searchText);
    }
}
