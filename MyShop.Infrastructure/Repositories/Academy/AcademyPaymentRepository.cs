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
    public class AcademyPaymentRepository : IAcademyPaymentRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyPaymentRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyPaymentDto>> GetAllAsync(int? enrollmentId = null, int? studentId = null, string status = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyPaymentDto>(
                "koch.spAcademyPayments_List",
                new { EnrollmentId = enrollmentId, StudentId = studentId, Status = status },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyPaymentDto>();
        }

        public async Task<AcademyPaymentDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyPaymentDto>(
                "koch.spAcademyPayments_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<AcademyPaymentDto> GetByAuthorityAsync(string authority)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyPaymentDto>(
                "koch.spAcademyPayments_GetByAuthority",
                new { Authority = authority },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(int enrollmentId, decimal amount, string status, string source, string gateway, string authority, string gatewayRef)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("EnrollmentId", enrollmentId);
            p.Add("Amount", amount);
            p.Add("Status", status);
            p.Add("Source", source);
            p.Add("Gateway", gateway);
            p.Add("Authority", authority);
            p.Add("GatewayRef", gatewayRef);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyPayments_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> SetAuthorityAsync(int id, string authority, string gateway)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Authority", authority);
            p.Add("Gateway", gateway);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyPayments_SetAuthority", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<int> MarkPaidAsync(int id, string gatewayRef)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("GatewayRef", gatewayRef);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyPayments_MarkPaid", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result");
        }

        public async Task<bool> MarkFailedAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyPayments_MarkFailed", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
