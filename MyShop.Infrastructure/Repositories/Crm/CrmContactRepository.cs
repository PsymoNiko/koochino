using Dapper;
using MyShop.Application.Interfaces.Crm;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Crm;
using MyShop.Core.Models.Security;
using MyShop.Core.Security;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class CrmContactRepository : ICrmContactRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CrmContactRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

        public async Task<CrmContactDto> GetByIdAsync(int id, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CrmContactDto>("koch.spCrmContacts_GetById", new { Id = id, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<CrmContactDto> GetByUserIdAsync(int siteId, int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CrmContactDto>(
                "koch.spCrmContacts_GetByUserId",
                new { SiteId = siteId, UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<CrmContactDto> GetByPhoneAsync(int siteId, string normalizedPhone)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CrmContactDto>(
                "koch.spCrmContacts_GetByPhone",
                new { SiteId = siteId, Phone = normalizedPhone },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<CrmContactDto>> GetAllAsync(int siteId, int? storeId = null, int? companyId = null, bool? isActive = null)
        {
            return await GetAllScopedAsync(DataScopeQuery.Unrestricted(), siteId, storeId, companyId, isActive);
        }

        public async Task<List<CrmContactDto>> GetAllScopedAsync(DataScopeQuery scope, int siteId, int? storeId = null, int? companyId = null, bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT TOP 500 c.*
                FROM koch.CrmContacts c
                LEFT JOIN koch.Stores s ON s.StoreID = c.StoreId
                WHERE c.SiteId = @SiteId
                  AND (@StoreId IS NULL OR c.StoreId = @StoreId)
                  AND (@CompanyId IS NULL OR c.CompanyId = @CompanyId)
                  AND (@IsActive IS NULL OR c.IsActive = @IsActive)
                  AND (c.StoreId IS NULL OR {DataScopeSqlBuilder.StoreScopePredicate("s")})
                ORDER BY c.Id DESC";
            var list = await conn.QueryAsync<CrmContactDto>(sql, new
            {
                SiteId = siteId,
                StoreId = storeId,
                CompanyId = companyId,
                IsActive = isActive,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues,
                ActorUserId = scope?.ActorUserId
            });
            return list?.ToList() ?? new List<CrmContactDto>();
        }

        public async Task<bool> UserBelongsToSiteAsync(int userId, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.Users WHERE Id=@UserId AND SiteId=@SiteId",
                new { UserId = userId, SiteId = siteId }) == 1;
        }

        public async Task<int> InsertAsync(CrmContact entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SiteId", entity.SiteId); p.Add("UserId", entity.UserId); p.Add("StoreId", entity.StoreId); p.Add("CompanyId", entity.CompanyId); p.Add("FullName", entity.FullName); p.Add("Phone", entity.Phone); p.Add("Phone2", entity.Phone2);
            p.Add("Email", entity.Email); p.Add("Company", entity.Company); p.Add("JobTitle", entity.JobTitle); p.Add("Address", entity.Address);
            p.Add("City", entity.City); p.Add("Province", entity.Province); p.Add("PostalCode", entity.PostalCode); p.Add("Source", entity.Source);
            p.Add("Tags", entity.Tags); p.Add("Notes", entity.Notes); p.Add("AssignedTo", entity.AssignedTo); p.Add("Status", entity.Status);
            p.Add("Rating", entity.Rating); p.Add("IsActive", entity.IsActive); p.Add("CreatedBy", entity.CreatedBy);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spCrmContacts_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(CrmContact entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id); p.Add("SiteId", entity.SiteId); p.Add("UserId", entity.UserId); p.Add("StoreId", entity.StoreId); p.Add("CompanyId", entity.CompanyId); p.Add("FullName", entity.FullName); p.Add("Phone", entity.Phone); p.Add("Phone2", entity.Phone2);
            p.Add("Email", entity.Email); p.Add("Company", entity.Company); p.Add("JobTitle", entity.JobTitle); p.Add("Address", entity.Address);
            p.Add("City", entity.City); p.Add("Province", entity.Province); p.Add("PostalCode", entity.PostalCode); p.Add("Source", entity.Source);
            p.Add("Tags", entity.Tags); p.Add("Notes", entity.Notes); p.Add("AssignedTo", entity.AssignedTo); p.Add("Status", entity.Status);
            p.Add("Rating", entity.Rating); p.Add("IsActive", entity.IsActive);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spCrmContacts_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters(); p.Add("Id", id); p.Add("SiteId", siteId); p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spCrmContacts_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
