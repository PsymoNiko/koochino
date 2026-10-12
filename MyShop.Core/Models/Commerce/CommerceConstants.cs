namespace MyShop.Core.Models.Commerce
{
    /// <summary>
    /// Foundation constants for the existing Commerce module.
    /// Does not replace live checkout; new feature keys default OFF at the gate.
    /// </summary>
    public static class CommerceConstants
    {
        public const string ModuleKey = "Commerce";

        /// <summary>Maps to existing koch.Orders.IsCommerce. No schema change.</summary>
        public static class OrderSource
        {
            public const string Legacy = "Legacy";
            public const string Commerce = "Commerce";
        }

        public static class Submodules
        {
            public const string Catalog = "Catalog";
            public const string Pricing = "Pricing";
            public const string Inventory = "Inventory";
            public const string Cart = "Cart";
            public const string Checkout = "Checkout";
            public const string Orders = "Orders";
            public const string Payments = "Payments";
            public const string Shipping = "Shipping";
            public const string Tax = "Tax";
            public const string Coupons = "Coupons";
            public const string Promotions = "Promotions";
            public const string Wishlist = "Wishlist";
            public const string Reviews = "Reviews";
            public const string Customers = "Customers";
            public const string Reports = "Reports";
            public const string Refunds = "Refunds";
            public const string Reservations = "Reservations";
            public const string Settlement = "Settlement";
        }

        /// <summary>Config path prefix: Commerce:Features:{Name}. Missing key = OFF.</summary>
        public static class Features
        {
            public const string ConfigSection = "Commerce:Features";
            public const string Variations = "Variations";
            public const string CatalogExtensions = "CatalogExtensions";
            public const string Wishlist = "Wishlist";
            public const string Promotions = "Promotions";
            public const string Tax = "Tax";
            public const string CouponsOnCheckout = "CouponsOnCheckout";
            public const string VariationCart = "VariationCart";
            public const string Refunds = "Refunds";
            public const string Reservation = "Reservation";
            public const string MultiVendor = "MultiVendor";
            public const string Reports = "Reports";
            public const string Settlement = "Settlement";
        }

        public static class SettlementStatus
        {
            public const byte Settled = 1;
            public const byte Voided = 2;
        }

        public static class ReservationStatus
        {
            public const byte Active = 0;
            public const byte Committed = 1;
            public const byte Released = 2;
        }

        public const int ReservationTtlMinutes = 30;

        public static class PromotionDiscountType
        {
            public const string Percent = "Percent";
            public const string Fixed = "Fixed";
        }
    }
}
