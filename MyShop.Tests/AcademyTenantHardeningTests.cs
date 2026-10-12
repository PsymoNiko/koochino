using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Services.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using MyShop.Core.Routing;

namespace MyShop.Tests;

public class AcademyTenantHardeningTests
{
    [Fact]
    public async Task LocationService_ResolvedTenant_OverridesListAndCreateSite()
    {
        var repo = new LocationRepositoryFake();
        var service = new AcademyLocationService(repo, new TenantFake(17));

        await service.GetAllAsync(siteId: 99);
        var id = await service.CreateAsync(new CreateAcademyLocationDto { SiteId = 99, Name = "کلاس" });

        Assert.Equal(17, repo.ListSiteId);
        Assert.Equal(17, repo.Inserted.SiteId);
        Assert.Equal(1, id);
    }

    [Fact]
    public async Task LocationService_ResolvedTenant_BlocksForeignOwnershipAndRestampsUpdate()
    {
        var repo = new LocationRepositoryFake
        {
            Existing = new AcademyLocationDto { Id = 5, SiteId = 18, Name = "سایت دیگر" }
        };
        var service = new AcademyLocationService(repo, new TenantFake(17));

        Assert.Null(await service.GetByIdAsync(5));
        Assert.False(await service.UpdateAsync(new UpdateAcademyLocationDto { Id = 5, SiteId = 17, Name = "تغییر" }));
        Assert.False(await service.DeleteAsync(5));
        Assert.False(repo.UpdateCalled);
        Assert.False(repo.DeleteCalled);

        repo.Existing.SiteId = 17;
        Assert.True(await service.UpdateAsync(new UpdateAcademyLocationDto { Id = 5, SiteId = 99, Name = "مجاز" }));
        Assert.Equal(17, repo.Updated.SiteId);
    }

    [Fact]
    public async Task LocationService_WithoutTenant_PreservesLegacyCallerSite()
    {
        var repo = new LocationRepositoryFake();
        var service = new AcademyLocationService(repo);

        await service.GetAllAsync(siteId: 23);
        await service.CreateAsync(new CreateAcademyLocationDto { SiteId = 23, Name = "قدیمی" });

        Assert.Equal(23, repo.ListSiteId);
        Assert.Equal(23, repo.Inserted.SiteId);
    }

    [Theory]
    [InlineData("/admin/academy/courses/12/daily")]
    [InlineData("/admin/academy/courses/12/exceptions")]
    [InlineData("/admin/academy/courses/12/quiz")]
    [InlineData("/admin/academy/daily-requests")]
    [InlineData("/admin/academy/reports/daily")]
    public void AcademyAdminSubRoutes_MapToAcademy(string path)
        => Assert.Equal("Academy", AdminRouteModuleMapping.GetModuleForPath(path));

    [Theory]
    [InlineData("/academy/courses/12")]
    [InlineData("/academy/courses/12/quiz")]
    [InlineData("/academy/certificate-pdf/4")]
    [InlineData("/academy/payment/callback")]
    [InlineData("/instructor/daily")]
    public void AcademyPortalAndInstructorRoutes_MapToAcademy(string path)
        => Assert.Equal("Academy", PublicRouteModuleMapping.GetModuleForPath(path));

    private sealed class TenantFake : ICurrentTenant
    {
        public TenantFake(int siteId) => SiteId = siteId;
        public string TenantKey => "test";
        public string ConnectionString => "";
        public int SiteId { get; }
        public string SiteCode => "test";
        public string SiteName => "Test";
        public bool IsResolved => true;
        public bool HasSite => SiteId > 0;
    }

    private sealed class LocationRepositoryFake : IAcademyLocationRepository
    {
        public int? ListSiteId { get; private set; }
        public CreateAcademyLocationDto Inserted { get; private set; }
        public UpdateAcademyLocationDto Updated { get; private set; }
        public AcademyLocationDto Existing { get; set; }
        public bool UpdateCalled { get; private set; }
        public bool DeleteCalled { get; private set; }

        public Task<List<AcademyLocationDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
        {
            ListSiteId = siteId;
            return Task.FromResult(new List<AcademyLocationDto>());
        }

        public Task<AcademyLocationDto> GetByIdAsync(int id)
            => Task.FromResult(Existing);

        public Task<int> InsertAsync(CreateAcademyLocationDto dto)
        {
            Inserted = dto;
            return Task.FromResult(1);
        }

        public Task<bool> UpdateAsync(UpdateAcademyLocationDto dto)
        {
            UpdateCalled = true;
            Updated = dto;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id)
        {
            DeleteCalled = true;
            return Task.FromResult(true);
        }
    }
}
