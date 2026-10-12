using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Tests
{
    internal sealed class InMemoryCommerceCatalog
    {
        public readonly HashSet<int> AccessibleProducts = new();
        public readonly Dictionary<int, string> ProductSkus = new();
        public readonly Dictionary<int, int> ProductSites = new();

        public int NextId = 1;
        public List<CommerceBrand> Brands = new();
        public Dictionary<int, int> ProductBrands = new();
        public List<CommerceTag> Tags = new();
        public Dictionary<int, List<int>> ProductTags = new();
        public List<CommerceProductImage> Images = new();
        public List<CommerceProductRelation> Relations = new();
        public Dictionary<int, CommerceProductSeo> Seo = new();
        public List<CommerceAttribute> Attributes = new();
        public List<CommerceAttributeTerm> Terms = new();
        public List<CommerceProductVariation> Variations = new();
        public List<CommerceVariationAttribute> VariationAttrs = new();
        public List<CommerceProductCategory> Categories = new();
        public Dictionary<int, List<int>> ProductCats = new();

        public bool Access(int productId, int? siteId)
        {
            if (!AccessibleProducts.Contains(productId)) return false;
            if (!siteId.HasValue) return true;
            if (!ProductSites.TryGetValue(productId, out var s)) return true;
            return s == siteId.Value;
        }
    }

    internal sealed class MemAccess : ICommerceProductAccessRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemAccess(InMemoryCommerceCatalog d) => _d = d;
        public Task<bool> ProductAccessibleAsync(int productId, int? siteId) => Task.FromResult(_d.Access(productId, siteId));
        public Task<string> GetProductSkuAsync(int productId) => Task.FromResult(_d.ProductSkus.TryGetValue(productId, out var s) ? s : null);
        public Task<bool> SkuUsedOnProductAsync(string sku, int? exceptProductId)
        {
            var used = _d.ProductSkus.Any(kv => kv.Value == sku && (!exceptProductId.HasValue || kv.Key != exceptProductId.Value));
            return Task.FromResult(used);
        }
    }

    internal sealed class MemBrands : ICommerceBrandRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemBrands(InMemoryCommerceCatalog d) => _d = d;
        public Task<List<CommerceBrand>> ListAsync(int? siteId, int skip, int take) =>
            Task.FromResult(_d.Brands.Where(b => !siteId.HasValue || b.SiteId == null || b.SiteId == siteId).Skip(skip).Take(take).ToList());
        public Task<CommerceBrand> GetAsync(int id, int? siteId) =>
            Task.FromResult(_d.Brands.FirstOrDefault(b => b.Id == id && (!siteId.HasValue || b.SiteId == null || b.SiteId == siteId)));
        public Task<int> InsertAsync(CommerceBrand m) { m.Id = _d.NextId++; _d.Brands.Add(m); return Task.FromResult(m.Id); }
        public Task UpdateAsync(CommerceBrand m) { var i = _d.Brands.FindIndex(x => x.Id == m.Id); if (i >= 0) _d.Brands[i] = m; return Task.CompletedTask; }
        public Task DeleteAsync(int id, int? siteId) { _d.Brands.RemoveAll(b => b.Id == id); return Task.CompletedTask; }
        public Task SetProductBrandAsync(int productId, int brandId) { _d.ProductBrands[productId] = brandId; return Task.CompletedTask; }
        public Task ClearProductBrandAsync(int productId) { _d.ProductBrands.Remove(productId); return Task.CompletedTask; }
        public Task<int?> GetProductBrandIdAsync(int productId) => Task.FromResult(_d.ProductBrands.TryGetValue(productId, out var b) ? b : (int?)null);
    }

    internal sealed class MemTags : ICommerceTagRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemTags(InMemoryCommerceCatalog d) => _d = d;
        public Task<List<CommerceTag>> ListAsync(int? siteId, int skip, int take) =>
            Task.FromResult(_d.Tags.Where(t => !siteId.HasValue || t.SiteId == null || t.SiteId == siteId).Skip(skip).Take(take).ToList());
        public Task<CommerceTag> GetAsync(int id, int? siteId) =>
            Task.FromResult(_d.Tags.FirstOrDefault(t => t.Id == id && (!siteId.HasValue || t.SiteId == null || t.SiteId == siteId)));
        public Task<int> InsertAsync(CommerceTag m) { m.Id = _d.NextId++; _d.Tags.Add(m); return Task.FromResult(m.Id); }
        public Task UpdateAsync(CommerceTag m) { var i = _d.Tags.FindIndex(x => x.Id == m.Id); if (i >= 0) _d.Tags[i] = m; return Task.CompletedTask; }
        public Task DeleteAsync(int id, int? siteId) { _d.Tags.RemoveAll(t => t.Id == id); return Task.CompletedTask; }
        public Task<List<int>> ListProductTagIdsAsync(int productId) => Task.FromResult(_d.ProductTags.TryGetValue(productId, out var l) ? l : new List<int>());
        public Task SetProductTagsAsync(int productId, IReadOnlyList<int> tagIds) { _d.ProductTags[productId] = tagIds.ToList(); return Task.CompletedTask; }
    }

    internal sealed class MemImages : ICommerceProductImageRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemImages(InMemoryCommerceCatalog d) => _d = d;
        public Task<List<CommerceProductImage>> ListAsync(int productId) => Task.FromResult(_d.Images.Where(i => i.ProductId == productId).ToList());
        public Task<CommerceProductImage> GetAsync(int id) => Task.FromResult(_d.Images.FirstOrDefault(i => i.Id == id));
        public Task<int> InsertAsync(CommerceProductImage m) { m.Id = _d.NextId++; _d.Images.Add(m); return Task.FromResult(m.Id); }
        public Task UpdateAsync(CommerceProductImage m) { var i = _d.Images.FindIndex(x => x.Id == m.Id); if (i >= 0) _d.Images[i] = m; return Task.CompletedTask; }
        public Task DeleteAsync(int id) { _d.Images.RemoveAll(i => i.Id == id); return Task.CompletedTask; }
        public Task ClearPrimaryAsync(int productId) { foreach (var i in _d.Images.Where(x => x.ProductId == productId)) i.IsPrimary = false; return Task.CompletedTask; }
    }

    internal sealed class MemRels : ICommerceProductRelationRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemRels(InMemoryCommerceCatalog d) => _d = d;
        public Task<List<CommerceProductRelation>> ListAsync(int productId) => Task.FromResult(_d.Relations.Where(r => r.ProductId == productId).ToList());
        public Task<CommerceProductRelation> GetAsync(int id) => Task.FromResult(_d.Relations.FirstOrDefault(r => r.Id == id));
        public Task<int> InsertAsync(CommerceProductRelation m) { m.Id = _d.NextId++; _d.Relations.Add(m); return Task.FromResult(m.Id); }
        public Task UpdateAsync(CommerceProductRelation m) { var i = _d.Relations.FindIndex(x => x.Id == m.Id); if (i >= 0) _d.Relations[i] = m; return Task.CompletedTask; }
        public Task DeleteAsync(int id) { _d.Relations.RemoveAll(r => r.Id == id); return Task.CompletedTask; }
        public Task<bool> ExistsAsync(int productId, int relatedProductId, string relationType, int? exceptId) =>
            Task.FromResult(_d.Relations.Any(r => r.ProductId == productId && r.RelatedProductId == relatedProductId && r.RelationType == relationType && (!exceptId.HasValue || r.Id != exceptId.Value)));
    }

    internal sealed class MemSeo : ICommerceProductSeoRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemSeo(InMemoryCommerceCatalog d) => _d = d;
        public Task<CommerceProductSeo> GetAsync(int productId) => Task.FromResult(_d.Seo.TryGetValue(productId, out var s) ? s : null);
        public Task UpsertAsync(CommerceProductSeo m) { _d.Seo[m.ProductId] = m; return Task.CompletedTask; }
    }

    internal sealed class MemAttrs : ICommerceAttributeRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemAttrs(InMemoryCommerceCatalog d) => _d = d;
        public Task<List<CommerceAttribute>> ListAsync(int? siteId, int skip, int take) =>
            Task.FromResult(_d.Attributes.Where(a => !siteId.HasValue || a.SiteId == null || a.SiteId == siteId).Skip(skip).Take(take).ToList());
        public Task<CommerceAttribute> GetAsync(int id, int? siteId) =>
            Task.FromResult(_d.Attributes.FirstOrDefault(a => a.Id == id && (!siteId.HasValue || a.SiteId == null || a.SiteId == siteId)));
        public Task<int> InsertAsync(CommerceAttribute m) { m.Id = _d.NextId++; _d.Attributes.Add(m); return Task.FromResult(m.Id); }
        public Task UpdateAsync(CommerceAttribute m) { var i = _d.Attributes.FindIndex(x => x.Id == m.Id); if (i >= 0) _d.Attributes[i] = m; return Task.CompletedTask; }
        public Task DeleteAsync(int id, int? siteId) { _d.Attributes.RemoveAll(a => a.Id == id); return Task.CompletedTask; }
        public Task<List<CommerceAttributeTerm>> ListTermsAsync(int attributeId) => Task.FromResult(_d.Terms.Where(t => t.AttributeId == attributeId).ToList());
        public Task<CommerceAttributeTerm> GetTermAsync(int termId) => Task.FromResult(_d.Terms.FirstOrDefault(t => t.Id == termId));
        public Task<int> InsertTermAsync(CommerceAttributeTerm m) { m.Id = _d.NextId++; _d.Terms.Add(m); return Task.FromResult(m.Id); }
        public Task UpdateTermAsync(CommerceAttributeTerm m) { var i = _d.Terms.FindIndex(x => x.Id == m.Id); if (i >= 0) _d.Terms[i] = m; return Task.CompletedTask; }
        public Task DeleteTermAsync(int termId) { _d.Terms.RemoveAll(t => t.Id == termId); return Task.CompletedTask; }
    }

    internal sealed class MemVars : ICommerceVariationRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemVars(InMemoryCommerceCatalog d) => _d = d;
        public Task<List<CommerceProductVariation>> ListAsync(int productId) => Task.FromResult(_d.Variations.Where(v => v.ProductId == productId).ToList());
        public Task<CommerceProductVariation> GetAsync(int id) => Task.FromResult(_d.Variations.FirstOrDefault(v => v.Id == id));
        public Task<int> InsertAsync(CommerceProductVariation m) { m.Id = _d.NextId++; _d.Variations.Add(m); return Task.FromResult(m.Id); }
        public Task UpdateAsync(CommerceProductVariation m) { var i = _d.Variations.FindIndex(x => x.Id == m.Id); if (i >= 0) _d.Variations[i] = m; return Task.CompletedTask; }
        public Task DeleteAsync(int id) { _d.VariationAttrs.RemoveAll(a => a.VariationId == id); _d.Variations.RemoveAll(v => v.Id == id); return Task.CompletedTask; }
        public Task<bool> SkuExistsAsync(string sku, int? siteId, int? exceptVariationId) =>
            Task.FromResult(_d.Variations.Any(v => v.Sku == sku && (!siteId.HasValue || v.SiteId == siteId) && (!exceptVariationId.HasValue || v.Id != exceptVariationId.Value)));
        public Task ReplaceAttributesAsync(int variationId, IReadOnlyList<CommerceVariationAttribute> attrs)
        {
            _d.VariationAttrs.RemoveAll(a => a.VariationId == variationId);
            foreach (var a in attrs) { a.Id = _d.NextId++; a.VariationId = variationId; _d.VariationAttrs.Add(a); }
            return Task.CompletedTask;
        }
        public Task<List<CommerceVariationAttribute>> ListAttributesAsync(int variationId) => Task.FromResult(_d.VariationAttrs.Where(a => a.VariationId == variationId).ToList());
        public Task SetDefaultAsync(int productId, int variationId)
        {
            foreach (var v in _d.Variations.Where(x => x.ProductId == productId))
                v.IsDefault = v.Id == variationId;
            return Task.CompletedTask;
        }
        public Task DecrementStockAsync(int variationId, int quantity)
        {
            var v = _d.Variations.FirstOrDefault(x => x.Id == variationId);
            if (v == null || v.Stock < quantity) throw new InvalidOperationException("موجودی تنوع کافی نیست.");
            v.Stock -= quantity;
            return Task.CompletedTask;
        }
    }

    internal sealed class MemCats : ICommerceProductCategoryRepository
    {
        private readonly InMemoryCommerceCatalog _d;
        public MemCats(InMemoryCommerceCatalog d) => _d = d;
        public Task<List<CommerceProductCategory>> ListAsync() => Task.FromResult(_d.Categories.ToList());
        public Task<int> InsertAsync(string name) { var c = new CommerceProductCategory { Id = _d.NextId++, Name = name }; _d.Categories.Add(c); return Task.FromResult(c.Id); }
        public Task UpdateAsync(int id, string name) { var c = _d.Categories.FirstOrDefault(x => x.Id == id); if (c != null) c.Name = name; return Task.CompletedTask; }
        public Task DeleteAsync(int id) { _d.Categories.RemoveAll(c => c.Id == id); return Task.CompletedTask; }
        public Task<List<int>> ListProductCategoryIdsAsync(int productId) => Task.FromResult(_d.ProductCats.TryGetValue(productId, out var l) ? l : new List<int>());
        public Task SetProductCategoriesAsync(int productId, IReadOnlyList<int> categoryIds) { _d.ProductCats[productId] = categoryIds.ToList(); return Task.CompletedTask; }
    }
}
