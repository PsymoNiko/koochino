using System;
using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using MyShop.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceProductAccessRepository : ICommerceProductAccessRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceProductAccessRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<bool> ProductAccessibleAsync(int productId, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM koch.Products p
                    LEFT JOIN koch.Stores st ON st.StoreID = p.StoreId
                    WHERE p.Id = @ProductId
                      AND (@SiteId IS NULL OR st.SiteId = @SiteId OR p.StoreId IS NULL)
                ) THEN 1 ELSE 0 END",
                new { ProductId = productId, SiteId = siteId });
            return n == 1;
        }

        public async Task<string> GetProductSkuAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<string>(
                "SELECT Sku FROM koch.Products WHERE Id=@Id", new { Id = productId });
        }

        public async Task<bool> SkuUsedOnProductAsync(string sku, int? exceptProductId)
        {
            if (string.IsNullOrWhiteSpace(sku)) return false;
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM koch.Products WHERE Sku=@Sku AND (@Except IS NULL OR Id<>@Except)) THEN 1 ELSE 0 END",
                new { Sku = sku, Except = exceptProductId });
            return n == 1;
        }
    }

    public class CommerceBrandRepository : ICommerceBrandRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceBrandRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceBrand>> ListAsync(int? siteId, int skip, int take)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceBrand>(@"
                SELECT * FROM koch.CommerceBrands
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)
                ORDER BY SortOrder, Id
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
                new { SiteId = siteId, Skip = skip, Take = take });
            return rows.ToList();
        }

        public async Task<CommerceBrand> GetAsync(int id, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceBrand>(@"
                SELECT * FROM koch.CommerceBrands
                WHERE Id=@Id AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)",
                new { Id = id, SiteId = siteId });
        }

        public async Task<int> InsertAsync(CommerceBrand m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceBrands (SiteId, Name, Slug, Description, LogoUrl, IsActive, SortOrder)
                OUTPUT INSERTED.Id
                VALUES (@SiteId, @Name, @Slug, @Description, @LogoUrl, @IsActive, @SortOrder)", m);
        }

        public async Task UpdateAsync(CommerceBrand m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceBrands
                SET Name=@Name, Slug=@Slug, Description=@Description, LogoUrl=@LogoUrl,
                    IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME()
                WHERE Id=@Id", m);
        }

        public async Task DeleteAsync(int id, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.CommerceProductBrands WHERE BrandId=@Id", "محصول"));
            await conn.ExecuteAsync(@"
                DELETE FROM koch.CommerceBrands
                WHERE Id=@Id AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)",
                new { Id = id, SiteId = siteId });
        }

        public async Task SetProductBrandAsync(int productId, int brandId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                MERGE koch.CommerceProductBrands AS t
                USING (SELECT @ProductId AS ProductId, @BrandId AS BrandId) AS s
                ON t.ProductId = s.ProductId
                WHEN MATCHED THEN UPDATE SET BrandId = s.BrandId
                WHEN NOT MATCHED THEN INSERT (ProductId, BrandId) VALUES (s.ProductId, s.BrandId);",
                new { ProductId = productId, BrandId = brandId });
        }

        public async Task ClearProductBrandAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceProductBrands WHERE ProductId=@ProductId", new { ProductId = productId });
        }

        public async Task<int?> GetProductBrandIdAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int?>(
                "SELECT BrandId FROM koch.CommerceProductBrands WHERE ProductId=@ProductId", new { ProductId = productId });
        }
    }

    public class CommerceTagRepository : ICommerceTagRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceTagRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceTag>> ListAsync(int? siteId, int skip, int take)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceTag>(@"
                SELECT * FROM koch.CommerceTags
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)
                ORDER BY SortOrder, Id
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
                new { SiteId = siteId, Skip = skip, Take = take });
            return rows.ToList();
        }

        public async Task<CommerceTag> GetAsync(int id, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceTag>(
                "SELECT * FROM koch.CommerceTags WHERE Id=@Id AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)",
                new { Id = id, SiteId = siteId });
        }

        public async Task<int> InsertAsync(CommerceTag m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceTags (SiteId, Name, Slug, IsActive, SortOrder)
                OUTPUT INSERTED.Id VALUES (@SiteId, @Name, @Slug, @IsActive, @SortOrder)", m);
        }

        public async Task UpdateAsync(CommerceTag m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.CommerceTags SET Name=@Name, Slug=@Slug, IsActive=@IsActive, SortOrder=@SortOrder WHERE Id=@Id", m);
        }

        public async Task DeleteAsync(int id, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.CommerceProductTags WHERE TagId=@Id", "محصول"));
            await conn.ExecuteAsync("DELETE FROM koch.CommerceTags WHERE Id=@Id AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)",
                new { Id = id, SiteId = siteId });
        }

        public async Task<List<int>> ListProductTagIdsAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<int>("SELECT TagId FROM koch.CommerceProductTags WHERE ProductId=@ProductId", new { ProductId = productId });
            return rows.ToList();
        }

        public async Task SetProductTagsAsync(int productId, IReadOnlyList<int> tagIds)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceProductTags WHERE ProductId=@ProductId", new { ProductId = productId });
            foreach (var tagId in tagIds)
                await conn.ExecuteAsync("INSERT INTO koch.CommerceProductTags (ProductId, TagId) VALUES (@ProductId, @TagId)",
                    new { ProductId = productId, TagId = tagId });
        }
    }

    public class CommerceProductImageRepository : ICommerceProductImageRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceProductImageRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceProductImage>> ListAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceProductImage>(
                "SELECT * FROM koch.CommerceProductImages WHERE ProductId=@ProductId ORDER BY SortOrder, Id",
                new { ProductId = productId });
            return rows.ToList();
        }

        public async Task<CommerceProductImage> GetAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceProductImage>(
                "SELECT * FROM koch.CommerceProductImages WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> InsertAsync(CommerceProductImage m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceProductImages (ProductId, Url, AltText, Title, SortOrder, IsPrimary, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@ProductId, @Url, @AltText, @Title, @SortOrder, @IsPrimary, @IsActive)", m);
        }

        public async Task UpdateAsync(CommerceProductImage m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceProductImages
                SET Url=@Url, AltText=@AltText, Title=@Title, SortOrder=@SortOrder, IsPrimary=@IsPrimary, IsActive=@IsActive
                WHERE Id=@Id", m);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceProductImages WHERE Id=@Id", new { Id = id });
        }

        public async Task ClearPrimaryAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.CommerceProductImages SET IsPrimary=0 WHERE ProductId=@ProductId", new { ProductId = productId });
        }
    }

    public class CommerceProductRelationRepository : ICommerceProductRelationRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceProductRelationRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceProductRelation>> ListAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceProductRelation>(@"
                SELECT r.*, p.Name AS RelatedProductName
                FROM koch.CommerceProductRelations r
                INNER JOIN koch.Products p ON p.Id = r.RelatedProductId
                WHERE r.ProductId=@ProductId
                ORDER BY r.RelationType, r.SortOrder, r.Id",
                new { ProductId = productId });
            return rows.ToList();
        }

        public async Task<CommerceProductRelation> GetAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceProductRelation>(
                "SELECT * FROM koch.CommerceProductRelations WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> InsertAsync(CommerceProductRelation m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceProductRelations (ProductId, RelatedProductId, RelationType, SortOrder, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@ProductId, @RelatedProductId, @RelationType, @SortOrder, @IsActive)", m);
        }

        public async Task UpdateAsync(CommerceProductRelation m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceProductRelations
                SET RelatedProductId=@RelatedProductId, RelationType=@RelationType, SortOrder=@SortOrder, IsActive=@IsActive
                WHERE Id=@Id", m);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceProductRelations WHERE Id=@Id", new { Id = id });
        }

        public async Task<bool> ExistsAsync(int productId, int relatedProductId, string relationType, int? exceptId)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM koch.CommerceProductRelations
                    WHERE ProductId=@ProductId AND RelatedProductId=@RelatedProductId AND RelationType=@RelationType
                      AND (@Except IS NULL OR Id<>@Except)
                ) THEN 1 ELSE 0 END",
                new { ProductId = productId, RelatedProductId = relatedProductId, RelationType = relationType, Except = exceptId });
            return n == 1;
        }
    }

    public class CommerceProductSeoRepository : ICommerceProductSeoRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceProductSeoRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<CommerceProductSeo> GetAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceProductSeo>(
                "SELECT * FROM koch.CommerceProductSeo WHERE ProductId=@ProductId", new { ProductId = productId });
        }

        public async Task UpsertAsync(CommerceProductSeo m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                MERGE koch.CommerceProductSeo AS t
                USING (SELECT @ProductId AS ProductId) AS s ON t.ProductId = s.ProductId
                WHEN MATCHED THEN UPDATE SET
                    MetaTitle=@MetaTitle, MetaDescription=@MetaDescription, CanonicalUrl=@CanonicalUrl,
                    NoIndex=@NoIndex, OpenGraphTitle=@OpenGraphTitle, OpenGraphDescription=@OpenGraphDescription,
                    UpdatedAt=SYSDATETIME()
                WHEN NOT MATCHED THEN INSERT
                    (ProductId, MetaTitle, MetaDescription, CanonicalUrl, NoIndex, OpenGraphTitle, OpenGraphDescription)
                    VALUES (@ProductId, @MetaTitle, @MetaDescription, @CanonicalUrl, @NoIndex, @OpenGraphTitle, @OpenGraphDescription);", m);
        }
    }

    public class CommerceAttributeRepository : ICommerceAttributeRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceAttributeRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceAttribute>> ListAsync(int? siteId, int skip, int take)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceAttribute>(@"
                SELECT * FROM koch.CommerceAttributes
                WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)
                ORDER BY SortOrder, Id
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
                new { SiteId = siteId, Skip = skip, Take = take });
            return rows.ToList();
        }

        public async Task<CommerceAttribute> GetAsync(int id, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceAttribute>(
                "SELECT * FROM koch.CommerceAttributes WHERE Id=@Id AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)",
                new { Id = id, SiteId = siteId });
        }

        public async Task<int> InsertAsync(CommerceAttribute m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceAttributes (SiteId, Name, Slug, SortOrder, IsActive)
                OUTPUT INSERTED.Id VALUES (@SiteId, @Name, @Slug, @SortOrder, @IsActive)", m);
        }

        public async Task UpdateAsync(CommerceAttribute m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.CommerceAttributes SET Name=@Name, Slug=@Slug, SortOrder=@SortOrder, IsActive=@IsActive WHERE Id=@Id", m);
        }

        public async Task DeleteAsync(int id, int? siteId)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.CommerceProductVariationAttributes va INNER JOIN koch.CommerceAttributeTerms t ON t.Id = va.TermId WHERE t.AttributeId=@Id", "تنوع محصول"));
            await conn.ExecuteAsync("DELETE FROM koch.CommerceAttributeTerms WHERE AttributeId=@Id", new { Id = id });
            await conn.ExecuteAsync("DELETE FROM koch.CommerceAttributes WHERE Id=@Id AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)",
                new { Id = id, SiteId = siteId });
        }

        public async Task<List<CommerceAttributeTerm>> ListTermsAsync(int attributeId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceAttributeTerm>(
                "SELECT * FROM koch.CommerceAttributeTerms WHERE AttributeId=@AttributeId ORDER BY SortOrder, Id",
                new { AttributeId = attributeId });
            return rows.ToList();
        }

        public async Task<CommerceAttributeTerm> GetTermAsync(int termId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceAttributeTerm>(
                "SELECT * FROM koch.CommerceAttributeTerms WHERE Id=@Id", new { Id = termId });
        }

        public async Task<int> InsertTermAsync(CommerceAttributeTerm m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceAttributeTerms (AttributeId, Name, Slug, SortOrder, IsActive)
                OUTPUT INSERTED.Id VALUES (@AttributeId, @Name, @Slug, @SortOrder, @IsActive)", m);
        }

        public async Task UpdateTermAsync(CommerceAttributeTerm m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.CommerceAttributeTerms SET Name=@Name, Slug=@Slug, SortOrder=@SortOrder, IsActive=@IsActive WHERE Id=@Id", m);
        }

        public async Task DeleteTermAsync(int termId)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = termId },
                ("SELECT COUNT(1) FROM koch.CommerceProductVariationAttributes WHERE TermId=@Id", "تنوع محصول"));
            await conn.ExecuteAsync("DELETE FROM koch.CommerceAttributeTerms WHERE Id=@Id", new { Id = termId });
        }
    }

    public class CommerceVariationRepository : ICommerceVariationRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceVariationRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceProductVariation>> ListAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceProductVariation>(
                "SELECT * FROM koch.CommerceProductVariations WHERE ProductId=@ProductId ORDER BY SortOrder, Id",
                new { ProductId = productId });
            return rows.ToList();
        }

        public async Task<CommerceProductVariation> GetAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceProductVariation>(
                "SELECT * FROM koch.CommerceProductVariations WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> InsertAsync(CommerceProductVariation m)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.CommerceProductVariations
                    (ProductId, SiteId, Sku, Price, CompareAtPrice, Stock, ImageUrl, IsActive, SortOrder)
                OUTPUT INSERTED.Id
                VALUES (@ProductId, @SiteId, @Sku, @Price, @CompareAtPrice, @Stock, @ImageUrl, @IsActive, @SortOrder)", m);
        }

        public async Task UpdateAsync(CommerceProductVariation m)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceProductVariations
                SET Sku=@Sku, Price=@Price, CompareAtPrice=@CompareAtPrice, Stock=@Stock, ImageUrl=@ImageUrl,
                    IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME()
                WHERE Id=@Id", m);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.OrderItems WHERE VariationId=@Id", "سفارش"),
                ("SELECT COUNT(1) FROM koch.CommerceVariationCartItems WHERE VariationId=@Id", "سبد خرید"));
            await conn.ExecuteAsync("DELETE FROM koch.CommerceProductVariationAttributes WHERE VariationId=@Id", new { Id = id });
            await conn.ExecuteAsync("DELETE FROM koch.CommerceProductVariations WHERE Id=@Id", new { Id = id });
        }

        public async Task<bool> SkuExistsAsync(string sku, int? siteId, int? exceptVariationId)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM koch.CommerceProductVariations
                    WHERE Sku=@Sku AND (@SiteId IS NULL OR SiteId=@SiteId)
                      AND (@Except IS NULL OR Id<>@Except)
                ) THEN 1 ELSE 0 END",
                new { Sku = sku, SiteId = siteId, Except = exceptVariationId });
            return n == 1;
        }

        public async Task ReplaceAttributesAsync(int variationId, IReadOnlyList<CommerceVariationAttribute> attrs)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceProductVariationAttributes WHERE VariationId=@VariationId", new { VariationId = variationId });
            foreach (var a in attrs)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO koch.CommerceProductVariationAttributes (VariationId, AttributeId, TermId)
                    VALUES (@VariationId, @AttributeId, @TermId)",
                    new { VariationId = variationId, a.AttributeId, a.TermId });
            }
        }

        public async Task SetDefaultAsync(int productId, int variationId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.CommerceProductVariations SET IsDefault=0, UpdatedAt=SYSDATETIME() WHERE ProductId=@ProductId;
                UPDATE koch.CommerceProductVariations SET IsDefault=1, UpdatedAt=SYSDATETIME() WHERE Id=@Id AND ProductId=@ProductId;",
                new { ProductId = productId, Id = variationId });
        }

        public async Task DecrementStockAsync(int variationId, int quantity)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteAsync(@"
                UPDATE koch.CommerceProductVariations
                SET Stock = Stock - @Qty, UpdatedAt = SYSDATETIME()
                WHERE Id=@Id AND Stock >= @Qty",
                new { Id = variationId, Qty = quantity });
            if (n == 0)
                throw new InvalidOperationException("موجودی تنوع کافی نیست.");
        }

        public async Task<List<CommerceVariationAttribute>> ListAttributesAsync(int variationId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceVariationAttribute>(@"
                SELECT va.*, a.Name AS AttributeName, t.Name AS TermName
                FROM koch.CommerceProductVariationAttributes va
                INNER JOIN koch.CommerceAttributes a ON a.Id = va.AttributeId
                INNER JOIN koch.CommerceAttributeTerms t ON t.Id = va.TermId
                WHERE va.VariationId=@VariationId",
                new { VariationId = variationId });
            return rows.ToList();
        }
    }

    public class CommerceProductCategoryRepository : ICommerceProductCategoryRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceProductCategoryRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceProductCategory>> ListAsync()
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceProductCategory>(
                "SELECT Id, Name FROM koch.ProductCategories ORDER BY Name");
            return rows.ToList();
        }

        public async Task<int> InsertAsync(string name)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                "INSERT INTO koch.ProductCategories (Name) OUTPUT INSERTED.Id VALUES (@Name)", new { Name = name });
        }

        public async Task UpdateAsync(int id, string name)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.ProductCategories SET Name=@Name WHERE Id=@Id", new { Id = id, Name = name });
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.ProductCategoryMap WHERE CategoryId=@Id", "محصول"),
                ("SELECT COUNT(1) FROM koch.Products WHERE CategoryId=@Id", "محصول"));
            await conn.ExecuteAsync("DELETE FROM koch.ProductCategories WHERE Id=@Id", new { Id = id });
        }

        public async Task<List<int>> ListProductCategoryIdsAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<int>(
                "SELECT CategoryId FROM koch.ProductCategoryMap WHERE ProductId=@ProductId", new { ProductId = productId });
            return rows.ToList();
        }

        public async Task SetProductCategoriesAsync(int productId, IReadOnlyList<int> categoryIds)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.ProductCategoryMap WHERE ProductId=@ProductId", new { ProductId = productId });
            foreach (var categoryId in categoryIds)
                await conn.ExecuteAsync(
                    "INSERT INTO koch.ProductCategoryMap (ProductId, CategoryId) VALUES (@ProductId, @CategoryId)",
                    new { ProductId = productId, CategoryId = categoryId });
        }
    }
}
