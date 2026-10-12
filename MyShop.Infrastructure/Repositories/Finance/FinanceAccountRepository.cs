using Dapper;
using MyShop.Application.Interfaces.Finance;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Finance;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class FinanceAccountRepository : IFinanceAccountRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public FinanceAccountRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<FinanceAccountDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<FinanceAccountDto>(
                "koch.spFinanceAccounts_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<FinanceAccountDto>> GetAllAsync(int? storeId = null, int? companyId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<FinanceAccountDto>(
                "koch.spFinanceAccounts_List",
                new { StoreId = storeId, CompanyId = companyId },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<FinanceAccountDto>();
        }

        public async Task<int> InsertAsync(FinanceAccount entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("ParentId", entity.ParentId);
            p.Add("Code", entity.Code);
            p.Add("Name", entity.Name);
            p.Add("Type", entity.Type);
            p.Add("Balance", entity.Balance);
            p.Add("Currency", entity.Currency);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spFinanceAccounts_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(FinanceAccount entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("ParentId", entity.ParentId);
            p.Add("Code", entity.Code);
            p.Add("Name", entity.Name);
            p.Add("Type", entity.Type);
            p.Add("Balance", entity.Balance);
            p.Add("Currency", entity.Currency);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spFinanceAccounts_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spFinanceAccounts_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
