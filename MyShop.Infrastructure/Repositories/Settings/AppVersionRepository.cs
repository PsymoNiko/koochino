using Dapper;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Settings
{
    public class AppVersionRepository
    {
        private readonly IDbConnectionFactory _factory;
        public AppVersionRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<PlatformReleaseNote>> ListAsync(int take = 30)
        {
            using var conn = _factory.CreateConnection();
            try
            {
                var rows = await conn.QueryAsync<PlatformReleaseNote>(@"
SELECT TOP (@Take) Id, Version, Title, Notes, ReleasedAt, CreatedAt, CreatedBy
FROM koch.PlatformReleaseNotes
ORDER BY ReleasedAt DESC, Id DESC", new { Take = take });
                return rows?.ToList() ?? new List<PlatformReleaseNote>();
            }
            catch { return new List<PlatformReleaseNote>(); }
        }

        public async Task<int> InsertAsync(PlatformReleaseNote note)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.PlatformReleaseNotes (Version, Title, Notes, ReleasedAt, CreatedBy)
VALUES (@Version, @Title, @Notes, @ReleasedAt, @CreatedBy);
SELECT CAST(SCOPE_IDENTITY() AS INT);", note);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.PlatformReleaseNotes WHERE Id=@Id", new { Id = id });
        }
    }
}
