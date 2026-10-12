using Dapper;
using MyShop.Application.Interfaces.Contracts;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Contracts;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class ContractRepository : IContractRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ContractRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<ContractDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ContractDto>(
                "koch.spContracts_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<ContractDto>> GetAllAsync(int? storeId = null, int? companyId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ContractDto>(
                "koch.spContracts_List",
                new { StoreId = storeId, CompanyId = companyId },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<ContractDto>();
        }

        public async Task<int> InsertAsync(Contract entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("ContractNo", entity.ContractNo);
            p.Add("Title", entity.Title);
            p.Add("PartyName", entity.PartyName);
            p.Add("PartyContact", entity.PartyContact);
            p.Add("PartyPhone", entity.PartyPhone);
            p.Add("StartDate", entity.StartDate);
            p.Add("EndDate", entity.EndDate);
            p.Add("Value", entity.Value);
            p.Add("Currency", entity.Currency);
            p.Add("Status", entity.Status);
            p.Add("Type", entity.Type);
            p.Add("DocumentUrl", entity.DocumentUrl);
            p.Add("Notes", entity.Notes);
            p.Add("ReminderDays", entity.ReminderDays);
            p.Add("ParentContractId", entity.ParentContractId);
            p.Add("CreatedBy", entity.CreatedBy);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spContracts_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Contract entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("ContractNo", entity.ContractNo);
            p.Add("Title", entity.Title);
            p.Add("PartyName", entity.PartyName);
            p.Add("PartyContact", entity.PartyContact);
            p.Add("PartyPhone", entity.PartyPhone);
            p.Add("StartDate", entity.StartDate);
            p.Add("EndDate", entity.EndDate);
            p.Add("Value", entity.Value);
            p.Add("Currency", entity.Currency);
            p.Add("Status", entity.Status);
            p.Add("Type", entity.Type);
            p.Add("DocumentUrl", entity.DocumentUrl);
            p.Add("Notes", entity.Notes);
            p.Add("ReminderDays", entity.ReminderDays);
            p.Add("ParentContractId", entity.ParentContractId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spContracts_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spContracts_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
