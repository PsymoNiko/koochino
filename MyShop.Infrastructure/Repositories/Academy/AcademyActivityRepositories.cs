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
    public class AcademyActivityRepository : IAcademyActivityRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyActivityRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyActivityDto>> GetAllAsync(int? siteId = null, int? categoryId = null, bool? isActive = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyActivityDto>(
                "koch.spAcademyActivities_List",
                new { SiteId = siteId, CategoryId = categoryId, IsActive = isActive },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyActivityDto>();
        }

        public async Task<AcademyActivityDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyActivityDto>(
                "koch.spAcademyActivities_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(CreateAcademyActivityDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SiteId", dto.SiteId);
            p.Add("CategoryId", dto.CategoryId);
            p.Add("Title", dto.Title);
            p.Add("Description", dto.Description);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivities_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<int> UpdateAsync(UpdateAcademyActivityDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("SiteId", dto.SiteId);
            p.Add("CategoryId", dto.CategoryId);
            p.Add("Title", dto.Title);
            p.Add("Description", dto.Description);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivities_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivities_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }

    public class AcademyActivitySessionRepository : IAcademyActivitySessionRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyActivitySessionRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyActivitySessionDto>> GetAllAsync(int? activityId = null, int? instructorId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyActivitySessionDto>(
                "koch.spAcademyActivitySessions_List",
                new { ActivityId = activityId, InstructorId = instructorId, Status = status },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyActivitySessionDto>();
        }

        public async Task<AcademyActivitySessionDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyActivitySessionDto>(
                "koch.spAcademyActivitySessions_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(CreateAcademyActivitySessionDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("ActivityId", dto.ActivityId);
            p.Add("LocationId", dto.LocationId);
            p.Add("InstructorId", dto.InstructorId);
            p.Add("Title", dto.Title);
            p.Add("StartsAt", dto.StartsAt);
            p.Add("EndsAt", dto.EndsAt);
            p.Add("Capacity", dto.Capacity);
            p.Add("Status", dto.Status ?? AcademyConstants.SessionStatus.Scheduled);
            p.Add("Notes", dto.Notes);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivitySessions_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(UpdateAcademyActivitySessionDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("ActivityId", dto.ActivityId);
            p.Add("LocationId", dto.LocationId);
            p.Add("InstructorId", dto.InstructorId);
            p.Add("Title", dto.Title);
            p.Add("StartsAt", dto.StartsAt);
            p.Add("EndsAt", dto.EndsAt);
            p.Add("Capacity", dto.Capacity);
            p.Add("Status", dto.Status ?? AcademyConstants.SessionStatus.Scheduled);
            p.Add("Notes", dto.Notes);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivitySessions_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivitySessions_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }

    public class AcademyActivityRequestRepository : IAcademyActivityRequestRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyActivityRequestRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyActivityRequestDto>> GetAllAsync(int? sessionId = null, int? studentId = null, int? instructorId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyActivityRequestDto>(
                "koch.spAcademyActivityRequests_List",
                new { SessionId = sessionId, StudentId = studentId, InstructorId = instructorId, Status = status },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyActivityRequestDto>();
        }

        public async Task<AcademyActivityRequestDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyActivityRequestDto>(
                "koch.spAcademyActivityRequests_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(int sessionId, int studentId, string studentNote)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SessionId", sessionId);
            p.Add("StudentId", studentId);
            p.Add("StudentNote", studentNote);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivityRequests_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<int> ReviewAsync(ReviewAcademyActivityRequestDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("Status", dto.Status);
            p.Add("ReviewedByInstructorId", dto.ReviewedByInstructorId);
            p.Add("ReviewerNote", dto.ReviewerNote);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyActivityRequests_Review", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result");
        }
    }
}
