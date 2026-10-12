using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Modules
{
    public static class ModuleSources
    {
        public const string Core = "Core";
        public const string Licensed = "Licensed";
        public const string Trial = "Trial";
    }

    public class ModuleCatalogItem
    {
        public string ModuleKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; }
        public bool IsSellable { get; set; }
        public decimal? BasePrice { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string DependsOnCsv { get; set; }
        public bool IsBundle { get; set; }

        public IEnumerable<string> DependsOn => ModuleBundleDefs.ParseCsv(DependsOnCsv);
    }

    public class SiteModuleEntitlement
    {
        public int SiteId { get; set; }
        public string ModuleKey { get; set; } = "";
        public bool IsEnabled { get; set; }
        public string Source { get; set; } = ModuleSources.Core;
        public DateTime? ExpiresAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string DisplayName { get; set; }
        public bool IsSellable { get; set; }

        public bool IsEffectivelyEnabled =>
            IsEnabled && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);
    }
}
