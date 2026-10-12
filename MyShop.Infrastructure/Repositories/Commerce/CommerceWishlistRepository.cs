using Dapper;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class CommerceWishlistRepository : ICommerceWishlistRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CommerceWishlistRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<CommerceWishlistItem>> ListByUserAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<CommerceWishlistItem>(
                "SELECT * FROM koch.CommerceWishlistItems WHERE UserId=@UserId ORDER BY Id DESC",
                new { UserId = userId });
            return rows.ToList();
        }

        public async Task<CommerceWishlistItem> GetAsync(int userId, int productId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CommerceWishlistItem>(
                "SELECT TOP 1 * FROM koch.CommerceWishlistItems WHERE UserId=@UserId AND ProductId=@ProductId",
                new { UserId = userId, ProductId = productId });
        }

        public async Task InsertAsync(int userId, int productId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "INSERT INTO koch.CommerceWishlistItems (UserId, ProductId) VALUES (@UserId, @ProductId)",
                new { UserId = userId, ProductId = productId });
        }

        public async Task DeleteAsync(int userId, int productId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.CommerceWishlistItems WHERE UserId=@UserId AND ProductId=@ProductId",
                new { UserId = userId, ProductId = productId });
        }
    }
}
