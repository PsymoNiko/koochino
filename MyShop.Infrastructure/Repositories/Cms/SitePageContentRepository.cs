using Dapper;
using MyShop.Application.Interfaces.Cms;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Cms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Cms
{
    public class SitePageContentRepository : ISitePageContentRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SitePageContentRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<SitePageContent> GetAsync(int siteId, string pageKey)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SitePageContent>(@"
SELECT Id, SiteId, PageKey, Title, ContentJson, UpdatedAt, UpdatedBy
FROM koch.SitePageContents
WHERE SiteId=@SiteId AND PageKey=@PageKey", new { SiteId = siteId, PageKey = pageKey });
        }

        public async Task<List<SitePageContent>> ListBySiteAsync(int siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SitePageContent>(@"
SELECT Id, SiteId, PageKey, Title, ContentJson, UpdatedAt, UpdatedBy
FROM koch.SitePageContents WHERE SiteId=@SiteId ORDER BY PageKey", new { SiteId = siteId });
            return rows?.ToList() ?? new List<SitePageContent>();
        }

        public async Task UpsertAsync(SitePageContent row)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
MERGE koch.SitePageContents AS t
USING (SELECT @SiteId AS SiteId, @PageKey AS PageKey) AS s
ON t.SiteId=s.SiteId AND t.PageKey=s.PageKey
WHEN MATCHED THEN UPDATE SET
    Title=@Title, ContentJson=@ContentJson, UpdatedAt=SYSDATETIME(), UpdatedBy=@UpdatedBy
WHEN NOT MATCHED THEN INSERT (SiteId, PageKey, Title, ContentJson, UpdatedBy)
VALUES (@SiteId, @PageKey, @Title, @ContentJson, @UpdatedBy);", row);
        }

        public async Task DeleteAsync(int siteId, string pageKey)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.SitePageContents WHERE SiteId=@SiteId AND PageKey=@PageKey",
                new { SiteId = siteId, PageKey = pageKey });
        }
    }
}
