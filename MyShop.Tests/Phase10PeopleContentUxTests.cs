using MyShop.Core.Models.Crm;
using MyShop.Core.Routing;

namespace MyShop.Tests;

public class Phase10PeopleContentUxTests
{
    [Fact]
    public void MosqueTags_SetAndHas_WorkWithoutNewEntity()
    {
        var tags = CrmMosqueTags.Set(null, CrmMosqueTags.Parent, true);
        tags = CrmMosqueTags.Set(tags, CrmMosqueTags.Volunteer, true);
        Assert.True(CrmMosqueTags.Has(tags, CrmMosqueTags.Parent));
        Assert.True(CrmMosqueTags.Has(tags, "داوطلب"));
        Assert.False(CrmMosqueTags.Has(tags, CrmMosqueTags.Donor));
        tags = CrmMosqueTags.Set(tags, CrmMosqueTags.Parent, false);
        Assert.False(CrmMosqueTags.Has(tags, CrmMosqueTags.Parent));
        Assert.Contains(CrmMosqueTags.Congregation, CrmMosqueTags.All);
        Assert.Contains(CrmMosqueTags.Donor, CrmMosqueTags.All);
    }

    [Theory]
    [InlineData("/mission", "Academy")]
    [InlineData("/hall", "Academy")]
    [InlineData("/education", "Academy")]
    public void PublicMissionAndHall_MapToAcademy(string path, string module)
        => Assert.Equal(module, PublicRouteModuleMapping.GetModuleForPath(path));

    [Fact]
    public void AdminMenu_AddsLessonsWithoutSeparateSportPanel()
    {
        var menu = File.ReadAllText(ProjectFile("MyShop.Web", "Services", "AdminMenuConfig.cs"));
        Assert.Contains("/admin/academy/lessons", menu);
        Assert.DoesNotContain("/admin/sport", menu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin/library", menu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin/charity", menu, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GuardianSql_IsAdditiveOnAcademyStudents()
    {
        var sql = File.ReadAllText(ProjectFile("sql", "Academy_Phase31_GuardianContact.sql"));
        Assert.Contains("GuardianContactId", sql);
        Assert.Contains("FK_AcademyStudents_GuardianContact", sql);
        Assert.DoesNotContain("CREATE TABLE koch.Household", sql);
        Assert.DoesNotContain("VolunteerUser", sql);
    }

    private static string ProjectFile(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
