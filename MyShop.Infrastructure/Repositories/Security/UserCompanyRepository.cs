using Dapper;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Security
{
    public class UserCompanyRepository : IUserCompanyRepository
    {
        private readonly IDbConnectionFactory _factory;
        public UserCompanyRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<UserCompanyRow>> GetByUserAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"
                SELECT uc.UserId, uc.CompanyId, ISNULL(c.Name, CAST(uc.CompanyId AS NVARCHAR(20))) AS CompanyName
                FROM Security.UserCompanies uc
                LEFT JOIN koch.Companies c ON c.Id = uc.CompanyId
                WHERE uc.UserId = @UserId";
            try
            {
                return (await conn.QueryAsync<UserCompanyRow>(sql, new { UserId = userId })).ToList();
            }
            catch
            {
                return new List<UserCompanyRow>();
            }
        }

        public async Task AddAsync(int userId, int companyId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM Security.UserCompanies WHERE UserId=@UserId AND CompanyId=@CompanyId)
                    INSERT INTO Security.UserCompanies (UserId, CompanyId) VALUES (@UserId, @CompanyId)",
                new { UserId = userId, CompanyId = companyId });
        }

        public async Task RemoveAsync(int userId, int companyId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM Security.UserCompanies WHERE UserId=@UserId AND CompanyId=@CompanyId",
                new { UserId = userId, CompanyId = companyId });
        }
    }
}
