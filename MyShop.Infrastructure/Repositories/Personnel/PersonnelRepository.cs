using Dapper;
using MyShop.Application.Interfaces.Personnel;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Personnel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class PersonnelRepository : IPersonnelRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PersonnelRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<PersonnelDto> GetByIdAsync(int id, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var item = await conn.QueryFirstOrDefaultAsync<PersonnelDto>(
                    "koch.spPersonnel_GetById",
                    new { Id = id, SiteId = siteId },
                    commandType: CommandType.StoredProcedure);
                return item;
            }
            catch (Exception ex) when (IsLegacyProcedureContract(ex))
            {
                var item = await conn.QueryFirstOrDefaultAsync<PersonnelDto>(
                    "koch.spPersonnel_GetById",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);
                if (item != null && item.SiteId.HasValue && item.SiteId.Value != siteId)
                    return null;
                return item;
            }
        }

        public async Task<List<PersonnelDto>> GetAllAsync(int siteId, int? storeId = null, int? companyId = null, bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<PersonnelDto>(
                    "koch.spPersonnel_List",
                    new { SiteId = siteId, StoreId = storeId, CompanyId = companyId, IsActive = isActive },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<PersonnelDto>();
            }
            catch (Exception ex) when (IsLegacyProcedureContract(ex))
            {
                var list = await conn.QueryAsync<PersonnelDto>(
                    "koch.spPersonnel_List",
                    new { StoreId = storeId, CompanyId = companyId, IsActive = isActive },
                    commandType: CommandType.StoredProcedure);
                return list?.Where(x => !x.SiteId.HasValue || x.SiteId == siteId).ToList()
                    ?? new List<PersonnelDto>();
            }
        }

        private static bool IsLegacyProcedureContract(Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message ?? "";
            return msg.IndexOf("SiteId", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("too many arguments", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("expects parameter", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async Task<int> InsertAsync(Personnel entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SiteId", entity.SiteId);
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("FullName", entity.FullName);
            p.Add("NationalCode", entity.NationalCode);
            p.Add("Phone", entity.Phone);
            p.Add("Email", entity.Email);
            p.Add("Position", entity.Position);
            p.Add("Department", entity.Department);
            p.Add("ResumeText", entity.ResumeText);
            p.Add("ResumeFileUrl", entity.ResumeFileUrl);
            p.Add("HireDate", entity.HireDate);
            p.Add("LeaveDate", entity.LeaveDate);
            p.Add("BirthDate", entity.BirthDate);
            p.Add("Address", entity.Address);
            p.Add("Specifications", entity.Specifications);
            p.Add("DefaultWorkStartTime", entity.DefaultWorkStartTime);
            p.Add("DefaultWorkEndTime", entity.DefaultWorkEndTime);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spPersonnel_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Personnel entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", entity.Id);
            p.Add("SiteId", entity.SiteId);
            p.Add("StoreId", entity.StoreId);
            p.Add("CompanyId", entity.CompanyId);
            p.Add("FullName", entity.FullName);
            p.Add("NationalCode", entity.NationalCode);
            p.Add("Phone", entity.Phone);
            p.Add("Email", entity.Email);
            p.Add("Position", entity.Position);
            p.Add("Department", entity.Department);
            p.Add("ResumeText", entity.ResumeText);
            p.Add("ResumeFileUrl", entity.ResumeFileUrl);
            p.Add("HireDate", entity.HireDate);
            p.Add("LeaveDate", entity.LeaveDate);
            p.Add("BirthDate", entity.BirthDate);
            p.Add("Address", entity.Address);
            p.Add("Specifications", entity.Specifications);
            p.Add("DefaultWorkStartTime", entity.DefaultWorkStartTime);
            p.Add("DefaultWorkEndTime", entity.DefaultWorkEndTime);
            p.Add("ImageUrl", entity.ImageUrl);
            p.Add("IsActive", entity.IsActive);
            p.Add("SortOrder", entity.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spPersonnel_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id, int siteId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("SiteId", siteId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spPersonnel_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
