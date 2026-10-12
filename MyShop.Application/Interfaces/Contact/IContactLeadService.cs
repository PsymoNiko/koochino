using MyShop.Core.Models.Contact;
using MyShop.Core.Models.Crm;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Contact
{
    public interface IContactLeadRepository
    {
        Task<int> InsertAsync(ContactLead row);
        Task<bool> ExistsRecentAsync(int? siteId, string phone, string email, int hours = 24);
        Task<ContactLead> GetByIdAsync(int id, int siteId);
        Task<List<ContactLead>> ListAsync(int? siteId, int top = 300);
        Task SetReadAsync(int id, int siteId, bool isRead);
        Task<bool> DeleteAsync(int id, int siteId);
        Task<CrmContactDto> ConvertAsync(int id, int siteId, int? createdBy);
    }

    public interface IContactLeadService
    {
        Task<(bool ok, string message)> SubmitAsync(int? siteId, string phone, string email, string source = "landing");
        Task<List<ContactLead>> ListAsync(int? siteId, int top = 300);
        Task SetReadAsync(int id, bool isRead);
        Task<bool> DeleteAsync(int id);
        Task<CrmContactDto> ConvertAsync(int id, int? createdBy = null);
    }
}
