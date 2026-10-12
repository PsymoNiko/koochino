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
    public class AcademyCertificateRepository : IAcademyCertificateRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyCertificateRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyCertificateDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyCertificateDto>(
                "koch.spAcademyCertificates_List",
                new { StudentId = studentId, InstructorId = instructorId, CourseId = courseId, Status = status },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyCertificateDto>();
        }

        public async Task<AcademyCertificateDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyCertificateDto>(
                "koch.spAcademyCertificates_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<AcademyCertificateDto> GetByEnrollmentAsync(int enrollmentId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyCertificateDto>(
                "koch.spAcademyCertificates_GetByEnrollment",
                new { EnrollmentId = enrollmentId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> IssueAsync(int enrollmentId, int? issuedByUserId, int? issuedByInstructorId, string notes)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("EnrollmentId", enrollmentId);
            p.Add("IssuedByUserId", issuedByUserId);
            p.Add("IssuedByInstructorId", issuedByInstructorId);
            p.Add("Notes", notes);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCertificates_Issue", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> RevokeAsync(int id, string reason)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("RevokeReason", reason);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCertificates_Revoke", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
