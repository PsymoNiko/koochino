using Dapper;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Messaging
{
    public class SmsLineRepository : ISmsLineRepository
    {
        private readonly IDbConnectionFactory _factory;

        public SmsLineRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<SmsLine>> ListAsync(int? siteId = null)
        {
            using var conn = _factory.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<SmsLine>(
                    "koch.spSmsLines_List",
                    new { SiteId = siteId },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<SmsLine>();
            }
            catch
            {
                var list = await conn.QueryAsync<SmsLine>(@"
SELECT Id, SiteId, LineNumber, DisplayName, IsDefault, IsActive, SortOrder, Provider, Username, ApiKey, SandboxApiKey, CreatedAt, UpdatedAt
FROM koch.SmsLines
WHERE (@SiteId IS NULL OR SiteId IS NULL OR SiteId = @SiteId)
ORDER BY IsDefault DESC, SortOrder, Id", new { SiteId = siteId });
                return list?.ToList() ?? new List<SmsLine>();
            }
        }

        public async Task<SmsLine> GetByIdAsync(int id)
        {
            var all = await ListAsync(null);
            return all.FirstOrDefault(x => x.Id == id);
        }

        public async Task<SmsLine> GetDefaultAsync(int? siteId = null)
        {
            var all = await ListAsync(siteId);
            return all.FirstOrDefault(x => x.IsDefault && x.IsActive)
                ?? all.FirstOrDefault(x => x.IsActive);
        }

        public async Task<int> UpsertAsync(SmsLine line, bool setAsDefault = false)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", line.Id > 0 ? line.Id : (int?)null);
            p.Add("SiteId", line.SiteId);
            p.Add("LineNumber", line.LineNumber?.Trim());
            p.Add("DisplayName", line.DisplayName?.Trim());
            p.Add("IsDefault", setAsDefault || line.IsDefault);
            p.Add("IsActive", line.IsActive);
            p.Add("SortOrder", line.SortOrder);
            p.Add("Provider", string.IsNullOrWhiteSpace(line.Provider) ? MyShop.Core.Models.Messaging.SmsProviders.SmsIr : line.Provider.Trim());
            p.Add("Username", string.IsNullOrWhiteSpace(line.Username) ? null : line.Username.Trim());
            p.Add("ApiKey", string.IsNullOrWhiteSpace(line.ApiKey) ? null : line.ApiKey.Trim());
            p.Add("SandboxApiKey", string.IsNullOrWhiteSpace(line.SandboxApiKey) ? null : line.SandboxApiKey.Trim());
            p.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSmsLines_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("NewId");
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("koch.spSmsLines_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task SetDefaultAsync(int id, int? siteId = null)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("koch.spSmsLines_SetDefault", new { Id = id, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<SmsProviderSettings> GetProviderSettingsAsync(string provider = "SmsIr")
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<SmsProviderSettings>(
                "koch.spSmsProviderSettings_Get",
                new { Provider = provider },
                commandType: CommandType.StoredProcedure);
        }

        public async Task SetUseSandboxAsync(bool useSandbox, string provider = "SmsIr")
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spSmsProviderSettings_SetSandbox",
                new { Provider = provider, UseSandbox = useSandbox },
                commandType: CommandType.StoredProcedure);
        }
    }
}
