using Dapper;
using MyShop.Application.Interfaces.Reports;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Reports;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class ReportDefinitionRepository : IReportDefinitionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ReportDefinitionRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<ReportDefinitionDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ReportDefinitionDto>(
                "koch.spReportDefinitions_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<ReportDefinitionDto>> GetAllAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ReportDefinitionDto>(
                "koch.spReportDefinitions_List",
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<ReportDefinitionDto>();
        }

        public async Task<int> InsertAsync(ReportDefinition entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Code", entity.Code);
            p.Add("Name", entity.Name);
            p.Add("Description", entity.Description);
            p.Add("ModuleKey", entity.ModuleKey);
            p.Add("QueryOrConfig", entity.QueryOrConfig);
            p.Add("OutputType", entity.OutputType);
            p.Add("ParametersJson", entity.ParametersJson);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spReportDefinitions_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(ReportDefinition entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("Code", entity.Code);
            p.Add("Name", entity.Name);
            p.Add("Description", entity.Description);
            p.Add("ModuleKey", entity.ModuleKey);
            p.Add("QueryOrConfig", entity.QueryOrConfig);
            p.Add("OutputType", entity.OutputType);
            p.Add("ParametersJson", entity.ParametersJson);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spReportDefinitions_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spReportDefinitions_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
