using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SupportChat
{
    public class SupportAnalyticsService : ISupportAnalyticsService
    {
        private readonly ISupportAnalyticsRepository _repo;
        private readonly ISupportChatRepository _chats;

        public SupportAnalyticsService(ISupportAnalyticsRepository repo, ISupportChatRepository chats)
        {
            _repo = repo;
            _chats = chats;
        }

        public Task<SupportAnalyticsSummary> GetSummaryAsync(SupportAnalyticsFilter filter) =>
            _repo.GetSummaryAsync(Normalize(filter));

        public Task<List<SupportDailyStat>> GetDailyAsync(SupportAnalyticsFilter filter) =>
            _repo.GetDailyAsync(Normalize(filter));

        public Task<List<SupportOperatorStat>> GetOperatorStatsAsync(SupportAnalyticsFilter filter) =>
            _repo.GetOperatorStatsAsync(Normalize(filter));

        public async Task SubmitCsatAsync(Guid chatId, byte score, string comment, int? userId, string guestToken)
        {
            if (score < 1 || score > 5)
                throw new InvalidOperationException("امتیاز باید بین ۱ تا ۵ باشد.");
            var chat = await _chats.GetAsync(chatId) ?? throw new InvalidOperationException("گفتگو یافت نشد.");
            if (chat.Status != SupportChatStatuses.Closed)
                throw new InvalidOperationException("فقط پس از بستن گفتگو می‌توان امتیاز داد.");

            if (userId.HasValue && userId.Value > 0)
            {
                if (chat.UserId.HasValue && chat.UserId.Value != userId.Value)
                    throw new InvalidOperationException("اجازه ثبت امتیاز برای این گفتگو را ندارید.");
            }
            else if (!string.IsNullOrWhiteSpace(guestToken))
            {
                if (!string.Equals(chat.GuestToken, guestToken.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("اجازه ثبت امتیاز برای این گفتگو را ندارید.");
            }
            else
                throw new InvalidOperationException("شناسه کاربر یا مهمان برای امتیازدهی لازم است.");

            await _repo.UpsertCsatAsync(new SupportCsatRating
            {
                ChatId = chatId,
                SiteId = chat.SiteId,
                Score = score,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                RatedByUserId = userId,
                GuestToken = string.IsNullOrWhiteSpace(guestToken) ? null : guestToken.Trim()
            });
        }

        public Task<SupportCsatRating> GetCsatAsync(Guid chatId) => _repo.GetCsatAsync(chatId);

        public async Task<bool> HasCsatAsync(Guid chatId) =>
            await _repo.GetCsatAsync(chatId) != null;

        static SupportAnalyticsFilter Normalize(SupportAnalyticsFilter filter)
        {
            filter ??= new SupportAnalyticsFilter();
            if (filter.From == default) filter.From = DateTime.Today.AddDays(-30);
            if (filter.To == default) filter.To = DateTime.Today;
            return filter;
        }
    }
}
