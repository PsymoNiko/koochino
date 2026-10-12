using MyShop.Application.Interfaces.Contact;
using MyShop.Application.Interfaces.Crm;
using MyShop.Application.Services.Contact;
using MyShop.Application.Services.Crm;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Contact;
using MyShop.Core.Models.Crm;

namespace MyShop.Tests;

public class Phase2IdentityCrmTests
{
    [Fact]
    public async Task Upsert_PrioritizesUserId_MergesOnlyEmptyFields_AndUnionsTags()
    {
        var repo = new FakeCrmRepository(
            new CrmContactDto
            {
                Id = 10, SiteId = 4, UserId = 70, FullName = "نام معتبر",
                Phone = null!, Email = null!, Tags = "والد", IsActive = true
            });
        repo.AllowUser(70, 4);
        var service = new CrmContactService(repo, new Tenant(4));

        var id = await service.UpsertAsync(new CreateCrmContactDto
        {
            UserId = 70,
            FullName = "نام ورودی",
            Phone = "+98 912-123-4567",
            Email = "person@example.test",
            Tags = "والد، داوطلب",
            IsActive = false
        }, 2);

        Assert.Equal(10, id);
        Assert.Equal("نام معتبر", repo.Items.Single().FullName);
        Assert.Equal("09121234567", repo.Items.Single().Phone);
        Assert.Equal("person@example.test", repo.Items.Single().Email);
        Assert.Equal("والد, داوطلب", repo.Items.Single().Tags);
        Assert.True(repo.Items.Single().IsActive);
    }

