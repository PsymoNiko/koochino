using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceVariationCartRepository : ICommerceVariationCartRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceVariationCartRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<CommerceVariationCartItem> GetAsync(int cartId, int variationId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceVariationCartItem>(@"
                SELECT i.*, v.Sku, p.Name AS ProductName
                FROM koch.CommerceVariationCartItems i
                INNER JOIN koch.CommerceProductVariations v ON v.Id = i.VariationId
                INNER JOIN koch.Products p ON p.Id = i.ProductId
                WHERE i.CartId=@CartId AND i.VariationId=@VariationId",
                new { CartId = cartId, VariationId = variationId });
        }

        public async Task<List<CommerceVariationCartItem>> ListAsync(int cartId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceVariationCartItem>(@"
                SELECT i.*, v.Sku, p.Name AS ProductName
                FROM koch.CommerceVariationCartItems i
                INNER JOIN koch.CommerceProductVariations v ON v.Id = i.VariationId
                INNER JOIN koch.Products p ON p.Id = i.ProductId
                WHERE i.CartId=@CartId
                ORDER BY i.Id",
                new { CartId = cartId });
            return rows.ToList();
        }

        public async Task UpsertAsync(CommerceVariationCartItem item)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                MERGE koch.CommerceVariationCartItems AS t
                USING (SELECT @CartId CartId, @VariationId VariationId) AS s
                ON t.CartId = s.CartId AND t.VariationId = s.VariationId
                WHEN MATCHED THEN UPDATE SET
                    Quantity=@Quantity,
                    UnitPriceSnapshot=@UnitPriceSnapshot,
                    CompareAtPriceSnapshot=@CompareAtPriceSnapshot,
                    UpdatedAt=SYSDATETIME()
                WHEN NOT MATCHED THEN INSERT
                    (CartId, ProductId, VariationId, Quantity, UnitPriceSnapshot, CompareAtPriceSnapshot)
                    VALUES (@CartId, @ProductId, @VariationId, @Quantity, @UnitPriceSnapshot, @CompareAtPriceSnapshot);
                UPDATE koch.CommerceCarts SET UpdatedAt=SYSDATETIME() WHERE Id=@CartId;",
                item);
        }

        public async Task RemoveAsync(int cartId, int variationId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.CommerceVariationCartItems WHERE CartId=@CartId AND VariationId=@VariationId",
                new { CartId = cartId, VariationId = variationId });
        }

        public async Task ClearAsync(int cartId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.CommerceVariationCartItems WHERE CartId=@CartId", new { CartId = cartId });
        }
    }
}
