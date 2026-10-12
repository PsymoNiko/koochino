using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Authorization;
using MyShop.Application.Interfaces.Security;
using MyShop.Web.Services;

namespace MyShop.Web.Extensions
{
    public static class AuthorizationServiceExtensions
    {
        public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, AdminModulePermissionAuthorizationHandler>();
            services.AddScoped<ISecurityContext, SecurityContext>();

            // Circuit-lifetime snapshot of UserId/SiteId (design doc Section L.4) — see
            // CircuitSecuritySnapshot/SecurityContextCircuitHandler for why this is needed.
            services.AddScoped<CircuitSecuritySnapshot>();
            services.AddScoped<CircuitHandler, SecurityContextCircuitHandler>();

            return services;
        }
    }
}
