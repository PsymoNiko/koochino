using Dapper;
using MyShop.Application.Interfaces.Assets;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Assets;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class AssetRepository : IAssetRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AssetRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<AssetDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AssetDto>(
                "koch.spAssets_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<AssetDto>> GetAllAsync(int? storeId = null, int? companyId = null, int? categoryId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<AssetDto>(
                "koch.spAssets_List",
                new { StoreId = storeId, CompanyId = companyId, CategoryId = categoryId },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<AssetDto>();
        }

        public async Task<List<AssetCategoryItemDto>> GetCategoriesAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<AssetCategoryItemDto>(
                "koch.spAssetCategories_List",
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<AssetCategoryItemDto>();
        }

        public async Task<int> InsertAsync(Asset entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("CategoryId", entity.CategoryId);
            p.Add("Code", entity.Code);
            p.Add("Name", entity.Name);
            p.Add("Description", entity.Description);
            p.Add("PurchaseDate", entity.PurchaseDate);
            p.Add("PurchasePrice", entity.PurchasePrice);
            p.Add("CurrentValue", entity.CurrentValue);
            p.Add("Location", entity.Location);
            p.Add("AssignedTo", entity.AssignedTo);
            p.Add("PersonnelId", entity.PersonnelId);
            p.Add("Status", entity.Status);
            p.Add("WarrantyEndDate", entity.WarrantyEndDate);
            p.Add("DepreciationRate", entity.DepreciationRate);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("SerialNo", entity.SerialNo);
            p.Add("Notes", entity.Notes);
            p.Add("IsActive", entity.IsActive);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAssets_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Asset entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("CategoryId", entity.CategoryId);
            p.Add("Code", entity.Code);
            p.Add("Name", entity.Name);
            p.Add("Description", entity.Description);
            p.Add("PurchaseDate", entity.PurchaseDate);
            p.Add("PurchasePrice", entity.PurchasePrice);
            p.Add("CurrentValue", entity.CurrentValue);
            p.Add("Location", entity.Location);
            p.Add("AssignedTo", entity.AssignedTo);
            p.Add("PersonnelId", entity.PersonnelId);
            p.Add("Status", entity.Status);
            p.Add("WarrantyEndDate", entity.WarrantyEndDate);
            p.Add("DepreciationRate", entity.DepreciationRate);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("SerialNo", entity.SerialNo);
            p.Add("Notes", entity.Notes);
            p.Add("IsActive", entity.IsActive);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAssets_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAssets_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
