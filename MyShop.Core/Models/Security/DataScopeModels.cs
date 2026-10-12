using System.Collections.Generic;

namespace MyShop.Core.Models.Security
{
    /// <summary>پارامترهای فیلتر دامنه برای SP/SQL — الگوی مرجع فاز ۷.</summary>
    public sealed class DataScopeQuery
    {
        public string Kind { get; set; } = "All";
        public IReadOnlyList<int> Values { get; set; } = new List<int>();
        public int? ActorUserId { get; set; }
        public bool IsUnrestricted => string.Equals(Kind, "All", System.StringComparison.OrdinalIgnoreCase);

        public static DataScopeQuery Unrestricted() => new() { Kind = "All" };
    }

    public sealed class UserCompanyRow
    {
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
    }
}
