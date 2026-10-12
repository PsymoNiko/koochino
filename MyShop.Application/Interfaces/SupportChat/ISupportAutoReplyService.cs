using MyShop.Core.Models.SupportChat;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SupportChat
{
    public interface ISupportAutoReplyService
    {
        Task TryAfterUserMessageAsync(SupportChatThread chat, SupportChatMessage userMessage);
    }

    public interface ISupportChannelOutbound
    {
        Task SendToExternalAsync(string provider, string externalChatId, string text);
    }

    public interface ISupportOmnichannelService
    {
        Task<bool> IsEnabledAsync(int siteId);
        Task<bool> ShouldInterceptFreeTextAsync(int siteId);
        Task HandleInboundAsync(string provider, int siteId, string externalChatId, string visitorName, string text);
    }
}
