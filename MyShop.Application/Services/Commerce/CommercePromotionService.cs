using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public class CommercePromotionService : ICommercePromotionService
    {
        private readonly ICommercePromotionRepository _repo;

        public CommercePromotionService(ICommercePromotionRepository repo) => _repo = repo;

        public Task<List<CommercePromotion>> ListAsync(int? siteId) => _repo.ListAllAsync(siteId);

        public async Task<int> SaveAsync(CommercePromotion m, int? siteId)
        {
            if (m == null) throw new InvalidOperationException("داده پروموشن ناقص است.");
            m.Name = (m.Name ?? "").Trim();
            if (m.Name.Length == 0) throw new InvalidOperationException("نام پروموشن الزامی است.");
            m.SiteId = siteId ?? m.SiteId;
            m.Code = string.IsNullOrWhiteSpace(m.Code) ? null : m.Code.Trim().ToUpperInvariant();
            m.DiscountType = string.Equals(m.DiscountType, CommerceConstants.PromotionDiscountType.Fixed, StringComparison.OrdinalIgnoreCase)
                ? CommerceConstants.PromotionDiscountType.Fixed
                : CommerceConstants.PromotionDiscountType.Percent;
            if (m.DiscountValue <= 0) throw new InvalidOperationException("مقدار تخفیف نامعتبر است.");
            if (m.DiscountType == CommerceConstants.PromotionDiscountType.Percent && m.DiscountValue > 100)
                throw new InvalidOperationException("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");
            if (m.Id > 0)
            {
                await _repo.UpdateAsync(m);
                return m.Id;
            }
            return await _repo.InsertAsync(m);
        }

        public Task DeleteAsync(int id, int? siteId) => _repo.DeleteAsync(id);

        public async Task<CommercePromotionQuote> QuoteAsync(int? siteId, int? storeId, long subtotal, string promoCode)
        {
            var empty = new CommercePromotionQuote();
            if (subtotal <= 0) return empty;

            CommercePromotion chosen = null;
            var code = (promoCode ?? "").Trim();
            if (code.Length > 0)
            {
                chosen = await _repo.GetByCodeAsync(code, siteId);
                if (chosen == null || !IsEligible(chosen, siteId, storeId, DateTime.Now))
                    throw new InvalidOperationException("کد پروموشن نامعتبر است.");
            }
            else
            {
                var list = await _repo.ListActiveAsync(siteId, storeId) ?? new List<CommercePromotion>();
                chosen = list
                    .Where(p => string.IsNullOrWhiteSpace(p.Code) && IsEligible(p, siteId, storeId, DateTime.Now))
                    .Select(p => (Promo: p, Amount: ComputeDiscount(p, subtotal)))
                    .Where(x => x.Amount > 0)
                    .OrderByDescending(x => x.Amount)
                    .ThenByDescending(x => x.Promo.Id)
                    .Select(x => x.Promo)
                    .FirstOrDefault();
            }

            if (chosen == null) return empty;
            var amount = ComputeDiscount(chosen, subtotal);
            if (amount <= 0)
            {
                if (code.Length > 0)
                    throw new InvalidOperationException("این پروموشن برای مبلغ سفارش قابل استفاده نیست.");
                return empty;
            }

            return new CommercePromotionQuote
            {
                PromotionId = chosen.Id,
                DiscountAmount = amount,
                Title = chosen.Name
            };
        }

        public async Task RedeemAsync(int promotionId, int orderId, int userId, long amount)
        {
            await _repo.IncrementUsageAsync(promotionId);
            await _repo.InsertRedemptionAsync(promotionId, orderId, userId, amount);
        }

        public static bool IsEligible(CommercePromotion p, int? siteId, int? storeId, DateTime now)
        {
            if (p == null || !p.IsActive) return false;
            if (p.UsageLimit.HasValue && p.UsedCount >= p.UsageLimit.Value) return false;
            if (p.StartsAt.HasValue && now < p.StartsAt.Value) return false;
            if (p.EndsAt.HasValue && now > p.EndsAt.Value) return false;
            if (p.SiteId.HasValue && siteId.HasValue && p.SiteId.Value != siteId.Value) return false;
            if (p.StoreId.HasValue && storeId.HasValue && p.StoreId.Value != storeId.Value) return false;
            if (p.StoreId.HasValue && !storeId.HasValue) return false;
            return true;
        }

        public static long ComputeDiscount(CommercePromotion p, long subtotal)
        {
            if (p == null || subtotal <= 0) return 0;
            if (p.MinSubtotal > 0 && subtotal < p.MinSubtotal) return 0;
            long d;
            if (string.Equals(p.DiscountType, CommerceConstants.PromotionDiscountType.Percent, StringComparison.OrdinalIgnoreCase))
                d = (long)Math.Floor(subtotal * (decimal)p.DiscountValue / 100m);
            else
                d = p.DiscountValue;
            if (p.MaxDiscount.HasValue && p.MaxDiscount.Value > 0 && d > p.MaxDiscount.Value)
                d = p.MaxDiscount.Value;
            if (d > subtotal) d = subtotal;
            return d < 0 ? 0 : d;
        }
    }
}
