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
    public class AcademyStudentRepository : IAcademyStudentRepository
    {
        private readonly IDbConnectionFactory _db;

        public AcademyStudentRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyStudentDto>> GetAllAsync(int? siteId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyStudentDto>(
                "koch.spAcademyStudents_List",
                new { SiteId = siteId, Status = status },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyStudentDto>();
        }

        public async Task<AcademyStudentDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyStudentDto>(
                "koch.spAcademyStudents_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<AcademyStudentDto> GetByUserIdAsync(int userId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyStudentDto>(
                "koch.spAcademyStudents_GetByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(int? siteId, int userId, string status, string notes, bool mustChangePassword)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SiteId", siteId);
            p.Add("UserId", userId);
            p.Add("Status", status ?? "Active");
            p.Add("Notes", notes);
            p.Add("MustChangePassword", mustChangePassword);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyStudents_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(UpdateAcademyStudentDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("SiteId", dto.SiteId);
            p.Add("Status", dto.Status ?? "Active");
            p.Add("Notes", dto.Notes);
            p.Add("MustChangePassword", dto.MustChangePassword);
            p.Add("GuardianContactId", dto.GuardianContactId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyStudents_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyStudents_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }

    public class AcademyInstructorRepository : IAcademyInstructorRepository
    {
        private readonly IDbConnectionFactory _db;

        public AcademyInstructorRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyInstructorDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyInstructorDto>(
                "koch.spAcademyInstructors_List",
                new { SiteId = siteId, IsActive = isActive },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyInstructorDto>();
        }

        public async Task<AcademyInstructorDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<AcademyInstructorDto>(
                "koch.spAcademyInstructors_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
            if (item != null)
                item.CategoryIds = await GetCategoryIdsAsync(id);
            return item;
        }

        public async Task<AcademyInstructorDto> GetByUserIdAsync(int userId)
        {
            using var conn = _db.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<AcademyInstructorDto>(
                "koch.spAcademyInstructors_GetByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
            if (item != null)
                item.CategoryIds = await GetCategoryIdsAsync(item.Id);
            return item;
        }

        public async Task<int> InsertAsync(int? siteId, int personnelId, int userId, string bio, bool isActive, bool mustChangePassword)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SiteId", siteId);
            p.Add("PersonnelId", personnelId);
            p.Add("UserId", userId);
            p.Add("Bio", bio);
            p.Add("IsActive", isActive);
            p.Add("MustChangePassword", mustChangePassword);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyInstructors_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(UpdateAcademyInstructorDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("SiteId", dto.SiteId);
            p.Add("Bio", dto.Bio);
            p.Add("IsActive", dto.IsActive);
            p.Add("MustChangePassword", dto.MustChangePassword);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyInstructors_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyInstructors_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<int>> GetCategoryIdsAsync(int instructorId)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<int>(
                "koch.spAcademyInstructorCategories_Get",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<int>();
        }

        public async Task SetCategoryIdsAsync(int instructorId, IEnumerable<int> categoryIds)
        {
            var csv = categoryIds == null
                ? null
                : string.Join(",", categoryIds.Where(x => x > 0).Distinct());
            using var conn = _db.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spAcademyInstructorCategories_Set",
                new { InstructorId = instructorId, CategoryIds = csv },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> GetRoleIdByNameAsync(string roleName)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("RoleName", roleName);
            p.Add("RoleId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademy_GetRoleIdByName", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("RoleId");
        }
    }
}
