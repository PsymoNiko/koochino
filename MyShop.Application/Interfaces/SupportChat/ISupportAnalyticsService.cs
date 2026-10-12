using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SupportChat
{
    public interface ISupportAnalyticsRepository
    {
        Task MarkFirstResponseAsync(Guid chatId);
        Task UpsertCsatAsync(SupportCsatRating rating);
        Task<SupportCsatRating> GetCsatAsync(Guid chatId);
        Task<SupportAnalyticsSummary> GetSummaryAsync(SupportAnalyticsFilter filter);
        Task<List<SupportDailyStat>> GetDailyAsync(SupportAnalyticsFilter filter);
        Task<List<SupportOperatorStat>> GetOperatorStatsAsync(SupportAnalyticsFilter filter);
    }

    public interface ISupportAnalyticsService
    {
        Task<SupportAnalyticsSummary> GetSummaryAsync(SupportAnalyticsFilter filter);
        Task<List<SupportDailyStat>> GetDailyAsync(SupportAnalyticsFilter filter);
        Task<List<SupportOperatorStat>> GetOperatorStatsAsync(SupportAnalyticsFilter filter);
        Task SubmitCsatAsync(Guid chatId, byte score, string comment, int? userId, string guestToken);
        Task<SupportCsatRating> GetCsatAsync(Guid chatId);
        Task<bool> HasCsatAsync(Guid chatId);
    }
}
