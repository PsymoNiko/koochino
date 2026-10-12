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
    public class AcademyEvaluationRepository : IAcademyEvaluationRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyEvaluationRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyEvaluationDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, bool publishedOnly = false)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyEvaluationDto>(
                "koch.spAcademyEvaluations_List",
                new { StudentId = studentId, InstructorId = instructorId, CourseId = courseId, PublishedOnly = publishedOnly },
                commandType: CommandType.StoredProcedure);
            return list?.Take(500).ToList() ?? new List<AcademyEvaluationDto>();
        }

        public async Task<AcademyEvaluationDto> GetByEnrollmentAsync(int enrollmentId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyEvaluationDto>(
                "koch.spAcademyEvaluations_GetByEnrollment",
                new { EnrollmentId = enrollmentId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertAsync(UpsertAcademyEvaluationDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("EnrollmentId", dto.EnrollmentId);
            p.Add("StudentId", dto.StudentId);
            p.Add("CourseRating", dto.CourseRating);
            p.Add("InstructorRating", dto.InstructorRating);
            p.Add("Comment", dto.Comment);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyEvaluations_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> SetPublishedAsync(int id, bool isPublished)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("IsPublished", isPublished);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyEvaluations_SetPublished", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<AcademyEvaluationCourseSummaryDto>> GetCourseSummaryAsync(int? instructorId = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyEvaluationCourseSummaryDto>(
                "koch.spAcademyEvaluations_CourseSummary",
                new { InstructorId = instructorId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyEvaluationCourseSummaryDto>();
        }

        public async Task<List<AcademyEvaluationInstructorSummaryDto>> GetInstructorSummaryAsync()
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyEvaluationInstructorSummaryDto>(
                "koch.spAcademyEvaluations_InstructorSummary", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyEvaluationInstructorSummaryDto>();
        }
    }
}
