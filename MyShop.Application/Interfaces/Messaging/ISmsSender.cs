using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Messaging
{
    public record SmsSendResult(bool Ok, string MessageId = null, string Error = null);

    public interface ISmsSender
    {
        Task<SmsSendResult> SendAsync(string phone, string message, int? lineId = null, string messageType = null, string templateCode = null);
        Task<SmsSendResult> SendTestAsync(string phone, int? lineId = null);
        Task<SmsSendResult> SendVerifyAsync(string phone, int templateId, IDictionary<string, string> parameters, string messageType = null, string templateCode = null);

        /// <summary>ارسال پترن ملی‌پیامک با SendByBaseNumber2 (BaseServiceNumber) — برای قالب‌هایی که MeliPayamakBodyId تنظیم شده.</summary>
        Task<SmsSendResult> SendMeliSharedAsync(string phone, int bodyId, string[] args, int? lineId = null, string messageType = null, string templateCode = null);
    }
}
