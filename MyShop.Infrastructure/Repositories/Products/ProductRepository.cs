using Dapper;
using MyShop.Application.Interfaces.Products;
using MyShop.Core.Interfaces;
using MyShop.Infrastructure.Data;
using MyShop.Core.Models.Products;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProductRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<ProductDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ProductDto>(
                "koch.spProducts_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ProductDto>(
                "koch.spProducts_List",
                new { StoreId = storeId, CompanyId = companyId },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<ProductDto>();
        }

        public async Task<int> InsertAsync(Product entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("CategoryId", entity.CategoryId);
            p.Add("Name", entity.Name);
            p.Add("Slug", entity.Slug);
            p.Add("Description", entity.Description);
            p.Add("ShortDescription", entity.ShortDescription);
            p.Add("Price", entity.Price);
            p.Add("CompareAtPrice", entity.CompareAtPrice);
            p.Add("Cost", entity.Cost);
            p.Add("Sku", entity.Sku);
            p.Add("Barcode", entity.Barcode);
            p.Add("Quantity", entity.Quantity);
            p.Add("IsActive", entity.IsActive);
            p.Add("IsFeatured", entity.IsFeatured);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProducts_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Product entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("CategoryId", entity.CategoryId);
            p.Add("Name", entity.Name);
            p.Add("Slug", entity.Slug);
            p.Add("Description", entity.Description);
            p.Add("ShortDescription", entity.ShortDescription);
            p.Add("Price", entity.Price);
            p.Add("CompareAtPrice", entity.CompareAtPrice);
            p.Add("Cost", entity.Cost);
            p.Add("Sku", entity.Sku);
            p.Add("Barcode", entity.Barcode);
            p.Add("Quantity", entity.Quantity);
            p.Add("IsActive", entity.IsActive);
            p.Add("IsFeatured", entity.IsFeatured);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProducts_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = id },
                ("SELECT COUNT(1) FROM koch.OrderItems WHERE ProductId=@Id", "سفارش"),
                ("SELECT COUNT(1) FROM koch.CommerceCartItems WHERE ProductId=@Id", "سبد خرید"),
                ("SELECT COUNT(1) FROM koch.CommerceWishlistItems WHERE ProductId=@Id", "علاقه‌مندی"));
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProducts_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task SetUnitIdAsync(int productId, int? unitId)
        {
            try
            {
                using var conn = _connectionFactory.CreateConnection();
                await conn.ExecuteAsync(
                    "UPDATE koch.Products SET UnitId=@UnitId WHERE Id=@Id",
                    new { Id = productId, UnitId = unitId });
            }
            catch
            {
                // ستون UnitId ممکن است هنوز اضافه نشده باشد
            }
        }
    }
}
