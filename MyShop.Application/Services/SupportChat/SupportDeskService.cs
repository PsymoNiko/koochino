using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SupportChat
{
    public class SupportDeskService : ISupportDeskService
    {
        private readonly ISupportDeskRepository _repo;
        private readonly ISupportChatRepository _chats;
        private readonly ISupportChatRealtimePublisher _realtime;

        public SupportDeskService(
            ISupportDeskRepository repo,
            ISupportChatRepository chats,
            ISupportChatRealtimePublisher realtime = null)
        {
            _repo = repo;
            _chats = chats;
            _realtime = realtime;
        }

        public Task<List<SupportChatThread>> ListInboxAsync(SupportInboxFilter filter) =>
            _repo.ListInboxAsync(filter);

        public async Task AssignToMeAsync(Guid chatId, int adminUserId)
        {
            await _repo.AssignAsync(chatId, adminUserId, null, null, SupportChatStatuses.Active, updateAssignee: true, updateTeam: false);
            await Notify(chatId, SupportChatStatuses.Active);
        }

        public async Task AssignAsync(Guid chatId, int actorUserId, int? toAdminUserId, int? toTeamId, string reason = null)
        {
            var chat = await _chats.GetAsync(chatId) ?? throw new InvalidOperationException("گفتگو یافت نشد.");
            await _repo.AddTransferAsync(new SupportTransferRow
            {
                ChatId = chatId,
                FromAdminUserId = chat.AssignedAdminUserId,
                ToAdminUserId = toAdminUserId,
                FromTeamId = chat.TeamId,
                ToTeamId = toTeamId ?? chat.TeamId,
                Reason = Trunc(reason, 400) ?? "تخصیص",
                CreatedByUserId = actorUserId
            });
            await _repo.AssignAsync(chatId, toAdminUserId, toTeamId, null, SupportChatStatuses.Active,
                updateAssignee: true, updateTeam: toTeamId.HasValue);
            await _chats.AddMessageAsync(new SupportChatMessage
            {
                ChatId = chatId,
                SenderType = SupportChatSenders.System,
                Body = "گفتگو تخصیص داده شد.",
                MessageType = SupportMessageTypes.SystemEvent
            });
            await Notify(chatId, SupportChatStatuses.Active);
        }

        public Task TransferAsync(Guid chatId, int actorUserId, int? toAdminUserId, int? toTeamId, string reason = null) =>
            AssignAsync(chatId, actorUserId, toAdminUserId, toTeamId, string.IsNullOrWhiteSpace(reason) ? "انتقال" : reason);

        public async Task SetPriorityAsync(Guid chatId, string priority)
        {
            var p = string.IsNullOrWhiteSpace(priority) ? SupportPriorities.Normal : priority.Trim();
            await _repo.AssignAsync(chatId, null, null, p, null, updateAssignee: false, updateTeam: false);
            await Notify(chatId, null);
        }

        public async Task SetStatusAsync(Guid chatId, string status)
        {
            await _repo.AssignAsync(chatId, null, null, null, status, updateAssignee: false, updateTeam: false);
            await Notify(chatId, status);
        }

        public async Task ReopenAsync(Guid chatId)
        {
            await _repo.AssignAsync(chatId, null, null, null, SupportChatStatuses.Waiting, updateAssignee: false, updateTeam: false);
            await _chats.AddMessageAsync(new SupportChatMessage
            {
                ChatId = chatId,
                SenderType = SupportChatSenders.System,
                Body = "گفتگو دوباره باز شد.",
                MessageType = SupportMessageTypes.SystemEvent
            });
            await Notify(chatId, SupportChatStatuses.Waiting);
        }

        public async Task AddInternalNoteAsync(Guid chatId, int authorUserId, string body)
        {
            var text = (body ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            await _repo.AddNoteAsync(new SupportInternalNote
            {
                ChatId = chatId,
                AuthorUserId = authorUserId,
                Body = text
            });
        }

        public async Task<SupportCustomerProfile> GetCustomerProfileAsync(Guid chatId)
        {
            var chat = await _chats.GetAsync(chatId) ?? throw new InvalidOperationException("گفتگو یافت نشد.");
            var prev = await _repo.ListPreviousChatsAsync(chat.SiteId, chat.UserId, chat.GuestToken, chatId);
            var notes = await _repo.ListNotesAsync(chatId);
            var transfers = await _repo.ListTransfersAsync(chatId);
            string email = null;
            if (chat.UserId.HasValue)
                email = await _repo.GetUserEmailAsync(chat.UserId.Value);
            var blocked = await _repo.IsBlockedAsync(chat.SiteId, chat.UserId, chat.GuestToken, chat.VisitorPhone ?? chat.UserPhone);
            return new SupportCustomerProfile
            {
                ChatId = chatId,
                UserId = chat.UserId,
                Name = FirstNonEmpty(chat.UserDisplayName, chat.VisitorName, "مهمان"),
                Phone = FirstNonEmpty(chat.UserPhone, chat.VisitorPhone),
                Email = email,
                Audience = chat.Audience,
                CurrentPageUrl = chat.CurrentPageUrl,
                FirstSeen = chat.CreatedAt,
                LastActivity = chat.LastMessageAt,
                PreviousConversationCount = prev.Count,
                PreviousConversations = prev,
                Notes = notes,
                Transfers = transfers,
                IsBlocked = blocked
            };
        }

        public Task<List<SupportOperatorOption>> ListOperatorsAsync(int? siteId) => _repo.ListOperatorsAsync(siteId);
        public Task<List<SupportTeam>> ListTeamsAsync(int? siteId) => _repo.ListTeamsAsync(siteId);
        public Task<List<int>> GetTeamMemberIdsAsync(int teamId) => _repo.GetTeamMemberIdsAsync(teamId);

        public async Task<int> SaveTeamAsync(SupportTeam team, IEnumerable<int> memberIds)
        {
            if (team == null || string.IsNullOrWhiteSpace(team.Name))
                throw new InvalidOperationException("نام تیم الزامی است.");
            team.Name = team.Name.Trim();
            int id;
            if (team.Id <= 0)
                id = await _repo.CreateTeamAsync(team);
            else
            {
                await _repo.UpdateTeamAsync(team);
                id = team.Id;
            }
            await _repo.SetTeamMembersAsync(id, memberIds ?? Enumerable.Empty<int>());
            return id;
        }

        public Task<List<SupportBlock>> ListBlocksAsync(int? siteId) => _repo.ListBlocksAsync(siteId);
        public Task BlockAsync(SupportBlock block) => _repo.AddBlockAsync(block);
        public Task UnblockAsync(int blockId) => _repo.SetBlockActiveAsync(blockId, false);
        public Task<bool> IsBlockedAsync(int? siteId, int? userId, string guestToken, string phone) =>
            _repo.IsBlockedAsync(siteId, userId, guestToken, phone);

        public Task<List<SupportBusinessHour>> GetBusinessHoursAsync(int siteId) => _repo.GetBusinessHoursAsync(siteId);

        public async Task EnsureDefaultBusinessHoursAsync(int siteId)
        {
            var existing = await _repo.GetBusinessHoursAsync(siteId);
            if (existing.Count > 0) return;
            var hours = Enumerable.Range(0, 7).Select(d => new SupportBusinessHour
            {
                SiteId = siteId,
                DayOfWeek = (byte)d,
                IsOpen = d is >= 1 and <= 5,
                OpenTime = TimeSpan.FromHours(9),
                CloseTime = TimeSpan.FromHours(18)
            });
            await _repo.SaveBusinessHoursAsync(siteId, hours);
        }

        public Task SaveBusinessHoursAsync(int siteId, IEnumerable<SupportBusinessHour> hours) =>
            _repo.SaveBusinessHoursAsync(siteId, hours);

        public Task<List<SupportHoliday>> ListHolidaysAsync(int siteId) => _repo.ListHolidaysAsync(siteId);
        public Task AddHolidayAsync(SupportHoliday holiday) => _repo.AddHolidayAsync(holiday);
        public Task DeleteHolidayAsync(int id) => _repo.DeleteHolidayAsync(id);

        public async Task<(bool withinHours, string message)> CheckAvailabilityAsync(int siteId)
        {
            var settings = await _chats.GetSettingsAsync(siteId);
            if (settings != null && !settings.IsChatEnabled)
                return (false, settings.OfflineMessage ?? "چت غیرفعال است.");
            if (settings == null || !settings.UseBusinessHours)
                return (true, null);

            await EnsureDefaultBusinessHoursAsync(siteId);
            DateTime now;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId ?? "Iran Standard Time");
                now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                now = DateTime.Now;
            }

            if (await _repo.IsHolidayAsync(siteId, now.Date))
                return (false, settings.OfflineMessage ?? "امروز تعطیل رسمی پشتیبانی است.");

            var hours = await _repo.GetBusinessHoursAsync(siteId);
            var today = hours.FirstOrDefault(h => h.DayOfWeek == (byte)now.DayOfWeek);
            if (today == null || !today.IsOpen)
                return (false, settings.OfflineMessage ?? "خارج از روزهای کاری هستیم.");
            var t = now.TimeOfDay;
            if (t < today.OpenTime || t > today.CloseTime)
                return (false, settings.OfflineMessage ?? "خارج از ساعات کاری هستیم.");
            return (true, null);
        }

        async Task Notify(Guid chatId, string status)
        {
            if (_realtime == null) return;
            var chat = await _chats.GetAsync(chatId);
            if (chat != null)
                await _realtime.ConversationUpdatedAsync(chatId, chat.SiteId, status ?? chat.Status);
        }

        static string FirstNonEmpty(params string[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            return s.Length <= n ? s : s.Substring(0, n);
        }
    }
}
