using System;
using Microsoft.AspNetCore.Authentication;

namespace MyShop.Web.Services
{
    internal static class GoogleLoginState
    {
        public const string RoleKey = "login_role";
        public const string RememberKey = "login_remember";

        public static string NormalizeRole(string raw)
        {
            raw = (raw ?? "").Trim().ToLowerInvariant();
            if (raw == "store" || raw == "storeowner") return "store";
            return "customer";
        }

        public static string RoleFromItems(AuthenticationProperties props)
        {
            if (props?.Items != null &&
                props.Items.TryGetValue(RoleKey, out var r) &&
                !string.IsNullOrWhiteSpace(r))
                return NormalizeRole(r);
            return "customer";
        }

        public static bool RememberFromItems(AuthenticationProperties props)
        {
            if (props?.Items != null &&
                props.Items.TryGetValue(RememberKey, out var v) &&
                string.Equals(v, "1", StringComparison.Ordinal))
                return true;
            return false;
        }
    }
}
