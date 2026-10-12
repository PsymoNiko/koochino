using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MyShop.Application.Interfaces;
using MyShop.Application.Services;
using MyShop.Core.Models;

#nullable enable

namespace MyShop.Web.Services
{
    /// <summary>
    /// کش در حافظه برای داده‌های SEO صفحات تا بار دیتابیس در هر درخواست کاهش یابد.
    /// </summary>
    public class CachedPageSeoService : IPageSeoService
    {
        private const string CacheKeyPrefix = "PageSeo:";
        private const string AllKey = "PageSeo:All";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

        private readonly IPageSeoService _inner;
        private readonly IMemoryCache _cache;

        public CachedPageSeoService(PageSeoService inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<PageSeo?> GetByPageKeyAsync(string pageKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey)) return null;
            var key = CacheKeyPrefix + pageKey.Trim();
            try
            {
                return await _cache.GetOrCreateAsync(key, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    return await _inner.GetByPageKeyAsync(pageKey.Trim());
                });
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<PageSeo>> GetAllAsync()
        {
            var list = await _cache.GetOrCreateAsync(AllKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await _inner.GetAllAsync();
            });
            return list ?? new List<PageSeo>();
        }

        public async Task<bool> SaveAsync(PageSeo entity)
        {
            var result = await _inner.SaveAsync(entity);
            if (result && entity != null && !string.IsNullOrWhiteSpace(entity.PageKey))
            {
                _cache.Remove(CacheKeyPrefix + entity.PageKey.Trim());
                _cache.Remove(AllKey);
            }
            return result;
        }
    }
}
