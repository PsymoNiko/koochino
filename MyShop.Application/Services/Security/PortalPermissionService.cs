using MyShop.Application.Interfaces.Security;
using MyShop.Core.Models.Security;
using System.Threading;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Security
{
    /// <summary>دسترسی یکپارچه پورتال‌های Customer/Instructor/Student/Vendor روی موتور Permission.</summary>
    public class PortalPermissionService : IPortalPermissionService
    {
        private readonly IPermissionService _engine;

        public PortalPermissionService(IPermissionService engine) => _engine = engine;

        public async Task<bool> HasAsync(int userId, string permissionCode, CancellationToken ct = default)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionCode)) return false;
            if (!await _engine.IsEngineEnabledAsync()) return true;
            return await _engine.HasPermissionAsync(userId, permissionCode, ct: ct);
        }
    }

    public interface IPortalPermissionService
    {
        Task<bool> HasAsync(int userId, string permissionCode, CancellationToken ct = default);
    }
}
