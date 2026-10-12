using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Interfaces.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class SupportChannelOutbound : ISupportChannelOutbound
    {
        private readonly IEnumerable<IMessagingChannel> _channels;
        public SupportChannelOutbound(IEnumerable<IMessagingChannel> channels) => _channels = channels;

        public async Task SendToExternalAsync(string provider, string externalChatId, string text)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalChatId) || string.IsNullOrWhiteSpace(text))
                return;
            var ch = _channels?.FirstOrDefault(c => string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));
            if (ch == null) return;
            await ch.SendTextAsync(externalChatId.Trim(), text);
        }
    }
}
