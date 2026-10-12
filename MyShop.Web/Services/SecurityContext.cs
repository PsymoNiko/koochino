using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyShop.Web.Services
{
    public class SecurityContext : ISecurityContext
    {
        private readonly IHttpContextAccessor _http;
        private readonly ICurrentTenant _tenant;
        private readonly CircuitSecuritySnapshot _snapshot;

        public SecurityContext(IHttpContextAccessor http, ICurrentTenant tenant, CircuitSecuritySnapshot snapshot)
        {
            _http = http;
            _tenant = tenant;
            _snapshot = snapshot;
        }

        // Inside an active Blazor Server circuit, HttpContext is null (see
        // SecurityContextCircuitHandler) — use the circuit-open snapshot once captured.
        // Falls back to live HttpContext for non-circuit callers (e.g. plain Razor Pages).
        public int? UserId => _snapshot.Captured ? _snapshot.UserId : ParseUserId(_http.HttpContext?.User);

        public int? SiteId => _snapshot.Captured ? _snapshot.SiteId : (_tenant?.HasSite == true ? _tenant.SiteId : null);

        public Task<int?> GetUserIdAsync(CancellationToken ct = default) => Task.FromResult(UserId);

        public Task<int?> GetSiteIdAsync(CancellationToken ct = default) => Task.FromResult(SiteId);

        static int? ParseUserId(ClaimsPrincipal user)
        {
            var idStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) && id > 0 ? id : null;
        }
    }
}
