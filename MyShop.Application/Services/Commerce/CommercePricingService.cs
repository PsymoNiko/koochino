using System;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Application.Services.Commerce
{
    /// <summary>
    /// Server-authoritative regular/sale price. Does not read client amounts.
    /// In this catalog, Price is the selling amount; CompareAtPrice is the optional original (strikethrough).
    /// </summary>
    public class CommercePricingService : ICommercePricingService
    {
        public CommercePriceQuote QuoteProduct(ProductDto product)
        {
            if (product == null) throw new InvalidOperationException("محصول یافت نشد.");
            return FromDecimals(product.Price, product.CompareAtPrice);
        }

        public CommercePriceQuote QuoteVariation(ProductDto product, CommerceProductVariation variation)
        {
            if (product == null) throw new InvalidOperationException("محصول یافت نشد.");
            if (variation == null) throw new InvalidOperationException("تنوع یافت نشد.");
            var price = variation.Price ?? product.Price;
            var compare = variation.CompareAtPrice ?? product.CompareAtPrice;
            return FromDecimals(price, compare);
        }

        static CommercePriceQuote FromDecimals(decimal? price, decimal? compare)
        {
            var unit = ToRial(price);
            if (unit <= 0)
                throw new InvalidOperationException("قیمت فروش معتبر نیست.");
            long? cmp = compare.HasValue ? ToRial(compare) : (long?)null;
            if (cmp.HasValue && cmp.Value <= 0) cmp = null;
            return new CommercePriceQuote { UnitPrice = unit, CompareAtPrice = cmp };
        }

        public static long ToRial(decimal? value) => (long)Math.Round(value ?? 0m, 0, MidpointRounding.AwayFromZero);
    }
}
