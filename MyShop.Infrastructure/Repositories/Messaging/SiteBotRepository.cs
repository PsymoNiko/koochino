using Dapper;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Messaging
{
    public class SiteBotRepository : ISiteBotRepository
    {
        private readonly IDbConnectionFactory _factory;

        public SiteBotRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<SiteBot>> ListAsync(int? siteId = null, string provider = null, bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"
                SELECT b.*, s.Code AS SiteCode, s.Name AS SiteName
                FROM koch.SiteBots b
                INNER JOIN koch.Sites s ON s.Id = b.SiteId
                WHERE (@SiteId IS NULL OR b.SiteId = @SiteId)
                  AND (@Provider IS NULL OR b.Provider = @Provider)
                  AND (@ActiveOnly = 0 OR b.IsActive = 1)
                ORDER BY b.SiteId, b.Provider";
            try
            {
                var list = await conn.QueryAsync<SiteBot>(sql, new
                {
                    SiteId = siteId,
                    Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim(),
                    ActiveOnly = activeOnly ? 1 : 0
                });
                return list?.ToList() ?? new List<SiteBot>();
            }
            catch
            {
                return new List<SiteBot>();
            }
        }

        public async Task<SiteBot> GetAsync(int siteId, string provider)
        {
            using var conn = _factory.CreateConnection();
            try
            {
                return await conn.QueryFirstOrDefaultAsync<SiteBot>(@"
                    SELECT b.*, s.Code AS SiteCode, s.Name AS SiteName
                    FROM koch.SiteBots b
                    INNER JOIN koch.Sites s ON s.Id = b.SiteId
                    WHERE b.SiteId = @SiteId AND b.Provider = @Provider",
                    new { SiteId = siteId, Provider = provider });
            }
            catch
            {
                return null;
            }
        }

        public async Task<SiteBot> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            try
            {
                return await conn.QueryFirstOrDefaultAsync<SiteBot>(@"
                    SELECT b.*, s.Code AS SiteCode, s.Name AS SiteName
                    FROM koch.SiteBots b
                    INNER JOIN koch.Sites s ON s.Id = b.SiteId
                    WHERE b.Id = @Id", new { Id = id });
            }
            catch { return null; }
        }

        public async Task<int> UpsertAsync(SiteBot bot)
        {
            using var conn = _factory.CreateConnection();
            if (bot.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.SiteBots SET
                        SiteId = @SiteId, Provider = @Provider, DisplayName = @DisplayName,
                        BotToken = @BotToken, ApiBaseUrl = @ApiBaseUrl, ChatId = @ChatId,
                        BotLink = @BotLink, WebhookSecret = @WebhookSecret, IsActive = @IsActive,
                        UpdatedAt = SYSDATETIME()
                    WHERE Id = @Id", bot);
                return bot.Id;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.SiteBots
                    (SiteId, Provider, DisplayName, BotToken, ApiBaseUrl, ChatId, BotLink, WebhookSecret, IsActive)
                OUTPUT INSERTED.Id
                VALUES
                    (@SiteId, @Provider, @DisplayName, @BotToken, @ApiBaseUrl, @ChatId, @BotLink, @WebhookSecret, @IsActive)",
                bot);
        }

        public async Task SetActiveAsync(int id, bool isActive)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.SiteBots SET IsActive = @IsActive, UpdatedAt = SYSDATETIME() WHERE Id = @Id",
                new { Id = id, IsActive = isActive });
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SiteBots WHERE Id = @Id", new { Id = id });
        }
    }
}
