using Dapper;
using MyShop.Application.Interfaces.Companies;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Companies;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CompanyRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

        public async Task<CompanyDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"SELECT c.Id, c.HoldingId, h.Name AS HoldingName, c.Code, c.Name, c.Address, c.Phone, c.Description, c.IsActive, c.SortOrder, c.CreatedAt, c.UpdatedAt
                FROM koch.Companies c LEFT JOIN koch.Holdings h ON c.HoldingId = h.Id WHERE c.Id = @Id";
            return await conn.QueryFirstOrDefaultAsync<CompanyDto>(sql, new { Id = id });
        }

        public async Task<List<CompanyDto>> GetAllAsync(int? holdingId = null, bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"SELECT c.Id, c.HoldingId, h.Name AS HoldingName, c.Code, c.Name, c.Address, c.Phone, c.Description, c.IsActive, c.SortOrder, c.CreatedAt, c.UpdatedAt
                FROM koch.Companies c LEFT JOIN koch.Holdings h ON c.HoldingId = h.Id
                WHERE (@HoldingId IS NULL OR c.HoldingId = @HoldingId) AND (@IsActive IS NULL OR c.IsActive = @IsActive)
                ORDER BY c.SortOrder, c.Name";
            var list = await conn.QueryAsync<CompanyDto>(sql, new { HoldingId = holdingId, IsActive = isActive });
            return list?.ToList() ?? new List<CompanyDto>();
        }

        public async Task<int> InsertAsync(Company entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"INSERT INTO koch.Companies (HoldingId, Code, Name, Address, Phone, Description, IsActive, SortOrder, CreatedAt)
                VALUES (@HoldingId, @Code, @Name, @Address, @Phone, @Description, @IsActive, @SortOrder, SYSDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await conn.ExecuteScalarAsync<int>(sql, entity);
        }

        public async Task<bool> UpdateAsync(Company entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"UPDATE koch.Companies SET HoldingId=@HoldingId, Code=@Code, Name=@Name, Address=@Address, Phone=@Phone, Description=@Description, IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME() WHERE Id=@Id";
            var rows = await conn.ExecuteAsync(sql, entity);
            return rows == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var rows = await conn.ExecuteAsync("DELETE FROM koch.Companies WHERE Id = @Id", new { Id = id });
            return rows == 1;
        }
    }
}
