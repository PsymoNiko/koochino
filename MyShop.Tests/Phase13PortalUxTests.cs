using MyShop.Core.Routing;

namespace MyShop.Tests;

public class Phase13PortalUxTests
{
    [Theory]
    [InlineData("/academy/learn", "Academy")]
    [InlineData("/instructor/learn", "Academy")]
    [InlineData("/instructor/courses/1", "Academy")]
    public void PortalLearnRoutes_MapToAcademy(string path, string module)
        => Assert.Equal(module, PublicRouteModuleMapping.GetModuleForPath(path));

    [Fact]
    public void StudentLayout_WiresMosqueAndLearn_DistinctFromPointMissions()
    {
        var layout = File.ReadAllText(ProjectFile("MyShop.Web", "Pages", "AcademyPortal", "StudentLayout.razor"));
        var home = File.ReadAllText(ProjectFile("MyShop.Web", "Pages", "AcademyPortal", "Index.razor"));
        Assert.Contains("/academy/learn", layout);
        Assert.Contains("/mission", layout);
        Assert.Contains("/hall", layout);
        Assert.Contains("ماموریت امتیاز", layout);
        Assert.Contains("/academy/learn", home);
        Assert.Contains("ماموریت مسجد", home);
        Assert.DoesNotContain("/admin/sport", layout);
    }

    [Fact]
    public void InstructorLayout_HasLearnAndMosqueLinks()
    {
        var layout = File.ReadAllText(ProjectFile("MyShop.Web", "Pages", "Instructor", "InstructorLayout.razor"));
        var home = File.ReadAllText(ProjectFile("MyShop.Web", "Pages", "Instructor", "Index.razor"));
        Assert.Contains("/instructor/learn", layout);
        Assert.Contains("/mission", layout);
        Assert.Contains("/instructor/learn", home);
        Assert.Contains("/hall", home);
    }

    private static string ProjectFile(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
