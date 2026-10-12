using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Application.Interfaces.Sites;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.AdminAccess;
using MyShop.Web.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Web.Hubs
{
    /// <summary>چت زنده پشتیبانی — WebSockets با fallback خودکار LongPolling برای Plesk.</summary>
    public class SupportChatHub : Hub
    {
        private readonly ISupportChatService _chats;
        private readonly SupportChatAccessTokenService _tokens;
        private readonly ISiteService _sites;
        private readonly IAdminPermissionService _perms;

        public SupportChatHub(
            ISupportChatService chats,
            SupportChatAccessTokenService tokens,
            ISiteService sites,
            IAdminPermissionService perms)
        {
            _chats = chats;
            _tokens = tokens;
            _sites = sites;
            _perms = perms;
        }

        public async Task JoinConversation(Guid chatId)
        {
            if (!await CanAccessConversationAsync(chatId))
                throw new HubException("دسترسی به این گفتگو مجاز نیست.");
            await Groups.AddToGroupAsync(Context.ConnectionId, ConvGroup(chatId));
        }

        public async Task LeaveConversation(Guid chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConvGroup(chatId));
        }

        [Authorize]
        public async Task JoinOperatorInbox(int? siteId)
        {
            if (!await CanAdminSiteAsync(siteId))
                throw new HubException("دسترسی به صندوق این سایت مجاز نیست.");
            await Groups.AddToGroupAsync(Context.ConnectionId, OpsGroup(siteId));
        }

        public async Task Typing(Guid chatId, string senderSide, bool isTyping)
        {
            if (!await CanAccessConversationAsync(chatId))
                return;
            await _chats.NotifyTypingAsync(chatId, senderSide ?? "User", isTyping);
        }

        async Task<bool> CanAccessConversationAsync(Guid chatId)
        {
            if (chatId == Guid.Empty) return false;
            var chat = await _chats.GetAsync(chatId);
            if (chat == null) return false;

            if (await IsSupportOperatorAsync())
                return await CanAdminSiteAsync(chat.SiteId);

            var userId = TryGetUserId();
            if (userId.HasValue && chat.UserId.HasValue && chat.UserId.Value == userId.Value)
                return true;

            return _tokens.TryAuthorize(chatId, chat.SiteId);
        }

        async Task<bool> CanAdminSiteAsync(int? siteId)
        {
            if (!await IsSupportOperatorAsync()) return false;
            var userId = TryGetUserId();
            if (!userId.HasValue) return false;
            if (!siteId.HasValue || siteId.Value <= 0) return true;
            return await _sites.AdminCanAccessSiteAsync(userId.Value, siteId.Value);
        }

        async Task<bool> IsSupportOperatorAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated != true) return false;
            return await PermissionCheckHelper.HasAdminModuleAsync(user, _perms, "Support", AdminPermissionActions.Read);
        }

        int? TryGetUserId()
        {
            var raw = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("UserId")?.Value;
            return int.TryParse(raw, out var id) && id > 0 ? id : null;
        }

        static string ConvGroup(Guid chatId) => "conv:" + chatId.ToString("N");
        static string OpsGroup(int? siteId) => "ops:site:" + (siteId ?? 0);
    }
}
