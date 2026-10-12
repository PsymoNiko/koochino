using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Commerce
{
    public static class CommerceFulfillmentStatus
    {
        public const byte Draft = 0;
        public const byte AwaitingPayment = 1;
        public const byte Paid = 2;
        public const byte Processing = 3;
        public const byte Shipped = 4;
        public const byte Delivered = 5;
        public const byte Cancelled = 6;
        public const byte Refunded = 7;

        public static string Title(byte s) => s switch
        {
            Draft => "پیش‌نویس",
            AwaitingPayment => "در انتظار پرداخت",
            Paid => "پرداخت‌شده",
            Processing => "در حال آماده‌سازی",
            Shipped => "ارسال‌شده",
            Delivered => "تحویل‌شده",
            Cancelled => "لغو شده",
            Refunded => "مسترد شده",
            _ => s.ToString()
        };
    }

    public static class CommercePaymentStatus
    {
        public const byte Unpaid = 0;
        public const byte Pending = 1;
        public const byte Paid = 2;
        public const byte Failed = 3;
        public const byte Refunded = 4;

        public static string Title(byte s) => s switch
        {
            Unpaid => "پرداخت‌نشده",
            Pending => "در حال پرداخت",
            Paid => "پرداخت موفق",
            Failed => "ناموفق",
            Refunded => "مسترد",
            _ => s.ToString()
        };
    }

    public static class CommercePaymentCodes
    {
        public const string CashOnDelivery = "CashOnDelivery";
        public const string OnlineGateway = "OnlineGateway";
        public const string CardToCard = "CardToCard";
        public const string Manual = "Manual";
    }

    public class CommerceShippingMethod
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; }
        public long Fee { get; set; }
        public long? MinOrderAmount { get; set; }
        /// <summary>Subtotal at or above this amount → shipping fee 0. Null = no free threshold.</summary>
        public long? FreeOverAmount { get; set; }
        public int? EstimatedDays { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommercePaymentMethod
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Title { get; set; } = "";
        public string Code { get; set; } = "";
        public string GatewayProvider { get; set; }
        public string Description { get; set; }
        public string ConfigJson { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommerceDeliverySlot
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Title { get; set; } = "";
        public byte? DayOfWeek { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? Capacity { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class CommerceCart
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? UserId { get; set; }
        public string GuestKey { get; set; }
        public int? StoreId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CommerceCartItem> Items { get; set; } = new();
        public long Subtotal
        {
            get
            {
                long s = 0;
                if (Items == null) return 0;
                foreach (var i in Items) s += i.LineTotal;
                return s;
            }
        }
    }

    public class CommerceCartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string Title { get; set; } = "";
        public long UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime CreatedAt { get; set; }
        public long LineTotal => UnitPrice * Quantity;
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? ProductId { get; set; }
        public int? StoreId { get; set; }
        public int? VariationId { get; set; }
        public string Sku { get; set; }
        public long DiscountAmount { get; set; }
        public string Title { get; set; } = "";
        public int Quantity { get; set; }
        public long UnitPrice { get; set; }
        public long LineTotal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public byte? FromStatus { get; set; }
        public byte ToStatus { get; set; }
        public string Note { get; set; }
        public int? ChangedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ToStatusTitle => CommerceFulfillmentStatus.Title(ToStatus);
        public string ChangedByName { get; set; }
    }

    public class CommercePayment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? PaymentMethodId { get; set; }
        public long Amount { get; set; }
        public string Provider { get; set; }
        public string Authority { get; set; }
        public string RefId { get; set; }
        public byte Status { get; set; }
        public string RawResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        /// <summary>0=none 1=in progress 2=done. Additive Phase 4.</summary>
        public byte FinalizedState { get; set; }
    }

    public static class CommercePaymentFinalizedState
    {
        public const byte None = 0;
        public const byte InProgress = 1;
        public const byte Done = 2;
    }

    public static class CommercePaymentAttemptTypes
    {
        public const string Callback = "Callback";
        public const string AlreadyProcessed = "AlreadyProcessed";
        public const string UserCancel = "UserCancel";
        public const string VerifyFailed = "VerifyFailed";
        public const string Completed = "Completed";
        public const string Rejected = "Rejected";
        public const string Conflict = "Conflict";
    }

    public class CommercePaymentAttempt
    {
        public long Id { get; set; }
        public int PaymentId { get; set; }
        public string AttemptType { get; set; }
        public string Authority { get; set; }
        public string ReferenceId { get; set; }
        public string Status { get; set; }
        public string ResponseCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public static class CommerceRefundStatus
    {
        public const byte Requested = 0;
        public const byte Completed = 1;
        public const byte Rejected = 2;

        public static string Title(byte s) => s switch
        {
            Requested => "درخواست‌شده",
            Completed => "انجام‌شده",
            Rejected => "رد شده",
            _ => s.ToString()
        };
    }

    public class CommerceRefund
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? PaymentId { get; set; }
        public long Amount { get; set; }
        public string Reason { get; set; }
        public byte Status { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string StatusTitle => CommerceRefundStatus.Title(Status);
    }

    public class CommerceOrderDetail
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int UserId { get; set; }
        public int? StoreId { get; set; }
        public int? SiteId { get; set; }
        public long TotalAmount { get; set; }
        public long DiscountAmount { get; set; }
        public long ShippingFee { get; set; }
        public long TaxAmount { get; set; }
        public long FinalAmount { get; set; }
        public int? ShippingMethodId { get; set; }
        public int? PaymentMethodId { get; set; }
        public int? DeliverySlotId { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string ShippingAddress { get; set; }
        public string TrackingCode { get; set; }
        public byte FulfillmentStatus { get; set; }
        public byte PaymentStatus { get; set; }
        public bool IsCommerce { get; set; }
        public DateTime OrderDate { get; set; }
        public string Notes { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string StoreName { get; set; }
        public string ShippingMethodTitle { get; set; }
        public string PaymentMethodTitle { get; set; }
        public string DeliverySlotTitle { get; set; }
        public string FulfillmentStatusTitle => CommerceFulfillmentStatus.Title(FulfillmentStatus);
        public string PaymentStatusTitle => CommercePaymentStatus.Title(PaymentStatus);
        public List<OrderItem> Items { get; set; } = new();
        public List<OrderStatusHistory> History { get; set; } = new();
    }

    public class CommerceCheckoutRequest
    {
        public int? StoreId { get; set; }
        public int ShippingMethodId { get; set; }
        public int PaymentMethodId { get; set; }
        public int? DeliverySlotId { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string ShippingAddress { get; set; }
        public string Notes { get; set; }
        /// <summary>Commerce promotion code only. Not Shop Copon.</summary>
        public string PromoCode { get; set; }
    }

    public class CommerceCheckoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? OrderId { get; set; }
        public string OrderNumber { get; set; }
        public bool NeedsOnlinePayment { get; set; }
        public string PaymentRedirectUrl { get; set; }
        public string Authority { get; set; }
        public static CommerceCheckoutResult Ok(string msg = null) => new() { Success = true, Message = msg };
        public static CommerceCheckoutResult Fail(string msg) => new() { Success = false, Message = msg };
    }
}
