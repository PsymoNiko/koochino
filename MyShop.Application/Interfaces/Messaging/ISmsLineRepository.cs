using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Messaging
{
    public interface ISmsLineRepository
    {
        Task<List<SmsLine>> ListAsync(int? siteId = null);
        Task<SmsLine> GetByIdAsync(int id);
        Task<SmsLine> GetDefaultAsync(int? siteId = null);
        Task<int> UpsertAsync(SmsLine line, bool setAsDefault = false);
        Task DeleteAsync(int id);
        Task SetDefaultAsync(int id, int? siteId = null);
        Task<SmsProviderSettings> GetProviderSettingsAsync(string provider = "SmsIr");
        Task SetUseSandboxAsync(bool useSandbox, string provider = "SmsIr");
    }
}
