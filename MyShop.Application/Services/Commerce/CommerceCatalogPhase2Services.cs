using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceBrandService : ICommerceBrandService
    {
        private readonly ICommerceBrandRepository _repo;
        private readonly ICommerceProductAccessRepository _access;

        public CommerceBrandService(ICommerceBrandRepository repo, ICommerceProductAccessRepository access)
        {
            _repo = repo;
            _access = access;
        }

        public Task<List<CommerceBrand>> ListAsync(int? siteId, int skip = 0, int take = 200)
            => _repo.ListAsync(siteId, Math.Max(0, skip), Math.Clamp(take, 1, 500));

        public async Task<int> SaveAsync(CommerceBrand m)
        {
            if (m == null) throw new InvalidOperationException("داده برند ناقص است.");
            m.Name = CommerceCatalogValidation.RequireName(m.Name, 128);
            m.Slug = CommerceCatalogValidation.Slug(m.Slug, m.Name);
            m.SortOrder = CommerceCatalogValidation.SortOrder(m.SortOrder);
            if (!string.IsNullOrWhiteSpace(m.LogoUrl))
                m.LogoUrl = CommerceCatalogValidation.ValidateImageUrl(m.LogoUrl);
            if (m.Id > 0)
            {
                var existing = await _repo.GetAsync(m.Id, m.SiteId);
                if (existing == null) throw new InvalidOperationException("برند یافت نشد.");
                await _repo.UpdateAsync(m);
                return m.Id;
            }
            return await _repo.InsertAsync(m);
        }

        public Task DeleteAsync(int id, int? siteId) => _repo.DeleteAsync(id, siteId);

        public async Task AssignToProductAsync(int productId, int brandId, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            if (brandId <= 0)
            {
                await _repo.ClearProductBrandAsync(productId);
                return;
            }
            var brand = await _repo.GetAsync(brandId, siteId);
            if (brand == null) throw new InvalidOperationException("برند یافت نشد.");
            await _repo.SetProductBrandAsync(productId, brandId);
        }
    }

    public class CommerceTagService : ICommerceTagService
    {
        private readonly ICommerceTagRepository _repo;
        private readonly ICommerceProductAccessRepository _access;

        public CommerceTagService(ICommerceTagRepository repo, ICommerceProductAccessRepository access)
        {
            _repo = repo;
            _access = access;
        }

        public Task<List<CommerceTag>> ListAsync(int? siteId, int skip = 0, int take = 200)
            => _repo.ListAsync(siteId, Math.Max(0, skip), Math.Clamp(take, 1, 500));

        public async Task<int> SaveAsync(CommerceTag m)
        {
            if (m == null) throw new InvalidOperationException("داده برچسب ناقص است.");
            m.Name = CommerceCatalogValidation.RequireName(m.Name, 64);
            m.Slug = CommerceCatalogValidation.Slug(m.Slug, m.Name);
            m.SortOrder = CommerceCatalogValidation.SortOrder(m.SortOrder);
            if (m.Id > 0)
            {
                if (await _repo.GetAsync(m.Id, m.SiteId) == null) throw new InvalidOperationException("برچسب یافت نشد.");
                await _repo.UpdateAsync(m);
                return m.Id;
            }
            return await _repo.InsertAsync(m);
        }

        public Task DeleteAsync(int id, int? siteId) => _repo.DeleteAsync(id, siteId);

        public async Task SetProductTagsAsync(int productId, IReadOnlyList<int> tagIds, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            var ids = (tagIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            foreach (var id in ids)
            {
                if (await _repo.GetAsync(id, siteId) == null)
                    throw new InvalidOperationException("برچسب نامعتبر است.");
            }
            await _repo.SetProductTagsAsync(productId, ids);
        }

        public async Task<List<int>> GetProductTagIdsAsync(int productId, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            return await _repo.ListProductTagIdsAsync(productId);
        }
    }

    public class CommerceProductImageService : ICommerceProductImageService
    {
        private readonly ICommerceProductImageRepository _repo;
        private readonly ICommerceProductAccessRepository _access;

        public CommerceProductImageService(ICommerceProductImageRepository repo, ICommerceProductAccessRepository access)
        {
            _repo = repo;
            _access = access;
        }

        public async Task<List<CommerceProductImage>> ListAsync(int productId, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            return await _repo.ListAsync(productId);
        }

        public async Task<int> SaveAsync(CommerceProductImage m, int? siteId)
        {
            if (m == null) throw new InvalidOperationException("داده تصویر ناقص است.");
            if (!await _access.ProductAccessibleAsync(m.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            m.Url = CommerceCatalogValidation.ValidateImageUrl(m.Url);
            m.SortOrder = CommerceCatalogValidation.SortOrder(m.SortOrder);
            if (m.IsPrimary)
                await _repo.ClearPrimaryAsync(m.ProductId);
            if (m.Id > 0)
            {
                var existing = await _repo.GetAsync(m.Id);
                if (existing == null || existing.ProductId != m.ProductId)
                    throw new InvalidOperationException("تصویر یافت نشد.");
                await _repo.UpdateAsync(m);
                return m.Id;
            }
            return await _repo.InsertAsync(m);
        }

        public async Task DeleteAsync(int id, int? siteId)
        {
            var existing = await _repo.GetAsync(id);
            if (existing == null) return;
            if (!await _access.ProductAccessibleAsync(existing.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            await _repo.DeleteAsync(id);
        }
    }

    public class CommerceProductRelationService : ICommerceProductRelationService
    {
        private readonly ICommerceProductRelationRepository _repo;
        private readonly ICommerceProductAccessRepository _access;

        public CommerceProductRelationService(ICommerceProductRelationRepository repo, ICommerceProductAccessRepository access)
        {
            _repo = repo;
            _access = access;
        }

        public async Task<List<CommerceProductRelation>> ListAsync(int productId, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            return await _repo.ListAsync(productId);
        }

        public async Task<int> SaveAsync(CommerceProductRelation m, int? siteId)
        {
            if (m == null) throw new InvalidOperationException("داده ارتباط ناقص است.");
            if (m.ProductId == m.RelatedProductId)
                throw new InvalidOperationException("محصول نمی‌تواند به خودش مرتبط شود.");
            if (!CommerceRelationTypes.IsValid(m.RelationType))
                throw new InvalidOperationException("نوع ارتباط نامعتبر است.");
            m.RelationType = CommerceRelationTypes.Normalize(m.RelationType);
            m.SortOrder = CommerceCatalogValidation.SortOrder(m.SortOrder);
            if (!await _access.ProductAccessibleAsync(m.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            if (!await _access.ProductAccessibleAsync(m.RelatedProductId, siteId))
                throw new InvalidOperationException("محصول مرتبط یافت نشد.");
            if (await _repo.ExistsAsync(m.ProductId, m.RelatedProductId, m.RelationType, m.Id > 0 ? m.Id : null))
                throw new InvalidOperationException("این ارتباط قبلاً ثبت شده است.");
            if (m.Id > 0)
            {
                var existing = await _repo.GetAsync(m.Id);
                if (existing == null || existing.ProductId != m.ProductId)
                    throw new InvalidOperationException("ارتباط یافت نشد.");
                await _repo.UpdateAsync(m);
                return m.Id;
            }
            return await _repo.InsertAsync(m);
        }

        public async Task DeleteAsync(int id, int? siteId)
        {
            var existing = await _repo.GetAsync(id);
            if (existing == null) return;
            if (!await _access.ProductAccessibleAsync(existing.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            await _repo.DeleteAsync(id);
        }
    }

    public class CommerceProductSeoService : ICommerceProductSeoService
    {
        private readonly ICommerceProductSeoRepository _repo;
        private readonly ICommerceProductAccessRepository _access;

        public CommerceProductSeoService(ICommerceProductSeoRepository repo, ICommerceProductAccessRepository access)
        {
            _repo = repo;
            _access = access;
        }

        public async Task<CommerceProductSeo> GetAsync(int productId, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            return await _repo.GetAsync(productId) ?? new CommerceProductSeo { ProductId = productId };
        }

        public async Task SaveAsync(CommerceProductSeo m, int? siteId)
        {
            if (m == null) throw new InvalidOperationException("داده سئو ناقص است.");
            if (!await _access.ProductAccessibleAsync(m.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            await _repo.UpsertAsync(m);
        }
    }

    public class CommerceAttributeService : ICommerceAttributeService
    {
        private readonly ICommerceAttributeRepository _repo;

        public CommerceAttributeService(ICommerceAttributeRepository repo) => _repo = repo;

        public async Task<List<CommerceAttribute>> ListAsync(int? siteId, int skip = 0, int take = 200)
        {
            var list = await _repo.ListAsync(siteId, Math.Max(0, skip), Math.Clamp(take, 1, 500));
            foreach (var a in list)
                a.Terms = await _repo.ListTermsAsync(a.Id);
            return list;
        }

        public async Task<int> SaveAttributeAsync(CommerceAttribute m)
        {
            if (m == null) throw new InvalidOperationException("داده ویژگی ناقص است.");
            m.Name = CommerceCatalogValidation.RequireName(m.Name, 64);
            m.Slug = CommerceCatalogValidation.Slug(m.Slug, m.Name);
            m.SortOrder = CommerceCatalogValidation.SortOrder(m.SortOrder);
            if (m.Id > 0)
            {
                if (await _repo.GetAsync(m.Id, m.SiteId) == null) throw new InvalidOperationException("ویژگی یافت نشد.");
                await _repo.UpdateAsync(m);
                return m.Id;
            }
            return await _repo.InsertAsync(m);
        }

        public Task DeleteAttributeAsync(int id, int? siteId) => _repo.DeleteAsync(id, siteId);

        public async Task<int> SaveTermAsync(CommerceAttributeTerm m, int? siteId)
        {
            if (m == null) throw new InvalidOperationException("داده مقدار ویژگی ناقص است.");
            var attr = await _repo.GetAsync(m.AttributeId, siteId);
            if (attr == null) throw new InvalidOperationException("ویژگی یافت نشد.");
            m.Name = CommerceCatalogValidation.RequireName(m.Name, 64);
            m.Slug = CommerceCatalogValidation.Slug(m.Slug, m.Name);
            m.SortOrder = CommerceCatalogValidation.SortOrder(m.SortOrder);
            if (m.Id > 0)
            {
                var t = await _repo.GetTermAsync(m.Id);
                if (t == null || t.AttributeId != m.AttributeId) throw new InvalidOperationException("مقدار ویژگی یافت نشد.");
                await _repo.UpdateTermAsync(m);
                return m.Id;
            }
            return await _repo.InsertTermAsync(m);
        }

        public async Task DeleteTermAsync(int termId, int? siteId)
        {
            var t = await _repo.GetTermAsync(termId);
            if (t == null) return;
            if (await _repo.GetAsync(t.AttributeId, siteId) == null)
                throw new InvalidOperationException("ویژگی یافت نشد.");
            await _repo.DeleteTermAsync(termId);
        }
    }

    public class CommerceVariationService : ICommerceVariationService
    {
        private readonly ICommerceVariationRepository _repo;
        private readonly ICommerceProductAccessRepository _access;
        private readonly ICommerceAttributeRepository _attrs;

        public CommerceVariationService(
            ICommerceVariationRepository repo,
            ICommerceProductAccessRepository access,
            ICommerceAttributeRepository attrs)
        {
            _repo = repo;
            _access = access;
            _attrs = attrs;
        }

        public async Task<List<CommerceProductVariation>> ListAsync(int productId, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            var list = await _repo.ListAsync(productId);
            foreach (var v in list)
                v.Attributes = await _repo.ListAttributesAsync(v.Id);
            return list;
        }

        public async Task<int> SaveAsync(CommerceProductVariation m, int? siteId)
        {
            if (m == null) throw new InvalidOperationException("داده تنوع ناقص است.");
            if (!await _access.ProductAccessibleAsync(m.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            m.SiteId = siteId;
            m.SortOrder = CommerceCatalogValidation.SortOrder(m.SortOrder);
            if (m.Stock < 0) throw new InvalidOperationException("موجودی تنوع نمی‌تواند منفی باشد.");
            if (!string.IsNullOrWhiteSpace(m.ImageUrl))
                m.ImageUrl = CommerceCatalogValidation.ValidateImageUrl(m.ImageUrl);
            var sku = (m.Sku ?? "").Trim();
            m.Sku = sku.Length == 0 ? null : sku;
            if (!string.IsNullOrEmpty(m.Sku))
            {
                if (await _repo.SkuExistsAsync(m.Sku, siteId, m.Id > 0 ? m.Id : null))
                    throw new InvalidOperationException("SKU تنوع تکراری است.");
                if (await _access.SkuUsedOnProductAsync(m.Sku, null))
                    throw new InvalidOperationException("SKU با محصول ساده دیگری تداخل دارد.");
            }
            if (m.Id > 0)
            {
                var existing = await _repo.GetAsync(m.Id);
                if (existing == null || existing.ProductId != m.ProductId)
                    throw new InvalidOperationException("تنوع یافت نشد.");
                await _repo.UpdateAsync(m);
                return m.Id;
            }
            return await _repo.InsertAsync(m);
        }

        public async Task DeleteAsync(int id, int? siteId)
        {
            var existing = await _repo.GetAsync(id);
            if (existing == null) return;
            if (!await _access.ProductAccessibleAsync(existing.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            await _repo.DeleteAsync(id);
        }

        public async Task SetAttributesAsync(int variationId, IReadOnlyList<CommerceVariationAttribute> attrs, int? siteId)
        {
            var v = await _repo.GetAsync(variationId);
            if (v == null) throw new InvalidOperationException("تنوع یافت نشد.");
            if (!await _access.ProductAccessibleAsync(v.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            var list = attrs ?? Array.Empty<CommerceVariationAttribute>();
            var seen = new HashSet<int>();
            foreach (var a in list)
            {
                if (!seen.Add(a.AttributeId))
                    throw new InvalidOperationException("هر ویژگی فقط یک‌بار روی تنوع مجاز است.");
                var attr = await _attrs.GetAsync(a.AttributeId, siteId);
                if (attr == null) throw new InvalidOperationException("ویژگی نامعتبر است.");
                var term = await _attrs.GetTermAsync(a.TermId);
                if (term == null || term.AttributeId != a.AttributeId)
                    throw new InvalidOperationException("مقدار ویژگی به این ویژگی تعلق ندارد.");
            }
            await _repo.ReplaceAttributesAsync(variationId, list.ToList());
        }

        public async Task SetDefaultAsync(int variationId, int? siteId)
        {
            var existing = await _repo.GetAsync(variationId);
            if (existing == null)
                throw new InvalidOperationException("تنوع یافت نشد.");
            if (!await _access.ProductAccessibleAsync(existing.ProductId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            await _repo.SetDefaultAsync(existing.ProductId, variationId);
        }

        public async Task<CommerceProductVariation> GetDefaultAsync(int productId, int? siteId)
        {
            var list = await ListAsync(productId, siteId);
            return list.FirstOrDefault(v => v.IsDefault && v.IsActive)
                ?? list.FirstOrDefault(v => v.IsActive);
        }
    }

    public class CommerceProductCategoryService : ICommerceProductCategoryService
    {
        private readonly ICommerceProductCategoryRepository _repo;
        private readonly ICommerceProductAccessRepository _access;

        public CommerceProductCategoryService(ICommerceProductCategoryRepository repo, ICommerceProductAccessRepository access)
        {
            _repo = repo;
            _access = access;
        }

        public Task<List<CommerceProductCategory>> ListAsync() => _repo.ListAsync();

        public async Task<int> SaveAsync(int id, string name)
        {
            name = CommerceCatalogValidation.RequireName(name, 200);
            if (id > 0)
            {
                await _repo.UpdateAsync(id, name);
                return id;
            }
            return await _repo.InsertAsync(name);
        }

        public Task DeleteAsync(int id) => _repo.DeleteAsync(id);

        public async Task SetProductCategoriesAsync(int productId, IReadOnlyList<int> categoryIds, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            await _repo.SetProductCategoriesAsync(productId, (categoryIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList());
        }

        public async Task<List<int>> GetProductCategoryIdsAsync(int productId, int? siteId)
        {
            if (!await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");
            return await _repo.ListProductCategoryIdsAsync(productId);
        }
    }

    public class CommerceSellerCatalogGuard : ICommerceSellerCatalogGuard
    {
        private readonly IProductService _products;
        private readonly IStoreService _stores;

        public CommerceSellerCatalogGuard(IProductService products, IStoreService stores)
        {
            _products = products;
            _stores = stores;
        }

        public async Task EnsureOwnsProductAsync(int userId, int productId)
        {
            if (userId <= 0) throw new InvalidOperationException("ورود الزامی است.");
            var p = await _products.GetByIdAsync(productId);
            if (p == null) throw new InvalidOperationException("محصول یافت نشد.");
            if (!p.StoreId.HasValue || p.StoreId.Value <= 0)
                throw new InvalidOperationException("این محصول به فروشگاهی متصل نیست.");
            var owned = await _stores.GetByUserIdAsync(userId) ?? new List<MyShop.Core.Models.Store>();
            if (!owned.Any(s => s.StoreID == p.StoreId.Value || s.Id == p.StoreId.Value))
                throw new InvalidOperationException("این محصول به فروشگاه شما تعلق ندارد.");
        }
    }

    public class CommerceShopCatalogService : ICommerceShopCatalogService
    {
        private readonly IProductService _products;
        private readonly IStoreService _stores;

        public CommerceShopCatalogService(IProductService products, IStoreService stores)
        {
            _products = products;
            _stores = stores;
        }

        public async Task<List<ProductDto>> ListVisibleAsync(int? siteId)
        {
            var all = await _products.GetAllAsync() ?? new List<ProductDto>();
            all = all.Where(p => p.IsActive).ToList();
            if (!siteId.HasValue)
                return all;

            var stores = await _stores.GetAllAsync() ?? new List<Store>();
            var ids = stores
                .Where(s => s.SiteId == siteId)
                .Select(s => s.StoreID != 0 ? s.StoreID : s.Id)
                .Where(id => id > 0)
                .ToHashSet();
            return all.Where(p => p.StoreId.HasValue && ids.Contains(p.StoreId.Value)).ToList();
        }

        public async Task<ProductDto> GetVisibleAsync(int productId, int? siteId)
        {
            var p = await _products.GetByIdAsync(productId);
            if (p == null || !p.IsActive)
                return null;
            if (!p.StoreId.HasValue || p.StoreId.Value <= 0)
                return null;
            if (!siteId.HasValue)
                return p;

            var store = await _stores.GetByIdAsync(p.StoreId.Value);
            if (store == null)
                return null;
            var storeKey = store.StoreID != 0 ? store.StoreID : store.Id;
            if (storeKey != p.StoreId.Value && store.Id != p.StoreId.Value && store.StoreID != p.StoreId.Value)
                return null;
            if (!store.SiteId.HasValue || store.SiteId.Value != siteId.Value)
                return null;
            return p;
        }

        public static bool VariationPickerEnabled(ICommerceFeatureFlags flags) =>
            flags != null
            && flags.IsEnabled(CommerceConstants.Features.Variations)
            && flags.IsEnabled(CommerceConstants.Features.VariationCart);
    }
}
