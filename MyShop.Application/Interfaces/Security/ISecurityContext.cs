using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Security
{
    public interface ISecurityContext
    {
        int? UserId { get; }
        int? SiteId { get; }
        Task<int?> GetUserIdAsync(CancellationToken ct = default);
        Task<int?> GetSiteIdAsync(CancellationToken ct = default);
    }
}
