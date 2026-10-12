using Dapper;
using MyShop.Application.Interfaces.ProjectManagement;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.ProjectManagement;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProjectRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<ProjectDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ProjectDto>(
                "koch.spProjects_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<ProjectDto>> GetAllAsync(int? storeId = null, int? companyId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ProjectDto>(
                "koch.spProjects_List",
                new { StoreId = storeId, CompanyId = companyId },
                commandType: CommandType.StoredProcedure
            );
            return list?.ToList() ?? new List<ProjectDto>();
        }

        public async Task<int> InsertAsync(Project entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", entity.StoreId);
            p.Add("Title", entity.Title);
            p.Add("Description", entity.Description);
            p.Add("Status", entity.Status);
            p.Add("Priority", entity.Priority);
            p.Add("StartDate", entity.StartDate);
            p.Add("EndDate", entity.EndDate);
            p.Add("Progress", entity.Progress);
            p.Add("CreatedBy", entity.CreatedBy);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProjects_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Project entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("StoreId", entity.StoreId);
            p.Add("Title", entity.Title);
            p.Add("Description", entity.Description);
            p.Add("Status", entity.Status);
            p.Add("Priority", entity.Priority);
            p.Add("StartDate", entity.StartDate);
            p.Add("EndDate", entity.EndDate);
            p.Add("Progress", entity.Progress);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProjects_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProjects_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