    [Fact]
    public async Task Upsert_DeduplicatesNormalizedPhone_WithinSite()
    {
        var repo = new FakeCrmRepository(
            new CrmContactDto { Id = 11, SiteId = 4, FullName = "Existing", Phone = "09121234567", IsActive = true });
        var service = new CrmContactService(repo, new Tenant(4));

        var id = await service.UpsertAsync(new CreateCrmContactDto
        {
            FullName = "Duplicate",
            Phone = "۹۱۲۱۲۳۴۵۶۷",
            IsActive = true
        }, null);

        Assert.Equal(11, id);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task SamePhone_OnDifferentSites_CreatesSeparateContact_AndStampsTenant()
    {
        var repo = new FakeCrmRepository(
            new CrmContactDto { Id = 1, SiteId = 8, FullName = "Other", Phone = "09121234567", IsActive = true });
        var service = new CrmContactService(repo, new Tenant(4));

        var id = await service.CreateAsync(new CreateCrmContactDto
        {
            FullName = "Current",
            Phone = "+989121234567",
            CompanyId = 3,
            StoreId = 5,
            IsActive = true
        }, 9);

        Assert.NotEqual(1, id);
        var created = repo.Items.Single(x => x.Id == id);
        Assert.Equal(4, created.SiteId);
        Assert.Equal(3, created.CompanyId);
        Assert.Equal(5, created.StoreId);
    }

    [Fact]
    public async Task LinkUser_RejectsCrossSiteUser()
    {
        var repo = new FakeCrmRepository(
            new CrmContactDto { Id = 5, SiteId = 4, FullName = "Contact", IsActive = true });
        repo.AllowUser(90, 8);
        var service = new CrmContactService(repo, new Tenant(4));

        Assert.False(await service.LinkUserAsync(5, 90));
        Assert.Null(repo.Items.Single().UserId);
    }

    [Fact]
    public async Task LeadConversion_IsTenantScoped_Idempotent_AndMarksRead()
    {
        var crmRepo = new FakeCrmRepository();
        var leads = new FakeLeadRepository(crmRepo,
            new ContactLead { Id = 20, SiteId = 4, Phone = "09121234567", Email = "lead@example.test" },
            new ContactLead { Id = 21, SiteId = 8, Phone = "09121234567", Email = "other@example.test" });
        var service = new ContactLeadService(leads, new Tenant(4));

        var first = await service.ConvertAsync(20, 7);
        var second = await service.ConvertAsync(20, 7);
        var crossSite = await service.ConvertAsync(21, 7);

        Assert.NotNull(first);
        Assert.Equal(first.Id, second.Id);
        Assert.Null(crossSite);
        Assert.True(leads.Items.Single(x => x.Id == 20).IsRead);
        Assert.Equal(first.Id, leads.Items.Single(x => x.Id == 20).ConvertedContactId);
        Assert.Single(crmRepo.Items);
    }

    [Fact]
    public async Task LeadSubmit_ResolvedTenant_OverridesCallerSite()
    {
        var leads = new FakeLeadRepository(new FakeCrmRepository());
        var service = new ContactLeadService(leads, new Tenant(4));

        var result = await service.SubmitAsync(8, "09121234567", "lead@example.test");

        Assert.True(result.ok);
        Assert.Equal(4, leads.Inserted!.SiteId);
    }

    [Fact]
    public async Task LeadSubmit_AllowsEmptyEmail()
    {
        var leads = new FakeLeadRepository(new FakeCrmRepository());
        var service = new ContactLeadService(leads, new Tenant(4));

        var result = await service.SubmitAsync(4, "09121234567", "  ");

        Assert.True(result.ok);
        Assert.Equal("", leads.Inserted!.Email);
    }

    [Fact]
    public void Migration_IsFilteredBySiteUser_AndDoesNotCreateUniquePhoneOrBackfill()
    {
        var sql = File.ReadAllText(ProjectFile("sql", "Phase2_Identity_Crm.sql"));
        Assert.Contains("UX_CrmContacts_SiteId_UserId", sql);
        Assert.Contains("WHERE SiteId IS NOT NULL AND UserId IS NOT NULL", sql);
        Assert.Contains("Deliberately non-unique", sql);
        Assert.DoesNotContain("UNIQUE INDEX UX_CrmContacts_SiteId_Phone", sql);
        Assert.Contains("no row was changed", sql);
        Assert.Contains("No SiteId/UserId backfill was performed", sql);
    }

    [Theory]
    [InlineData("+98 912-123-4567", "09121234567")]
    [InlineData("۹۱۲۱۲۳۴۵۶۷", "09121234567")]
    [InlineData("٠٩١٢١٢٣٤٥٦٧", "09121234567")]
    public void PhoneNormalizer_ReusesLeadRules(string input, string expected) =>
        Assert.Equal(expected, PhoneNormalizer.Normalize(input));

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
        private readonly HashSet<(int userId, int siteId)> _users = new();
        public List<CrmContactDto> Items { get; } = new();

        public FakeCrmRepository(params CrmContactDto[] contacts) => Items.AddRange(contacts);
        public void AllowUser(int userId, int siteId) => _users.Add((userId, siteId));

        public Task<CrmContactDto> GetByIdAsync(int id, int siteId) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id && x.SiteId == siteId)!);
        public Task<CrmContactDto> GetByUserIdAsync(int siteId, int userId) =>
            Task.FromResult(Items.FirstOrDefault(x => x.SiteId == siteId && x.UserId == userId)!);
        public Task<CrmContactDto> GetByPhoneAsync(int siteId, string normalizedPhone) =>
            Task.FromResult(Items.FirstOrDefault(x => x.SiteId == siteId && x.Phone == normalizedPhone)!);
        public Task<List<CrmContactDto>> GetAllAsync(int siteId, int? storeId = null, int? companyId = null, bool? isActive = null) =>
            Task.FromResult(Items.Where(x => x.SiteId == siteId).ToList());
        public Task<bool> UserBelongsToSiteAsync(int userId, int siteId) =>
            Task.FromResult(_users.Contains((userId, siteId)));

        public Task<int> InsertAsync(CrmContact entity)
        {
            var id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
            Items.Add(ToDto(entity, id));
            return Task.FromResult(id);
        }

        public Task<bool> UpdateAsync(CrmContact entity)
        {
            var index = Items.FindIndex(x => x.Id == entity.Id && x.SiteId == entity.SiteId);
            if (index < 0) return Task.FromResult(false);
            Items[index] = ToDto(entity, entity.Id);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id, int siteId) =>
            Task.FromResult(Items.RemoveAll(x => x.Id == id && x.SiteId == siteId) == 1);

        private static CrmContactDto ToDto(CrmContact x, int id) => new()
        {
            Id = id, SiteId = x.SiteId, UserId = x.UserId, CompanyId = x.CompanyId, StoreId = x.StoreId,
            FullName = x.FullName, Phone = x.Phone, Phone2 = x.Phone2, Email = x.Email, Company = x.Company,
            JobTitle = x.JobTitle, Address = x.Address, City = x.City, Province = x.Province,
            PostalCode = x.PostalCode, Source = x.Source, Tags = x.Tags, Notes = x.Notes,
            AssignedTo = x.AssignedTo, Status = x.Status, Rating = x.Rating, IsActive = x.IsActive,
            CreatedBy = x.CreatedBy, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
            LastActivityAt = x.LastActivityAt
        };
    }

    private sealed class FakeLeadRepository : IContactLeadRepository
    {
        private readonly FakeCrmRepository _crm;
        public List<ContactLead> Items { get; } = new();
        public ContactLead? Inserted { get; private set; }

        public FakeLeadRepository(FakeCrmRepository crm, params ContactLead[] leads)
        {
            _crm = crm;
            Items.AddRange(leads);
        }

        public Task<int> InsertAsync(ContactLead row)
        {
            Inserted = row;
            return Task.FromResult(1);
        }
        public Task<bool> ExistsRecentAsync(int? siteId, string phone, string email, int hours = 24) => Task.FromResult(false);
        public Task<ContactLead> GetByIdAsync(int id, int siteId) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id && x.SiteId == siteId)!);
        public Task<List<ContactLead>> ListAsync(int? siteId, int top = 300) =>
            Task.FromResult(Items.Where(x => x.SiteId == siteId).Take(top).ToList());
        public Task SetReadAsync(int id, int siteId, bool isRead)
        {
            var item = Items.FirstOrDefault(x => x.Id == id && x.SiteId == siteId);
            if (item != null) item.IsRead = isRead;
            return Task.CompletedTask;
        }
        public Task<bool> DeleteAsync(int id, int siteId) =>
            Task.FromResult(Items.RemoveAll(x => x.Id == id && x.SiteId == siteId) == 1);

        public async Task<CrmContactDto> ConvertAsync(int id, int siteId, int? createdBy)
        {
            var lead = Items.FirstOrDefault(x => x.Id == id && x.SiteId == siteId);
            if (lead == null) return null!;
            if (lead.ConvertedContactId.HasValue)
                return await _crm.GetByIdAsync(lead.ConvertedContactId.Value, siteId);

            var contact = await _crm.GetByPhoneAsync(siteId, lead.Phone);
            if (contact == null)
            {
                var contactId = await _crm.InsertAsync(new CrmContact
                {
                    SiteId = siteId, FullName = lead.Phone, Phone = lead.Phone, Email = lead.Email,
                    Tags = "Lead", Status = "Lead", IsActive = true, CreatedBy = createdBy
                });
                contact = await _crm.GetByIdAsync(contactId, siteId);
            }
            lead.ConvertedContactId = contact.Id;
            lead.ConvertedAt = DateTime.UtcNow;
            lead.IsRead = true;
            return contact;
        }
    }
}
