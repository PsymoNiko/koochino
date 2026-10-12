using MyShop.Core.Models.Lucky;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Lucky
{
    public interface ILuckyLeagueService
    {
        // ادمین — لیگ‌ها
        Task<List<LuckyLeague>> GetAllLeaguesAsync(bool activeOnly = false);
        Task<LuckyLeague> GetLeagueByIdAsync(int id);
        Task<(bool Success, string Message)> CreateLeagueAsync(LuckyLeague league);
        Task<(bool Success, string Message)> UpdateLeagueAsync(LuckyLeague league);
        Task<(bool Success, string Message)> ToggleLeagueStatusAsync(int id, bool isActive);

        // ادمین — جوایز
        Task<List<LuckyPrize>> GetPrizesByLeagueIdAsync(int leagueId, bool includeInactive = false);
        Task<List<LuckyPrize>> GetPendingPrizeQueueAsync();
        Task<(bool Success, string Message)> CreateAdminPrizeAsync(LuckyPrize prize, int adminUserId);
        Task<(bool Success, string Message)> UpdatePrizeAsync(LuckyPrize prize);
        Task<(bool Success, string Message)> DeletePrizeAsync(int id);
        Task<(bool Success, string Message)> TogglePrizeStatusAsync(int id, bool isActive);
        Task<(bool Success, string Message)> ApprovePrizeAsync(int id, bool approve, int adminUserId, string note);

        // فروشگاه‌دار
        Task<List<LuckyPrize>> GetMyProposedPrizesAsync(int storeOwnerUserId);
        Task<(bool Success, string Message)> ProposeStorePrizeAsync(LuckyPrize prize, int storeOwnerUserId, int storeId);

        // مشتری
        Task<List<LuckyLeagueViewDto>> GetEligibleLeaguesAsync(int userId);
        Task<List<LuckyBox>> GetOrStartRoundAsync(int userId, int leagueId);
        Task<LuckyBoxOpenResultDto> OpenBoxAsync(int userId, int leagueId, int roundId, int boxId, string paymentMethod);
        Task<(bool Success, string Message, int NewBalance)> ConvertPointsToChanceAsync(int userId, int leagueId);

        Task<List<LuckyBoxOpenReportRow>> GetBoxOpenReportAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search, int skip, int take);
        Task<LuckyBoxOpenReportStats> GetBoxOpenReportStatsAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search);
    }
}
