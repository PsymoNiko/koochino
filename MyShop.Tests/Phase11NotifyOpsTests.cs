using MyShop.Core.Academy;
using MyShop.Core.Models.Academy;
using MyShop.Core.Routing;

namespace MyShop.Tests;

public class Phase11NotifyOpsTests
{
    [Fact]
    public void NotificationRules_ReuseExistingEvents_WithoutNewEngine()
    {
        Assert.Contains(AcademyNotificationRules.All, r => r.EventCode == AcademyConstants.NotificationEvent.SessionReminder);
        Assert.Contains(AcademyNotificationRules.All, r => r.EventCode == AcademyConstants.NotificationEvent.PaymentDue);
        Assert.True(AcademyNotificationRules.All.Count >= 6);
    }

    [Fact]
    public void OpsReportRoute_StaysOnAcademyAdmin()
        => Assert.Equal("Academy", AdminRouteModuleMapping.GetModuleForPath("/admin/academy/reports/ops"));

    [Fact]
    public void Menu_HasOpsReport_NoCharityAdminPanel()
    {
        var menu = File.ReadAllText(ProjectFile("MyShop.Web", "Services", "AdminMenuConfig.cs"));
        Assert.Contains("/admin/academy/reports/ops", menu);
        Assert.DoesNotContain("/admin/charity", menu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin/library", menu, StringComparison.OrdinalIgnoreCase);
    }

    private static string ProjectFile(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
