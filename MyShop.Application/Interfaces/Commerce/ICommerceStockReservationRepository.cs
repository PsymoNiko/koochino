using MyShop.Core.Models.Commerce;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommerceStockReservationRepository
    {
        Task ExpireStaleAsync();
        /// <summary>Returns null on success, otherwise a Persian error. Does not decrement on-hand stock.</summary>
        Task<string> TryReserveLinesAsync(int cartId, IReadOnlyList<OrderItem> items, DateTime expiresAt);
        Task BindOrderAsync(int cartId, int orderId);
        Task ReleaseByCartAsync(int cartId);
        Task ReleaseByOrderAsync(int orderId);
        Task<bool> HasActiveAsync(int orderId);
        Task<bool> HasCommittedAsync(int orderId);
        Task MarkCommittedAsync(int orderId);
    }
}
