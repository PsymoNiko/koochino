using Dapper;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Academy
{
    public class AcademyAttendanceRepository : IAcademyAttendanceRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyAttendanceRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyAttendanceRosterItemDto>> GetCourseSessionRosterAsync(int courseSessionId)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyAttendanceRosterItemDto>(
                "koch.spAcademyAttendance_RosterCourseSession",
                new { CourseSessionId = courseSessionId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyAttendanceRosterItemDto>();
        }

        public async Task<List<AcademyAttendanceRosterItemDto>> GetActivitySessionRosterAsync(int activitySessionId)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyAttendanceRosterItemDto>(
                "koch.spAcademyAttendance_RosterActivitySession",
                new { ActivitySessionId = activitySessionId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyAttendanceRosterItemDto>();
        }

        public async Task<List<AcademyAttendanceRosterItemDto>> GetDailyRosterAsync(int scheduleId, DateTime sessionDate)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyAttendanceRosterItemDto>(
                "koch.spAcademyAttendance_RosterDaily",
                new { ScheduleId = scheduleId, SessionDate = sessionDate.Date },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyAttendanceRosterItemDto>();
        }

        public async Task<List<AcademyAttendanceDto>> GetAllAsync(int? courseSessionId = null, int? activitySessionId = null, int? studentId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyAttendanceDto>(
                "koch.spAcademyAttendance_List",
                new { CourseSessionId = courseSessionId, ActivitySessionId = activitySessionId, StudentId = studentId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyAttendanceDto>();
        }

        public async Task<int> UpsertAsync(UpsertAcademyAttendanceDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CourseSessionId", dto.CourseSessionId);
            p.Add("ActivitySessionId", dto.ActivitySessionId);
            p.Add("StudentId", dto.StudentId);
            p.Add("Status", dto.Status);
            p.Add("Source", dto.Source ?? AcademyConstants.AttendanceSource.Manual);
            p.Add("Notes", dto.Notes);
            p.Add("MarkedByInstructorId", dto.MarkedByInstructorId);
            p.Add("MarkedByAdminUserId", dto.MarkedByAdminUserId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyAttendance_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<int> UpsertDailyAsync(UpsertAcademyAttendanceDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("ScheduleId", dto.ScheduleId);
            p.Add("SessionDate", dto.SessionDate?.Date, DbType.Date);
            p.Add("StudentId", dto.StudentId);
            p.Add("Status", dto.Status);
            p.Add("Source", dto.Source ?? AcademyConstants.AttendanceSource.Manual);
            p.Add("Notes", dto.Notes);
            p.Add("MarkedByInstructorId", dto.MarkedByInstructorId);
            p.Add("MarkedByAdminUserId", dto.MarkedByAdminUserId);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyAttendance_UpsertDaily", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyAttendance_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<int> CreateQrTokenAsync(string sessionType, int sessionId, string token, DateTime expiresAt, int? instructorId, int? adminUserId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("SessionType", sessionType);
            p.Add("SessionId", sessionId);
            p.Add("Token", token);
            p.Add("ExpiresAt", expiresAt);
            p.Add("CreatedByInstructorId", instructorId);
            p.Add("CreatedByAdminUserId", adminUserId);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyAttendanceQr_Create", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<AcademyAttendanceQrTokenDto> GetQrByTokenAsync(string token)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyAttendanceQrTokenDto>(
                "koch.spAcademyAttendanceQr_GetByToken",
                new { Token = token },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<AcademyAttendanceQrTokenDto> GetActiveQrAsync(string sessionType, int sessionId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyAttendanceQrTokenDto>(
                "koch.spAcademyAttendanceQr_GetActive",
                new { SessionType = sessionType, SessionId = sessionId },
                commandType: CommandType.StoredProcedure);
        }
    }
}
