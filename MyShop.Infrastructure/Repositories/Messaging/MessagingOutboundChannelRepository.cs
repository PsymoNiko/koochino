using Dapper;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Messaging
{
    public class MessagingOutboundChannelRepository : IMessagingOutboundChannelRepository
    {
        private readonly IDbConnectionFactory _factory;

        public MessagingOutboundChannelRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<MessagingOutboundChannel>> ListAsync(string provider = null, int? siteId = null, bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<MessagingOutboundChannel>(@"
                SELECT c.Id, c.SiteId, c.Provider, c.Title, c.Username, c.ChatId, c.IsActive, c.CreatedAt, c.UpdatedAt,
                       s.Code AS SiteCode, s.Name AS SiteName
                FROM koch.MessagingOutboundChannels c
                LEFT JOIN koch.Sites s ON s.Id = c.SiteId
                WHERE (@Provider IS NULL OR c.Provider = @Provider)
                  AND (@SiteId IS NULL OR c.SiteId IS NULL OR c.SiteId = @SiteId)
                  AND (@ActiveOnly = 0 OR c.IsActive = 1)
                ORDER BY c.Provider, c.Title",
                new
                {
                    Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim(),
                    SiteId = siteId,
                    ActiveOnly = activeOnly ? 1 : 0
                });
            return rows.ToList();
        }

        public async Task<MessagingOutboundChannel> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<MessagingOutboundChannel>(@"
                SELECT c.Id, c.SiteId, c.Provider, c.Title, c.Username, c.ChatId, c.IsActive, c.CreatedAt, c.UpdatedAt,
                       s.Code AS SiteCode, s.Name AS SiteName
                FROM koch.MessagingOutboundChannels c
                LEFT JOIN koch.Sites s ON s.Id = c.SiteId
                WHERE c.Id = @Id", new { Id = id });
        }

        public async Task<int> UpsertAsync(MessagingOutboundChannel channel)
        {
            if (channel == null) return 0;
            channel.Username = (channel.Username ?? "").Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(channel.ChatId) && !string.IsNullOrWhiteSpace(channel.Username))
            {
                var isEita = string.Equals(channel.Provider, MessagingProviders.Eita, StringComparison.OrdinalIgnoreCase);
                channel.ChatId = isEita || long.TryParse(channel.Username, out _)
                    ? channel.Username
                    : "@" + channel.Username;
            }

            using var conn = _factory.CreateConnection();
            if (channel.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.MessagingOutboundChannels
                    SET SiteId = @SiteId, Provider = @Provider, Title = @Title, Username = @Username,
                        ChatId = @ChatId, IsActive = @IsActive, UpdatedAt = SYSDATETIME()
                    WHERE Id = @Id", channel);
                return channel.Id;
            }

            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.MessagingOutboundChannels (SiteId, Provider, Title, Username, ChatId, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@SiteId, @Provider, @Title, @Username, @ChatId, @IsActive)", channel);
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.MessagingOutboundChannels WHERE Id = @Id", new { Id = id });
        }

        public async Task UpdateChatIdAsync(int id, string chatId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE koch.MessagingOutboundChannels
                SET ChatId = @ChatId, UpdatedAt = SYSDATETIME()
                WHERE Id = @Id", new { Id = id, ChatId = chatId });
        }
    }
}
