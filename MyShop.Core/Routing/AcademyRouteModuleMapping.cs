using System;

namespace MyShop.Core.Routing
{
    /// <summary>نگاشت مسیرهای پورتال دانش‌آموز (/academy) به کلید Feature — نه مسیرهای ادمین (/admin/academy).</summary>
    public static class AcademyRouteModuleMapping
    {
        public static string GetFeatureForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var p = path.Trim().ToLowerInvariant();
            var q = p.IndexOf('?');
            if (q >= 0) p = p.Substring(0, q);
            p = p.TrimEnd('/');
            if (!p.StartsWith("/academy") || p.StartsWith("/admin/academy")) return null;

            // Match only within the first path segment after the prefix -- see
            // CustomerRouteModuleMapping for why an unanchored Contains() on the full
            // remaining path is unsafe (nested/unrelated segments could false-match).
            var rest = p.Length > "/academy".Length ? p.Substring("/academy".Length) : "";
            var seg = "/" + rest.TrimStart('/').Split('/')[0];

            if (p == "/academy" || seg.Contains("/dashboard")) return "Dashboard";
            if (seg.Contains("/courses") || seg.Contains("/learn")) return "Courses";
            if (seg.Contains("/enrollments")) return "Enrollments";
            if (seg.Contains("/sessions") || seg.Contains("/calendar") || seg.Contains("/attendance") || seg.Contains("/attend"))
                return "Sessions";
            if (seg.Contains("/payments") || seg.Contains("/payment")) return "Payments";
            if (seg.Contains("/certificates") || seg.Contains("/certificate-pdf")) return "Certificates";
            if (seg.Contains("/evaluations")) return "Evaluations";
            if (seg.Contains("/missions") || seg.Contains("/points")) return "Points";
            if (seg.Contains("/requests") || seg.Contains("/daily")) return "Requests";
            if (seg.Contains("/reports")) return "Reports";
            if (seg.Contains("/tickets")) return "Tickets";
            if (seg.Contains("/profile")) return "Profile";
            return null;
        }
    }
}
