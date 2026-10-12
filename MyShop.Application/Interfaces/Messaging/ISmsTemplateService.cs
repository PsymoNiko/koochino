using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Messaging
{
    public interface ISmsTemplateService
    {
        Task<List<SmsTemplate>> ListAsync(bool activeOnly = false);
        Task<SmsTemplate> GetByCodeAsync(string code);
        Task<int> UpsertAsync(SmsTemplate template);
        Task DeleteAsync(int id);
        string Render(string bodyTemplate, IDictionary<string, string> parameters);
    }
}
