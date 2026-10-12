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
    public class AcademyCourseScheduleRequestRepository : IAcademyCourseScheduleRequestRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyCourseScheduleRequestRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyCourseScheduleRequestDto>> ListAsync(
            int? studentId = null, int? scheduleId = null, int? courseId = null,
            string status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                using var conn = _db.CreateConnection();
                var list = await conn.QueryAsync<AcademyCourseScheduleRequestDto>(
                    "koch.spAcademyCourseScheduleRequests_List",
                    new
                    {
                        StudentId = studentId,
                        ScheduleId = scheduleId,
                        CourseId = courseId,
                        Status = status,
                        FromDate = fromDate?.Date,
                        ToDate = toDate?.Date
                    },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<AcademyCourseScheduleRequestDto>();
            }
            catch
            {
                return new List<AcademyCourseScheduleRequestDto>();
            }
        }

        public async Task<AcademyCourseScheduleRequestDto> GetByIdAsync(int id)
        {
            try
            {
                using var conn = _db.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<AcademyCourseScheduleRequestDto>(
                    "koch.spAcademyCourseScheduleRequests_GetById",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                return null;
            }
        }

        public async Task<int> InsertAsync(int scheduleId, int studentId, DateTime sessionDate, string studentNote, string status = null)
        {
            _ = status;
            try
            {
                using var conn = _db.CreateConnection();
                var p = new DynamicParameters();
                p.Add("ScheduleId", scheduleId);
                p.Add("StudentId", studentId);
                p.Add("SessionDate", sessionDate.Date, DbType.Date);
                p.Add("StudentNote", studentNote);
                p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("koch.spAcademyCourseScheduleRequests_Insert", p, commandType: CommandType.StoredProcedure);
                return p.Get<int>("Id");
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(
            int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (instructorId <= 0)
                return new List<AcademyCourseScheduleRequestDto>();
            try
            {
                using var conn = _db.CreateConnection();
                var list = await conn.QueryAsync<AcademyCourseScheduleRequestDto>(
                    "koch.spAcademyCourseScheduleRequests_ListByInstructor",
                    new
                    {
                        InstructorId = instructorId,
                        Status = status,
                        FromDate = fromDate?.Date,
                        ToDate = toDate?.Date
                    },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<AcademyCourseScheduleRequestDto>();
            }
            catch
            {
                return new List<AcademyCourseScheduleRequestDto>();
            }
        }

        public async Task<int> ReviewAsync(ReviewAcademyCourseScheduleRequestDto dto)
        {
            try
            {
                using var conn = _db.CreateConnection();
                var p = new DynamicParameters();
                p.Add("Id", dto.Id);
                p.Add("Status", dto.Status);
                p.Add("ReviewerNote", dto.ReviewerNote);
                p.Add("ReviewedByInstructorId", dto.ReviewedByInstructorId);
                p.Add("ReviewedByAdminUserId", dto.ReviewedByAdminUserId);
                p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("koch.spAcademyCourseScheduleRequests_Review", p, commandType: CommandType.StoredProcedure);
                return p.Get<int>("Result");
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId)
        {
            try
            {
                using var conn = _db.CreateConnection();
                var list = await conn.QueryAsync<AcademyCourseScheduleRequestAuditDto>(
                    "koch.spAcademyCourseScheduleRequestAudits_List",
                    new { RequestId = requestId },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<AcademyCourseScheduleRequestAuditDto>();
            }
            catch
            {
                return new List<AcademyCourseScheduleRequestAuditDto>();
            }
        }

        public async Task<int> CountApprovedAsync(int scheduleId, DateTime sessionDate)
        {
            if (scheduleId <= 0)
                return 0;
            try
            {
                using var conn = _db.CreateConnection();
                return await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1)
                      FROM koch.AcademyCourseScheduleRequests
                      WHERE ScheduleId = @ScheduleId AND SessionDate = @SessionDate AND Status = N'Approved'",
                    new { ScheduleId = scheduleId, SessionDate = sessionDate.Date });
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime fromDate, DateTime toDate)
        {
            if (scheduleId <= 0)
                return new List<AcademyDailyApprovedCountDto>();
            try
            {
                using var conn = _db.CreateConnection();
                var list = await conn.QueryAsync<AcademyDailyApprovedCountDto>(
                    @"SELECT ScheduleId, SessionDate, COUNT(1) AS ApprovedCount
                      FROM koch.AcademyCourseScheduleRequests
                      WHERE ScheduleId = @ScheduleId
                        AND Status = N'Approved'
                        AND SessionDate >= @FromDate AND SessionDate <= @ToDate
                      GROUP BY ScheduleId, SessionDate",
                    new { ScheduleId = scheduleId, FromDate = fromDate.Date, ToDate = toDate.Date });
                return list?.ToList() ?? new List<AcademyDailyApprovedCountDto>();
            }
            catch
            {
                return new List<AcademyDailyApprovedCountDto>();
            }
        }
    }
}
