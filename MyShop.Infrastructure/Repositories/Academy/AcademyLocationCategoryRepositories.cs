using Dapper;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Academy
{
    public class AcademyLocationRepository : IAcademyLocationRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyLocationRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyLocationDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyLocationDto>(
                "koch.spAcademyLocations_List",
                new { SiteId = siteId, IsActive = isActive },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyLocationDto>();
        }

        public async Task<AcademyLocationDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyLocationDto>(
                "koch.spAcademyLocations_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(CreateAcademyLocationDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SiteId", dto.SiteId);
            p.Add("Name", dto.Name);
            p.Add("Address", dto.Address);
            p.Add("Capacity", dto.Capacity);
            p.Add("Description", dto.Description);
            p.Add("Latitude", dto.Latitude);
            p.Add("Longitude", dto.Longitude);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyLocations_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(UpdateAcademyLocationDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("SiteId", dto.SiteId);
            p.Add("Name", dto.Name);
            p.Add("Address", dto.Address);
            p.Add("Capacity", dto.Capacity);
            p.Add("Description", dto.Description);
            p.Add("Latitude", dto.Latitude);
            p.Add("Longitude", dto.Longitude);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyLocations_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyLocations_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }

    public class AcademyActivityCategoryRepository : IAcademyActivityCategoryRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyActivityCategoryRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyActivityCategoryDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyActivityCategoryDto>(
                "koch.spAcademyActivityCategories_List",
                new { SiteId = siteId, IsActive = isActive },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyActivityCategoryDto>();
        }

        public async Task<AcademyActivityCategoryDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyActivityCategoryDto>(
                "koch.spAcademyActivityCategories_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(CreateAcademyActivityCategoryDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SiteId", dto.SiteId);
            p.Add("ParentId", dto.ParentId);
            p.Add("Name", dto.Name);
            p.Add("Description", dto.Description);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("IsActive", dto.IsActive);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivityCategories_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<int> UpdateAsync(UpdateAcademyActivityCategoryDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("SiteId", dto.SiteId);
            p.Add("ParentId", dto.ParentId);
            p.Add("Name", dto.Name);
            p.Add("Description", dto.Description);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("IsActive", dto.IsActive);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivityCategories_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result");
        }

        public async Task<int> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivityCategories_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result");
        }
    }
}
