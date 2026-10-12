using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Messaging
{
    public interface ISiteBotRepository
    {
        Task<List<SiteBot>> ListAsync(int? siteId = null, string provider = null, bool activeOnly = false);
        Task<SiteBot> GetAsync(int siteId, string provider);
        Task<SiteBot> GetByIdAsync(int id);
        Task<int> UpsertAsync(SiteBot bot);
        Task SetActiveAsync(int id, bool isActive);
        Task DeleteAsync(int id);
    }

    /// <summary>Resolve بات بر اساس SiteId + Provider (با fallback به appsettings).</summary>
    public interface IBotCredentialsResolver
    {
        Task<BotCredentials> ResolveAsync(string provider, int? siteId = null);
        BotCredentials ResolveFromConfigFallback(string provider);
    }
}
