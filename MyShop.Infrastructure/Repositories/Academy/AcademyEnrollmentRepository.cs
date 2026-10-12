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
    public class AcademyEnrollmentRepository : IAcademyEnrollmentRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyEnrollmentRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyEnrollmentDto>> GetAllAsync(int? courseId = null, int? studentId = null, int? instructorId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyEnrollmentDto>(
                "koch.spAcademyEnrollments_List",
                new { CourseId = courseId, StudentId = studentId, InstructorId = instructorId, Status = status },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyEnrollmentDto>();
        }

        public async Task<AcademyEnrollmentDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyEnrollmentDto>(
                "koch.spAcademyEnrollments_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<AcademyEnrollmentDto> GetByCourseStudentAsync(int courseId, int studentId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyEnrollmentDto>(
                "koch.spAcademyEnrollments_GetByCourseStudent",
                new { CourseId = courseId, StudentId = studentId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> RegisterAsync(int courseId, int studentId, string studentNote)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CourseId", courseId);
            p.Add("StudentId", studentId);
            p.Add("StudentNote", studentNote);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyEnrollments_Register", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<int> ReviewAsync(ReviewAcademyEnrollmentDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("Status", dto.Status);
            p.Add("ReviewedByInstructorId", dto.ReviewedByInstructorId);
            p.Add("ReviewedByAdminUserId", dto.ReviewedByAdminUserId);
            p.Add("ReviewerNote", dto.ReviewerNote);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyEnrollments_Review", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result");
        }

        public async Task<bool> CancelByStudentAsync(int id, int studentId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("StudentId", studentId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyEnrollments_CancelByStudent", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
