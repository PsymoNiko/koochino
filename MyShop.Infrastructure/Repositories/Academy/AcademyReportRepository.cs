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
    public class AcademyReportRepository : IAcademyReportRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyReportRepository(IDbConnectionFactory db) => _db = db;

        public async Task<AcademyAdminSummaryDto> GetAdminSummaryAsync()
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyAdminSummaryDto>(
                "koch.spAcademyReports_AdminSummary", commandType: CommandType.StoredProcedure)
                ?? new AcademyAdminSummaryDto();
        }

        public async Task<AcademyAdminSummaryDto> GetAdminSummaryBySiteAsync(int siteId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyAdminSummaryDto>(
                "koch.spAcademyReports_AdminSummaryBySite",
                new { SiteId = siteId },
                commandType: CommandType.StoredProcedure)
                ?? new AcademyAdminSummaryDto();
        }

        public async Task<List<AcademyCourseReportDto>> GetCoursesAsync(int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyCourseReportDto>(
                "koch.spAcademyReports_Courses",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyCourseReportDto>();
        }

        public async Task<List<AcademyTuitionReportDto>> GetTuitionAsync(int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyTuitionReportDto>(
                "koch.spAcademyReports_Tuition",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyTuitionReportDto>();
        }

        public async Task<List<AcademyAttendanceReportDto>> GetAttendanceByCourseAsync(int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyAttendanceReportDto>(
                "koch.spAcademyReports_AttendanceByCourse",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyAttendanceReportDto>();
        }

        public async Task<List<AcademyActivityReportDto>> GetActivitiesAsync(int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyActivityReportDto>(
                "koch.spAcademyReports_Activities",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyActivityReportDto>();
        }

        public async Task<List<AcademyInstructorReportDto>> GetInstructorsAsync()
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyInstructorReportDto>(
                "koch.spAcademyReports_Instructors", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyInstructorReportDto>();
        }

        public async Task<AcademyInstructorSummaryDto> GetInstructorSummaryAsync(int instructorId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyInstructorSummaryDto>(
                "koch.spAcademyReports_InstructorSummary",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure)
                ?? new AcademyInstructorSummaryDto();
        }

        public async Task<AcademyStudentSummaryDto> GetStudentSummaryAsync(int studentId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyStudentSummaryDto>(
                "koch.spAcademyReports_StudentSummary",
                new { StudentId = studentId },
                commandType: CommandType.StoredProcedure)
                ?? new AcademyStudentSummaryDto();
        }

        public async Task<AcademyDailyReportSummaryDto> GetDailySummaryAsync(int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyDailyReportSummaryDto>(
                "koch.spAcademyReports_DailySummary",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure)
                ?? new AcademyDailyReportSummaryDto();
        }

        public async Task<AcademyDailyReportSummaryDto> GetDailySummaryBySiteAsync(
            int siteId, int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyDailyReportSummaryDto>(
                "koch.spAcademyReports_DailySummaryBySite",
                new { SiteId = siteId, InstructorId = instructorId },
                commandType: CommandType.StoredProcedure)
                ?? new AcademyDailyReportSummaryDto();
        }

        public async Task<List<AcademyDailyByDateReportDto>> GetDailyByDateAsync(
            DateTime fromDate, DateTime toDate, int? courseId = null, int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyDailyByDateReportDto>(
                "koch.spAcademyReports_DailyByDate",
                new
                {
                    FromDate = fromDate.Date,
                    ToDate = toDate.Date,
                    CourseId = courseId,
                    InstructorId = instructorId
                },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyDailyByDateReportDto>();
        }

        public async Task<List<AcademyDailyByCourseReportDto>> GetDailyByCourseAsync(int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyDailyByCourseReportDto>(
                "koch.spAcademyReports_DailyByCourse",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyDailyByCourseReportDto>();
        }
    }
}
