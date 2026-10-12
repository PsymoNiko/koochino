using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceVariationCartService : ICommerceVariationCartService
    {
        private readonly ICommerceFeatureFlags _flags;
        private readonly ICommerceCartRepository _carts;
        private readonly ICommerceVariationCartRepository _lines;
        private readonly ICommerceVariationRepository _variations;
        private readonly ICommerceProductAccessRepository _access;
        private readonly IProductService _products;
        private readonly ICommercePricingService _pricing;

        public CommerceVariationCartService(
            ICommerceFeatureFlags flags,
            ICommerceCartRepository carts,
            ICommerceVariationCartRepository lines,
            ICommerceVariationRepository variations,
            ICommerceProductAccessRepository access,
            IProductService products,
            ICommercePricingService pricing)
        {
            _flags = flags;
            _carts = carts;
            _lines = lines;
            _variations = variations;
            _access = access;
            _products = products;
            _pricing = pricing;
        }

        public async Task<List<CommerceVariationCartItem>> ListAsync(int? siteId, int? userId, string guestKey)
        {
            EnsureFlag();
            var cart = await _carts.GetOrCreateAsync(siteId, userId, guestKey);
            return await _lines.ListAsync(cart.Id);
        }

        public async Task<CommerceVariationCartItem> AddAsync(int? siteId, int? userId, string guestKey, int variationId, int quantity)
        {
            EnsureFlag();
            var qtyAdd = Math.Max(1, quantity);

            var variation = await _variations.GetAsync(variationId);
            if (variation == null || !variation.IsActive)
                throw new InvalidOperationException("تنوع یافت نشد یا غیرفعال است.");
            if (siteId.HasValue && variation.SiteId.HasValue && variation.SiteId.Value != siteId.Value)
                throw new InvalidOperationException("تنوع به این سایت تعلق ندارد.");
            if (!await _access.ProductAccessibleAsync(variation.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");

            var product = await _products.GetByIdAsync(variation.ProductId);
            if (product == null || !product.IsActive)
                throw new InvalidOperationException("محصول یافت نشد یا غیرفعال است.");

            var quote = _pricing.QuoteVariation(product, variation);

            var cart = await _carts.GetOrCreateAsync(siteId, userId, guestKey);
            var existing = await _lines.GetAsync(cart.Id, variationId);
            var newQty = (existing?.Quantity ?? 0) + qtyAdd;
            if (variation.Stock < newQty)
                throw new InvalidOperationException($"موجودی تنوع کافی نیست (موجود: {variation.Stock}).");

            var line = new CommerceVariationCartItem
            {
                CartId = cart.Id,
                ProductId = variation.ProductId,
                VariationId = variationId,
                Quantity = newQty,
                UnitPriceSnapshot = quote.UnitPrice,
                CompareAtPriceSnapshot = quote.CompareAtPrice
            };
            await _lines.UpsertAsync(line);
            return await _lines.GetAsync(cart.Id, variationId);
        }

        public async Task SetQuantityAsync(int? siteId, int? userId, string guestKey, int variationId, int quantity)
        {
            EnsureFlag();
            if (quantity <= 0)
            {
                await RemoveAsync(siteId, userId, guestKey, variationId);
                return;
            }

            var variation = await _variations.GetAsync(variationId);
            if (variation == null || !variation.IsActive)
                throw new InvalidOperationException("تنوع یافت نشد یا غیرفعال است.");
            if (siteId.HasValue && variation.SiteId.HasValue && variation.SiteId.Value != siteId.Value)
                throw new InvalidOperationException("تنوع به این سایت تعلق ندارد.");
            if (!await _access.ProductAccessibleAsync(variation.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");

            var product = await _products.GetByIdAsync(variation.ProductId);
            if (product == null || !product.IsActive)
                throw new InvalidOperationException("محصول یافت نشد یا غیرفعال است.");
            if (variation.Stock < quantity)
                throw new InvalidOperationException($"موجودی تنوع کافی نیست (موجود: {variation.Stock}).");

            var quote = _pricing.QuoteVariation(product, variation);
            var cart = await _carts.GetOrCreateAsync(siteId, userId, guestKey);
            await _lines.UpsertAsync(new CommerceVariationCartItem
            {
                CartId = cart.Id,
                ProductId = variation.ProductId,
                VariationId = variationId,
                Quantity = quantity,
                UnitPriceSnapshot = quote.UnitPrice,
                CompareAtPriceSnapshot = quote.CompareAtPrice
            });
        }

        public async Task RemoveAsync(int? siteId, int? userId, string guestKey, int variationId)
        {
            EnsureFlag();
            var cart = await _carts.GetOrCreateAsync(siteId, userId, guestKey);
            await _lines.RemoveAsync(cart.Id, variationId);
        }

        void EnsureFlag()
        {
            if (_flags == null || !_flags.IsEnabled(CommerceConstants.Features.VariationCart))
                throw new InvalidOperationException("سبد تنوع غیرفعال است.");
        }
    }
}
