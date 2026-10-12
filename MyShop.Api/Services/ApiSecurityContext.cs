using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyShop.Api.Services
{
    public class ApiSecurityContext : ISecurityContext
    {
        private readonly IHttpContextAccessor _http;
        private readonly ICurrentTenant _tenant;

        public ApiSecurityContext(IHttpContextAccessor http, ICurrentTenant tenant)
        {
            _http = http;
            _tenant = tenant;
        }

        public int? UserId => ParseUserId(_http.HttpContext?.User);
        public int? SiteId => _tenant?.HasSite == true ? _tenant.SiteId : null;

        public Task<int?> GetUserIdAsync(CancellationToken ct = default) => Task.FromResult(UserId);
        public Task<int?> GetSiteIdAsync(CancellationToken ct = default) => Task.FromResult(SiteId);

        static int? ParseUserId(ClaimsPrincipal user)
        {
            var idStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) && id > 0 ? id : null;
        }
    }
}
