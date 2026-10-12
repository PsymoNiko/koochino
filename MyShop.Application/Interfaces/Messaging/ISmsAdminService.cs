using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Messaging
{
    public record SmsConfigurationStatus(
        bool IsConfigured,
        bool UseSandbox,
        string Mode,
        string MaskedApiKey,
        string LineNumber,
        IReadOnlyList<SmsLine> Lines = null,
        int? DefaultLineId = null);

    public interface ISmsAdminService
    {
        SmsConfigurationStatus GetConfigurationStatus();
        Task<SmsConfigurationStatus> GetConfigurationStatusAsync(int? siteId = null);
        Task<SmsSendResult> SendTestAsync(string phone, int? lineId = null);
        Task<SmsSendResult> SendBulkAsync(SmsBulkSendRequest request);
        Task<List<SmsDeliveryReport>> GetPackReportAsync(string packId);
        Task<SmsDeliveryReport> GetMessageReportAsync(string messageId);
        Task<string> RefreshDeliveryForLogAsync(MessagingSendLog log);
        Task<List<MessagingSendLog>> GetRecentLogsAsync(int top = 50);
        Task<List<MessagingSendLog>> SearchLogsAsync(SmsSendLogFilter filter);
        Task<List<SmsLine>> ListLinesAsync(int? siteId = null);
        Task<int> SaveLineAsync(SmsLine line, bool setAsDefault = false);
        Task DeleteLineAsync(int id);
        Task SetDefaultLineAsync(int id, int? siteId = null);
        Task SetUseSandboxAsync(bool useSandbox);
    }
}
