using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Messaging
{
    public interface IMessagingOutboundChannelRepository
    {
        Task<List<MessagingOutboundChannel>> ListAsync(string provider = null, int? siteId = null, bool activeOnly = false);
        Task<MessagingOutboundChannel> GetByIdAsync(int id);
        Task<int> UpsertAsync(MessagingOutboundChannel channel);
        Task DeleteAsync(int id);
        Task UpdateChatIdAsync(int id, string chatId);
    }

    public class ChannelBroadcastResultItem
    {
        public int ChannelId { get; set; }
        public string Title { get; set; }
        public string Provider { get; set; }
        public string Target { get; set; }
        public bool Ok { get; set; }
        public string Message { get; set; }
    }

    public interface IChannelBroadcastService
    {
        Task<List<MessagingOutboundChannel>> ListChannelsAsync(string provider = null, int? siteId = null);
        Task<int> SaveChannelAsync(MessagingOutboundChannel channel);
        Task DeleteChannelAsync(int id);
        /// <summary>getChat برای کشف ChatId عددی؛ در صورت موفقیت در رکورد ذخیره می‌شود.</summary>
        Task<(bool Ok, string ChatId, string Message)> ResolveChatIdAsync(int channelId, int? siteId = null);
        /// <summary>ارسال متن به کانال با بات resolve‌شده برای همان Provider/Site. photoUrl اختیاری (کاور).</summary>
        Task<(bool Ok, string Message)> SendAsync(int channelId, string text, int? siteId = null, string photoUrl = null);
        /// <summary>ارسال سریع بدون رکورد ذخیره‌شده (username مثل hoseinthings).</summary>
        Task<(bool Ok, string Message)> SendQuickAsync(string provider, string usernameOrChatId, string text, int? siteId = null, string photoUrl = null);
        /// <summary>ارسال به همه کانال‌های فعال (اختیاری فیلتر سایت).</summary>
        Task<List<ChannelBroadcastResultItem>> SendToAllActiveAsync(string text, int? siteId = null, string photoUrl = null);
    }
}
