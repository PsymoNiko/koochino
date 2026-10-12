using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models.Commerce;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceWishlistService : ICommerceWishlistService
    {
        private readonly ICommerceWishlistRepository _repo;
        private readonly ICommerceFeatureFlags _flags;

        public CommerceWishlistService(ICommerceWishlistRepository repo, ICommerceFeatureFlags flags = null)
        {
            _repo = repo;
            _flags = flags;
        }

        bool On() => _flags != null && _flags.IsEnabled(CommerceConstants.Features.Wishlist);

        public async Task<List<CommerceWishlistItem>> ListAsync(int userId)
        {
            if (!On() || userId <= 0) return new List<CommerceWishlistItem>();
            try { return await _repo.ListByUserAsync(userId) ?? new List<CommerceWishlistItem>(); }
            catch { return new List<CommerceWishlistItem>(); }
        }

        public async Task<bool> ContainsAsync(int userId, int productId)
        {
            if (!On() || userId <= 0) return false;
            try { return await _repo.GetAsync(userId, productId) != null; }
            catch { return false; }
        }

        public async Task<bool> ToggleAsync(int userId, int productId)
        {
            if (!On()) throw new InvalidOperationException("علاقه‌مندی فعال نیست.");
            if (userId <= 0) throw new InvalidOperationException("برای علاقه‌مندی وارد شوید.");
            if (productId <= 0) throw new InvalidOperationException("محصول نامعتبر است.");
            try
            {
                var existing = await _repo.GetAsync(userId, productId);
                if (existing != null)
                {
                    await _repo.DeleteAsync(userId, productId);
                    return false;
                }
                await _repo.InsertAsync(userId, productId);
                return true;
            }
            catch (InvalidOperationException) { throw; }
            catch { throw new InvalidOperationException("علاقه‌مندی روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 11 را اعمال کنید."); }
        }

        public async Task RemoveAsync(int userId, int productId)
        {
            if (!On()) return;
            try { await _repo.DeleteAsync(userId, productId); }
            catch { }
        }
    }
}
