using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Commerce
{
    public static class CommerceRelationTypes
    {
        public const string Related = "Related";
        public const string Upsell = "Upsell";
        public const string CrossSell = "CrossSell";

        public static bool IsValid(string t) =>
            string.Equals(t, Related, StringComparison.OrdinalIgnoreCase)
            || string.Equals(t, Upsell, StringComparison.OrdinalIgnoreCase)
            || string.Equals(t, CrossSell, StringComparison.OrdinalIgnoreCase);

        public static string Normalize(string t)
        {
            if (string.Equals(t, Upsell, StringComparison.OrdinalIgnoreCase)) return Upsell;
            if (string.Equals(t, CrossSell, StringComparison.OrdinalIgnoreCase)) return CrossSell;
            return Related;
        }
    }

    public class CommerceBrand
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommerceTag
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CommerceProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Url { get; set; } = "";
        public string AltText { get; set; }
        public string Title { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class CommerceProductRelation
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int RelatedProductId { get; set; }
        public string RelationType { get; set; } = CommerceRelationTypes.Related;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string RelatedProductName { get; set; }
    }

    public class CommerceProductSeo
    {
        public int ProductId { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string CanonicalUrl { get; set; }
        public bool NoIndex { get; set; }
        public string OpenGraphTitle { get; set; }
        public string OpenGraphDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CommerceAttribute
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public List<CommerceAttributeTerm> Terms { get; set; } = new();
    }

    public class CommerceAttributeTerm
    {
        public int Id { get; set; }
        public int AttributeId { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class CommerceProductVariation
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? SiteId { get; set; }
        public string Sku { get; set; }
        public decimal? Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CommerceVariationAttribute> Attributes { get; set; } = new();
    }

    public class CommerceVariationCartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int VariationId { get; set; }
        public int Quantity { get; set; } = 1;
        public long UnitPriceSnapshot { get; set; }
        public long? CompareAtPriceSnapshot { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long LineTotal => UnitPriceSnapshot * Quantity;
        public string Sku { get; set; }
        public string ProductName { get; set; }
    }

    public class CommercePriceQuote
    {
        public long UnitPrice { get; set; }
        public long? CompareAtPrice { get; set; }
        public bool IsSale => CompareAtPrice.HasValue && CompareAtPrice.Value > UnitPrice;
    }

    public class CommerceStockReservation
    {
        public int Id { get; set; }
        public int? CartId { get; set; }
        public int? OrderId { get; set; }
        public int ProductId { get; set; }
        public int? VariationId { get; set; }
        public int Quantity { get; set; }
        public byte Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CommercePromotion
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? StoreId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string DiscountType { get; set; } = CommerceConstants.PromotionDiscountType.Percent;
        public long DiscountValue { get; set; }
        public long MinSubtotal { get; set; }
        public long? MaxDiscount { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CommercePromotionQuote
    {
        public int? PromotionId { get; set; }
        public long DiscountAmount { get; set; }
        public string Title { get; set; }
    }

    public class CommerceVariationAttribute
    {
        public int Id { get; set; }
        public int VariationId { get; set; }
        public int AttributeId { get; set; }
        public int TermId { get; set; }
        public string AttributeName { get; set; }
        public string TermName { get; set; }
    }

    public class CommerceProductCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class CommerceTaxRate
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = "";
        public decimal RatePercent { get; set; }
        public bool AppliesToShipping { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class CommerceStoreShippingRate
    {
        public int Id { get; set; }
        public int ShippingMethodId { get; set; }
        public int StoreId { get; set; }
        public long Fee { get; set; }
    }
}
