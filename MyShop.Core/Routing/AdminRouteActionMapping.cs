using MyShop.Core.Models.AdminAccess;
using System;
using System.Linq;

namespace MyShop.Core.Routing
{
    /// <summary>Maps admin URL paths to CRUD actions for route-level permission checks.</summary>
    public static class AdminRouteActionMapping
    {
        public static string GetActionForPath(string path, string httpMethod = "GET")
        {
            var m = (httpMethod ?? "GET").ToUpperInvariant();
            if (m == "DELETE")
                return AdminPermissionActions.Delete;

            if (string.IsNullOrWhiteSpace(path))
                return AdminPermissionActions.Read;

            var p = path.Trim().ToLowerInvariant();
            var q = p.IndexOf('?');
            if (q >= 0)
                p = p.Substring(0, q);
            p = p.TrimEnd('/');

            if (!p.StartsWith("/admin"))
                return AdminPermissionActions.Read;

            var segments = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length <= 1)
                return AdminPermissionActions.Read;

            foreach (var seg in segments.Skip(1))
            {
                if (seg == "create" || seg == "create-admin")
                    return AdminPermissionActions.Create;
                if (seg == "edit")
                    return AdminPermissionActions.Update;
            }

            return AdminPermissionActions.Read;
        }
    }
}
