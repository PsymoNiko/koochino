using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using MyShop.Core.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    /// <summary>
    /// A Blazor Server circuit outlives the HTTP request that started it, and
    /// IHttpContextAccessor.HttpContext is null once interactive component code runs
    /// (design doc Section L.4). This handler captures UserId/SiteId once, at circuit
    /// open — while HttpContext from the initial connection is still valid — into a
    /// circuit-scoped snapshot that SecurityContext reads from for the rest of the
    /// circuit's life.
    /// </summary>
    public class SecurityContextCircuitHandler : CircuitHandler
    {
        private readonly IHttpContextAccessor _http;
        private readonly ICurrentTenant _tenant;
        private readonly CircuitSecuritySnapshot _snapshot;

        public SecurityContextCircuitHandler(IHttpContextAccessor http, ICurrentTenant tenant, CircuitSecuritySnapshot snapshot)
        {
            _http = http;
            _tenant = tenant;
            _snapshot = snapshot;
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            var user = _http.HttpContext?.User;
            var idStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            _snapshot.UserId = int.TryParse(idStr, out var id) && id > 0 ? id : (int?)null;
            _snapshot.SiteId = _tenant?.HasSite == true ? _tenant.SiteId : (int?)null;
            _snapshot.Captured = true;
            return base.OnCircuitOpenedAsync(circuit, cancellationToken);
        }
    }
}
