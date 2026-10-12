using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommerceCatalogRepository
    {
        Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false);
        Task<CommerceShippingMethod> GetShippingAsync(int id);
        Task<int> UpsertShippingAsync(CommerceShippingMethod m);
        Task DeleteShippingAsync(int id);

        Task<List<CommercePaymentMethod>> ListPaymentAsync(int? siteId, bool activeOnly = false);
        Task<CommercePaymentMethod> GetPaymentAsync(int id);
        Task<int> UpsertPaymentAsync(CommercePaymentMethod m);
        Task DeletePaymentAsync(int id);

        Task<List<CommerceDeliverySlot>> ListSlotsAsync(int? siteId, bool activeOnly = false);
        Task<CommerceDeliverySlot> GetSlotAsync(int id);
        Task<int> UpsertSlotAsync(CommerceDeliverySlot m);
        Task DeleteSlotAsync(int id);
    }

    public interface ICommerceCartRepository
    {
        Task<CommerceCart> GetOrCreateAsync(int? siteId, int? userId, string guestKey);
        Task<CommerceCart> GetWithItemsAsync(int cartId);
        Task SetItemAsync(int cartId, int productId, string title, long unitPrice, int quantity);
        Task RemoveItemAsync(int cartId, int productId);
        Task ClearAsync(int cartId);
        Task AttachUserAsync(int cartId, int userId);
    }

    public interface ICommerceOrderRepository
    {
        Task<int> CreateCommerceOrderAsync(CommerceOrderDetail order, IEnumerable<OrderItem> items, int? changedByUserId);
        Task<CommerceOrderDetail> GetDetailAsync(int orderId);
        Task<List<CommerceOrderDetail>> ListAsync(int? siteId = null, int? userId = null, int? storeId = null, int top = 200);
        Task<List<CommerceOrderDetail>> ListScopedAsync(DataScopeQuery scope, int? siteId = null, int? userId = null, int? storeId = null, int top = 200)
            => ListAsync(siteId, userId, storeId, top);
        Task<List<CommerceOrderDetail>> ListForStoreIdsAsync(IReadOnlyList<int> storeIds, int top = 200);
        Task UpdateFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId);
        Task UpdatePaymentStatusAsync(int orderId, byte paymentStatus, byte? fulfillmentStatus = null);
        Task<List<OrderStatusHistory>> ListHistoryAsync(int orderId);
        Task<int> InsertPaymentAsync(CommercePayment payment);
        Task UpdatePaymentAsync(CommercePayment payment);
        Task<CommercePayment> GetPaymentByAuthorityAsync(string authority);
        Task<CommercePayment> GetPaymentByIdAsync(int paymentId);
        Task<bool> TryMarkPaymentPaidAsync(int paymentId, string refId, string rawResponse);
        Task<bool> TryBeginPaymentFinalizationAsync(int paymentId);
        Task MarkPaymentFinalizationDoneAsync(int paymentId);
        Task AbandonPaymentFinalizationAsync(int paymentId);
        Task<bool> TryMarkPaymentFailedIfNotPaidAsync(int paymentId, string rawResponse);
        Task InsertPaymentAttemptAsync(CommercePaymentAttempt attempt);
        Task<CommerceDashboardKpis> GetDashboardKpisAsync(int? siteId);
        Task<bool> OrderItemColumnExistsAsync(string columnName);
        Task<bool> OrderColumnExistsAsync(string columnName);
    }

    public interface ICommerceService
    {
        Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false);
        Task<int> SaveShippingAsync(CommerceShippingMethod m);
        Task DeleteShippingAsync(int id);

        Task<List<CommercePaymentMethod>> ListPaymentAsync(int? siteId, bool activeOnly = false);
        Task<int> SavePaymentAsync(CommercePaymentMethod m);
        Task DeletePaymentAsync(int id);

        Task<List<CommerceDeliverySlot>> ListSlotsAsync(int? siteId, bool activeOnly = false);
        Task<int> SaveSlotAsync(CommerceDeliverySlot m);
        Task DeleteSlotAsync(int id);

        Task<CommerceCart> GetCartAsync(int? siteId, int? userId, string guestKey);
        Task<CommerceCart> AddToCartAsync(int? siteId, int? userId, string guestKey, int productId, string title, long unitPrice, int quantity);
        Task RemoveFromCartAsync(int cartId, int productId);
        Task ClearCartAsync(int cartId);

        Task<CommerceCheckoutResult> CheckoutAsync(int? siteId, int userId, string guestKey, CommerceCheckoutRequest request, string callbackBaseUrl);
        Task<CommerceCheckoutResult> CompleteOnlinePaymentAsync(string authority, string statusQuery);
        Task<CommerceOrderDetail> GetOrderAsync(int orderId);
        Task<List<CommerceOrderDetail>> ListOrdersAsync(int? siteId = null, int? userId = null, int? storeId = null);
        Task<List<CommerceOrderDetail>> ListOrdersForUserAsync(int actorUserId, int? siteId = null, int? userId = null, int? storeId = null);
        Task<List<CommerceOrderDetail>> ListSellerOrdersAsync(int sellerUserId);
        Task SetFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId);
        Task SetSellerFulfillmentAsync(int sellerUserId, int orderId, byte status, string trackingCode, string note, int? changedByUserId);
        Task<CommerceDashboardKpis> GetDashboardKpisAsync(int? siteId);
    }

    public interface IPaymentGateway
    {
        string Provider { get; }
        Task<(bool Ok, string Authority, string RedirectUrl, string Error)> StartPaymentAsync(long amountRial, string description, string callbackUrl, string orderNumber);
        Task<(bool Ok, string RefId, string Error)> VerifyAsync(string authority, long amountRial);
    }

    public interface IPaymentGatewayRegistry
    {
        IReadOnlyList<IPaymentGateway> GetAll();
        IPaymentGateway Get(string provider);
    }
}
