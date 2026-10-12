using MyShop.Application.Interfaces.Crm;
using MyShop.Application.Services.Crm;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Crm;

namespace MyShop.Tests;

public class Phase1SiteIdHardeningTests
{
    [Fact]
    public async Task CrmCreate_StampsCurrentSite_AndPreservesCompatibilityScopes()
    {
        var repo = new FakeCrmRepository();
        var service = new CrmContactService(repo, new Tenant(12));

        var id = await service.CreateAsync(new CreateCrmContactDto
        {
            FullName = "Scoped contact",
            CompanyId = 7,
            StoreId = 9,
            IsActive = true
        }, 3);

        Assert.True(id > 0);
        Assert.Equal(12, repo.LastInserted!.SiteId);
        Assert.Equal(7, repo.LastInserted.CompanyId);
        Assert.Equal(9, repo.LastInserted.StoreId);
    }

    [Fact]
    public async Task CrmDirectId_Update_AndDelete_RejectCrossSiteRecord()
    {
        var repo = new FakeCrmRepository(new CrmContactDto
        {
            Id = 44,
            SiteId = 99,
            FullName = "Other site",
            IsActive = true
        });
        var service = new CrmContactService(repo, new Tenant(12));

        Assert.Null(await service.GetByIdAsync(44));
        Assert.False(await service.UpdateAsync(new UpdateCrmContactDto { Id = 44, FullName = "Changed" }));
        Assert.False(await service.DeleteAsync(44));
        Assert.False(repo.UpdateCalled);
        Assert.True(repo.DeleteCalled);
    }

    [Fact]
    public async Task UnresolvedTenant_FailsClosed()
    {
        var repo = new FakeCrmRepository();
        var service = new CrmContactService(repo, new Tenant(0));

        Assert.Empty(await service.GetAllAsync());
        Assert.Equal(0, await service.CreateAsync(new CreateCrmContactDto { FullName = "No site" }, null));
        Assert.False(await service.DeleteAsync(1));
    }

    [Fact]
    public void Migration_LeavesLegacyRowsNullable_AndScopesDirectIdWrites()
    {
        var sql = File.ReadAllText(ProjectFile("sql", "Phase1_Crm_Personnel_Tickets_SiteId.sql"));

        Assert.Contains("ADD SiteId INT NULL", sql);
        Assert.DoesNotContain("SiteId INT NOT NULL", sql);
        Assert.Contains("WHERE Id=@Id AND SiteId=@SiteId", sql);
        Assert.Contains("Legacy NULL rows were NOT backfilled", sql);
        Assert.Contains("@OrderId INT=NULL", sql);
    }

    private static string ProjectFile(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private sealed class Tenant : ICurrentTenant
    {
        public Tenant(int siteId) => SiteId = siteId;
        public string TenantKey => "test";
        public string ConnectionString => "";
        public int SiteId { get; }
        public string SiteCode => "test";
        public string SiteName => "Test";
        public bool IsResolved => SiteId > 0;
        public bool HasSite => SiteId > 0;
    }

    private sealed class FakeCrmRepository : ICrmContactRepository
    {
        private readonly List<CrmContactDto> _items;
        public FakeCrmRepository(params CrmContactDto[] items) => _items = items.ToList();
        public CrmContact? LastInserted { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool DeleteCalled { get; private set; }

        public Task<CrmContactDto> GetByIdAsync(int id, int siteId) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id && x.SiteId == siteId)!);

        public Task<CrmContactDto> GetByUserIdAsync(int siteId, int userId) =>
            Task.FromResult(_items.FirstOrDefault(x => x.SiteId == siteId && x.UserId == userId)!);

        public Task<CrmContactDto> GetByPhoneAsync(int siteId, string normalizedPhone) =>
            Task.FromResult(_items.FirstOrDefault(x => x.SiteId == siteId && x.Phone == normalizedPhone)!);

        public Task<List<CrmContactDto>> GetAllAsync(int siteId, int? storeId = null, int? companyId = null, bool? isActive = null) =>
            Task.FromResult(_items.Where(x => x.SiteId == siteId).ToList());

        public Task<bool> UserBelongsToSiteAsync(int userId, int siteId) => Task.FromResult(true);

        public Task<int> InsertAsync(CrmContact entity)
        {
            LastInserted = entity;
            return Task.FromResult(1);
        }

        public Task<bool> UpdateAsync(CrmContact entity)
        {
            UpdateCalled = true;
            return Task.FromResult(_items.Any(x => x.Id == entity.Id && x.SiteId == entity.SiteId));
        }

        public Task<bool> DeleteAsync(int id, int siteId)
        {
            DeleteCalled = true;
            return Task.FromResult(_items.Any(x => x.Id == id && x.SiteId == siteId));
        }
    }
}
