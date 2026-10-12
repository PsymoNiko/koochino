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
    public class HoldingRepository : IHoldingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public HoldingRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

        public async Task<HoldingDto> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = "SELECT Id, Code, Name, Description, IsActive, SortOrder, CreatedAt, UpdatedAt FROM koch.Holdings WHERE Id = @Id";
            return await conn.QueryFirstOrDefaultAsync<HoldingDto>(sql, new { Id = id });
        }

        public async Task<List<HoldingDto>> GetAllAsync(bool? isActive = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT Id, Code, Name, Description, IsActive, SortOrder, CreatedAt, UpdatedAt FROM koch.Holdings WHERE (@IsActive IS NULL OR IsActive = @IsActive) ORDER BY SortOrder, Name";
            var list = await conn.QueryAsync<HoldingDto>(sql, new { IsActive = isActive });
            return list?.ToList() ?? new List<HoldingDto>();
        }

        public async Task<int> InsertAsync(Holding entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"INSERT INTO koch.Holdings (Code, Name, Description, IsActive, SortOrder, CreatedAt)
                VALUES (@Code, @Name, @Description, @IsActive, @SortOrder, SYSDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await conn.ExecuteScalarAsync<int>(sql, entity);
        }

        public async Task<bool> UpdateAsync(Holding entity)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"UPDATE koch.Holdings SET Code=@Code, Name=@Name, Description=@Description, IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME() WHERE Id=@Id";
            var rows = await conn.ExecuteAsync(sql, entity);
            return rows == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var rows = await conn.ExecuteAsync("DELETE FROM koch.Holdings WHERE Id = @Id", new { Id = id });
            return rows == 1;
        }
    }
}
