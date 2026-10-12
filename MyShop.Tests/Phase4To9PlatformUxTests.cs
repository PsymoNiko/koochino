using MyShop.Core.Academy;
using MyShop.Core.Models.Academy;
using MyShop.Core.Routing;

namespace MyShop.Tests;

public class Phase4To9PlatformUxTests
{
    [Theory]
    [InlineData("/education", "Academy")]
    [InlineData("/sport", "Academy")]
    [InlineData("/culture", "Academy")]
    [InlineData("/ceremony", "Academy")]
    [InlineData("/academy/courses", "Academy")]
    [InlineData("/support", "Support")]
    [InlineData("/help", "Support")]
    [InlineData("/services", "Contact")]
    [InlineData("/mission", "Academy")]
    [InlineData("/hall", "Academy")]
    [InlineData("/charity", "Contact")]
    [InlineData("/library", "Announcements")]
    public void PublicAliases_MapToExistingModules(string path, string module)
        => Assert.Equal(module, PublicRouteModuleMapping.GetModuleForPath(path));

    [Theory]
    [InlineData("/admin/academy/daily-requests", "Academy")]
    [InlineData("/admin/academy/reports/daily", "Academy")]
    [InlineData("/admin/academy/calendar", "Academy")]
    [InlineData("/admin/projects", "ProjectManagement")]
    [InlineData("/admin/crm", "CRM")]
    [InlineData("/admin/academy/lessons", "Academy")]
    [InlineData("/admin/tickets", "Tickets")]
    [InlineData("/admin/support/knowledge", "Support")]
    [InlineData("/admin/contact", "Contact")]
    public void CentralAdminRoutes_StayOnSharedPanel(string path, string module)
        => Assert.Equal(module, AdminRouteModuleMapping.GetModuleForPath(path));

    [Fact]
    public void AdminMenu_HasNoSeparateSportCultureCeremonyPanels()
    {
        var menu = File.ReadAllText(ProjectFile("MyShop.Web", "Services", "AdminMenuConfig.cs"));
        Assert.Contains("/admin/academy/daily-requests", menu);
        Assert.Contains("/admin/academy/reports/daily", menu);
        Assert.Contains("/admin/projects", menu);
        Assert.DoesNotContain("/admin/sport", menu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin/culture", menu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin/ceremony", menu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Sport\"", menu);
        Assert.DoesNotContain("\"Ceremony\"", menu);
        Assert.DoesNotContain("\"Library\"", menu);
    }

    [Fact]
    public void Taxonomy_MatchesCodeOrPersianName_AndIncludesChildren()
    {
        var cats = new[]
        {
            new AcademyActivityCategoryDto { Id = 1, Name = "ورزش", Code = AcademyConstants.Taxonomy.Sport },
            new AcademyActivityCategoryDto { Id = 2, ParentId = 1, Name = "فوتبال" },
            new AcademyActivityCategoryDto { Id = 3, Name = "فرهنگ" }
        };

        var sport = AcademyTaxonomy.CategoryIds(cats, AcademyConstants.Taxonomy.Sport);
        Assert.Contains(1, sport);
        Assert.Contains(2, sport);
        Assert.DoesNotContain(3, sport);
        Assert.True(AcademyTaxonomy.Matches(cats[2], AcademyConstants.Taxonomy.Culture));
    }

    private static string ProjectFile(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
