using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;

namespace MyShop.Application.Interfaces
{
    public interface IOrderService
    {
        /// <summary>ثبت سفارش با کوپن (وضعیت: در انتظار تایید)</summary>
        Task<(bool Success, string Message, int OrderId)> CreateOrderWithCouponAsync(int userId, int couponUsageId, int? storeId, long totalAmount, long discountAmount, long walletAmountApplied = 0);

        /// <summary>ثبت سفارش با جعبه‌شانس (وضعیت: در انتظار تایید)</summary>
        Task<(bool Success, string Message, int OrderId)> CreateOrderWithChanceBoxAsync(int userId, int chanceBoxCodeUsageId, int storeId, long purchaseAmount, long walletAmountApplied = 0);

        /// <summary>ثبت سفارش عادی بدون کوپن/جعبه‌شانس (وضعیت: در انتظار تایید)</summary>
        Task<(bool Success, string Message, int OrderId)> CreateOrderNormalAsync(int userId, int storeId, long totalAmount, string? notes = null, long walletAmountApplied = 0);

        Task<bool> UpdateStatusAsync(int orderId, byte status);
        Task<Order?> GetByIdAsync(int orderId);
        Task<List<Order>> GetListAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status, int pageSize, int pageIndex);
        Task<int> CountAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status);
        Task<List<Order>> GetPendingOrdersAsync(int? storeId = null);
        Task<List<Order>> GetPendingOrdersForUserAsync(int userId, int? storeId = null, int? siteId = null);
        Task<List<Order>> GetListForUserAsync(int userId, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? filterUserId, byte? status, int pageSize, int pageIndex, int? siteId = null);
        Task<int> CountForUserAsync(int userId, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? filterUserId, byte? status, int? siteId = null);
        Task<List<Order>> GetListByStoreOwnerAsync(int storeOwnerUserId, DateTime? fromDate, DateTime? toDate, byte? orderType, byte? status, string? searchType, string? searchText, int pageSize, int pageIndex);
    }
}
