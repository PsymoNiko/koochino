using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Data
{
    public class InviteRepository : IInviteRepository
    {
        private readonly IDbConnectionFactory _factory;

        public InviteRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<string> EnsureInviteCodeAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);
            parameters.Add("InviteCode", dbType: DbType.String, size: 20, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spInvite_EnsureCode", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<string>("InviteCode") ?? "";
        }

        public async Task<InviteStats> GetStatsAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<InviteStats>(
                "koch.spInvite_GetStats",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            ) ?? new InviteStats();
        }
    }

    public class StoreCategoryRepository : IStoreCategoryRepository
    {
        private readonly IDbConnectionFactory _factory;

        public StoreCategoryRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<StoreCategory>> GetAllAsync(bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<StoreCategory>(
                "koch.spStoreCategories_GetAll",
                new { ActiveOnly = activeOnly },
                commandType: CommandType.StoredProcedure);
            return list.ToList();
        }

        public async Task<int> CreateAsync(StoreCategory category)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Name", category.Name);
            p.Add("IsActive", category.IsActive);
            p.Add("SortOrder", category.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreCategories_Create", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(StoreCategory category)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", category.Id);
            p.Add("Name", category.Name);
            p.Add("IsActive", category.IsActive);
            p.Add("SortOrder", category.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreCategories_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<int> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreCategories_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result");
        }
    }
}
