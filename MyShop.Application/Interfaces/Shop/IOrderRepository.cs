using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;
using MyShop.Core.Models.Security;

namespace MyShop.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<string> GetNextOrderNumberAsync();
        Task<int> InsertAsync(Order order);
        Task<bool> UpdateStatusAsync(int orderId, byte status);
        Task<Order?> GetByIdAsync(int orderId);
        Task<List<Order>> GetListAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status, int pageSize, int pageIndex);
        Task<int> CountAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status);
        Task<List<Order>> GetPendingAsync(int? storeId);
        /// <summary>لیست سفارشات فروشنده با جستجو (SearchType: StoreName یا CustomerName)</summary>
        Task<List<Order>> GetListByStoreOwnerAsync(int storeOwnerUserId, DateTime? fromDate, DateTime? toDate, byte? orderType, byte? status, string? searchType, string? searchText, int pageSize, int pageIndex);
        Task<List<Order>> GetListScopedAsync(DataScopeQuery scope, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status, int pageSize, int pageIndex);
        Task<int> CountScopedAsync(DataScopeQuery scope, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status);
        Task<List<Order>> GetPendingScopedAsync(DataScopeQuery scope, int? storeId);
    }
}
