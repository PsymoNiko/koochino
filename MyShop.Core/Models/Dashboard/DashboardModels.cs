using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Dashboard
{
    public class DashboardWidgetDef
    {
        public string WidgetKey { get; set; }
        public string Title { get; set; }
        public string ModuleKey { get; set; }
        public string PanelType { get; set; }
        public int DefaultOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UserDashboardWidget
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PanelType { get; set; }
        public string WidgetKey { get; set; }
        public string Title { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public string ConfigJson { get; set; }
    }

    public class HomeSection
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string SectionKey { get; set; }
        public string Title { get; set; }
        public string ContentJson { get; set; }
        public string LayoutType { get; set; } = "block";
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public DateTime UpdatedAt { get; set; }
    }
}
