using Dapper;
using MyShop.Application.Interfaces.Contact;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Contact;
using MyShop.Core.Models.Crm;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Contact
{
    public class ContactLeadRepository : IContactLeadRepository
    {
        private readonly IDbConnectionFactory _factory;
        public ContactLeadRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<int> InsertAsync(ContactLead row)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.ContactLeads (SiteId, Phone, Email, Source, Notes)
VALUES (@SiteId, @Phone, @Email, @Source, @Notes);
SELECT CAST(SCOPE_IDENTITY() AS INT);", row);
        }

        public async Task<bool> ExistsRecentAsync(int? siteId, string phone, string email, int hours = 24)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM koch.ContactLeads
WHERE Phone=@Phone AND Email=@Email
  AND (@SiteId IS NULL OR SiteId=@SiteId)
  AND CreatedAt >= DATEADD(HOUR, -@Hours, SYSDATETIME())",
                new { SiteId = siteId, Phone = phone, Email = email, Hours = hours <= 0 ? 24 : hours });
            return n > 0;
        }

        public async Task<ContactLead> GetByIdAsync(int id, int siteId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ContactLead>(@"
SELECT Id, SiteId, Phone, Email, Source, IsRead, Notes, CreatedAt, ConvertedContactId, ConvertedAt
FROM koch.ContactLeads
WHERE Id=@Id AND SiteId=@SiteId", new { Id = id, SiteId = siteId });
        }

        public async Task<List<ContactLead>> ListAsync(int? siteId, int top = 300)
        {
            if (top < 1) top = 300;
            if (top > 1000) top = 1000;
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<ContactLead>(@"
SELECT TOP (@Top) c.Id, c.SiteId, c.Phone, c.Email, c.Source, c.IsRead, c.Notes, c.CreatedAt,
       c.ConvertedContactId, c.ConvertedAt,
       s.Name AS SiteName
FROM koch.ContactLeads c
LEFT JOIN koch.Sites s ON s.Id = c.SiteId
WHERE (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
ORDER BY c.CreatedAt DESC, c.Id DESC",
                new { SiteId = siteId, Top = top });
            return rows?.ToList() ?? new List<ContactLead>();
        }

        public async Task SetReadAsync(int id, int siteId, bool isRead)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.ContactLeads SET IsRead=@IsRead WHERE Id=@Id AND SiteId=@SiteId",
                new { Id = id, SiteId = siteId, IsRead = isRead });
        }

        public async Task<bool> DeleteAsync(int id, int siteId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteAsync(
                "DELETE FROM koch.ContactLeads WHERE Id=@Id AND SiteId=@SiteId",
                new { Id = id, SiteId = siteId }) == 1;
        }

        public async Task<CrmContactDto> ConvertAsync(int id, int siteId, int? createdBy)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<CrmContactDto>(
                "koch.spContactLeads_ConvertToCrmContact",
                new { LeadId = id, SiteId = siteId, CreatedBy = createdBy },
                commandType: System.Data.CommandType.StoredProcedure);
        }
    }
}
