using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public interface ICommerceProductAccessRepository
    {
        Task<bool> ProductAccessibleAsync(int productId, int? siteId);
        Task<string> GetProductSkuAsync(int productId);
        Task<bool> SkuUsedOnProductAsync(string sku, int? exceptProductId);
    }

    public interface ICommerceBrandRepository
    {
        Task<List<CommerceBrand>> ListAsync(int? siteId, int skip, int take);
        Task<CommerceBrand> GetAsync(int id, int? siteId);
        Task<int> InsertAsync(CommerceBrand m);
        Task UpdateAsync(CommerceBrand m);
        Task DeleteAsync(int id, int? siteId);
        Task SetProductBrandAsync(int productId, int brandId);
        Task ClearProductBrandAsync(int productId);
        Task<int?> GetProductBrandIdAsync(int productId);
    }

    public interface ICommerceTagRepository
    {
        Task<List<CommerceTag>> ListAsync(int? siteId, int skip, int take);
        Task<CommerceTag> GetAsync(int id, int? siteId);
        Task<int> InsertAsync(CommerceTag m);
        Task UpdateAsync(CommerceTag m);
        Task DeleteAsync(int id, int? siteId);
        Task<List<int>> ListProductTagIdsAsync(int productId);
        Task SetProductTagsAsync(int productId, IReadOnlyList<int> tagIds);
    }

    public interface ICommerceProductImageRepository
    {
        Task<List<CommerceProductImage>> ListAsync(int productId);
        Task<CommerceProductImage> GetAsync(int id);
        Task<int> InsertAsync(CommerceProductImage m);
        Task UpdateAsync(CommerceProductImage m);
        Task DeleteAsync(int id);
        Task ClearPrimaryAsync(int productId);
    }

    public interface ICommerceProductRelationRepository
    {
        Task<List<CommerceProductRelation>> ListAsync(int productId);
        Task<CommerceProductRelation> GetAsync(int id);
        Task<int> InsertAsync(CommerceProductRelation m);
        Task UpdateAsync(CommerceProductRelation m);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int productId, int relatedProductId, string relationType, int? exceptId);
    }

    public interface ICommerceProductSeoRepository
    {
        Task<CommerceProductSeo> GetAsync(int productId);
        Task UpsertAsync(CommerceProductSeo m);
    }

    public interface ICommerceAttributeRepository
    {
        Task<List<CommerceAttribute>> ListAsync(int? siteId, int skip, int take);
        Task<CommerceAttribute> GetAsync(int id, int? siteId);
        Task<int> InsertAsync(CommerceAttribute m);
        Task UpdateAsync(CommerceAttribute m);
        Task DeleteAsync(int id, int? siteId);
        Task<List<CommerceAttributeTerm>> ListTermsAsync(int attributeId);
        Task<CommerceAttributeTerm> GetTermAsync(int termId);
        Task<int> InsertTermAsync(CommerceAttributeTerm m);
        Task UpdateTermAsync(CommerceAttributeTerm m);
        Task DeleteTermAsync(int termId);
    }

    public interface ICommerceVariationRepository
    {
        Task<List<CommerceProductVariation>> ListAsync(int productId);
        Task<CommerceProductVariation> GetAsync(int id);
        Task<int> InsertAsync(CommerceProductVariation m);
        Task UpdateAsync(CommerceProductVariation m);
        Task DeleteAsync(int id);
        Task<bool> SkuExistsAsync(string sku, int? siteId, int? exceptVariationId);
        Task ReplaceAttributesAsync(int variationId, IReadOnlyList<CommerceVariationAttribute> attrs);
        Task<List<CommerceVariationAttribute>> ListAttributesAsync(int variationId);
        Task SetDefaultAsync(int productId, int variationId);
        Task DecrementStockAsync(int variationId, int quantity);
    }

    public interface ICommerceProductCategoryRepository
    {
        Task<List<CommerceProductCategory>> ListAsync();
        Task<int> InsertAsync(string name);
        Task UpdateAsync(int id, string name);
        Task DeleteAsync(int id);
        Task<List<int>> ListProductCategoryIdsAsync(int productId);
        Task SetProductCategoriesAsync(int productId, IReadOnlyList<int> categoryIds);
    }

    public interface ICommerceBrandService
    {
        Task<List<CommerceBrand>> ListAsync(int? siteId, int skip = 0, int take = 200);
        Task<int> SaveAsync(CommerceBrand m);
        Task DeleteAsync(int id, int? siteId);
        Task AssignToProductAsync(int productId, int brandId, int? siteId);
    }

    public interface ICommerceTagService
    {
        Task<List<CommerceTag>> ListAsync(int? siteId, int skip = 0, int take = 200);
        Task<int> SaveAsync(CommerceTag m);
        Task DeleteAsync(int id, int? siteId);
        Task SetProductTagsAsync(int productId, IReadOnlyList<int> tagIds, int? siteId);
        Task<List<int>> GetProductTagIdsAsync(int productId, int? siteId);
    }

    public interface ICommerceProductImageService
    {
        Task<List<CommerceProductImage>> ListAsync(int productId, int? siteId);
        Task<int> SaveAsync(CommerceProductImage m, int? siteId);
        Task DeleteAsync(int id, int? siteId);
    }

    public interface ICommerceProductRelationService
    {
        Task<List<CommerceProductRelation>> ListAsync(int productId, int? siteId);
        Task<int> SaveAsync(CommerceProductRelation m, int? siteId);
        Task DeleteAsync(int id, int? siteId);
    }

    public interface ICommerceProductSeoService
    {
        Task<CommerceProductSeo> GetAsync(int productId, int? siteId);
        Task SaveAsync(CommerceProductSeo m, int? siteId);
    }

    public interface ICommerceAttributeService
    {
        Task<List<CommerceAttribute>> ListAsync(int? siteId, int skip = 0, int take = 200);
        Task<int> SaveAttributeAsync(CommerceAttribute m);
        Task DeleteAttributeAsync(int id, int? siteId);
        Task<int> SaveTermAsync(CommerceAttributeTerm m, int? siteId);
        Task DeleteTermAsync(int termId, int? siteId);
    }

    public interface ICommerceVariationService
    {
        Task<List<CommerceProductVariation>> ListAsync(int productId, int? siteId);
        Task<int> SaveAsync(CommerceProductVariation m, int? siteId);
        Task DeleteAsync(int id, int? siteId);
        Task SetAttributesAsync(int variationId, IReadOnlyList<CommerceVariationAttribute> attrs, int? siteId);
        Task SetDefaultAsync(int variationId, int? siteId);
        Task<CommerceProductVariation> GetDefaultAsync(int productId, int? siteId);
    }

    public interface ICommerceProductCategoryService
    {
        Task<List<CommerceProductCategory>> ListAsync();
        Task<int> SaveAsync(int id, string name);
        Task DeleteAsync(int id);
        Task SetProductCategoriesAsync(int productId, IReadOnlyList<int> categoryIds, int? siteId);
        Task<List<int>> GetProductCategoryIdsAsync(int productId, int? siteId);
    }

    /// <summary>StoreOwner may only mutate catalog extras for products of stores they own.</summary>
    public interface ICommerceSellerCatalogGuard
    {
        Task EnsureOwnsProductAsync(int userId, int productId);
    }

    public interface ICommerceShopCatalogService
    {
        Task<List<MyShop.Core.Models.Products.ProductDto>> ListVisibleAsync(int? siteId);
        Task<MyShop.Core.Models.Products.ProductDto> GetVisibleAsync(int productId, int? siteId);
    }
}
