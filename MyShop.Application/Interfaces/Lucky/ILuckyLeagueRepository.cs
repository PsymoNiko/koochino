using MyShop.Core.Models.Lucky;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Lucky
{
    public interface ILuckyLeagueRepository
    {
        Task<List<LuckyLeague>> GetAllLeaguesAsync(bool activeOnly = false);
        Task<LuckyLeague> GetLeagueByIdAsync(int id);
        Task<int> CreateLeagueAsync(LuckyLeague league);
        Task<bool> UpdateLeagueAsync(LuckyLeague league);
        Task<bool> ToggleLeagueStatusAsync(int id, bool isActive);
        Task<List<LuckyLeagueViewDto>> GetEligibleLeaguesForUserAsync(int userId, int availablePoints, int? siteId);

        Task<List<LuckyPrize>> GetPrizesByLeagueIdAsync(int leagueId, bool includeInactive = false);
        Task<LuckyPrize> GetPrizeByIdAsync(int id);
        Task<List<LuckyPrize>> GetPendingPrizeQueueAsync();
        Task<List<LuckyPrize>> GetPrizesByCreatorAsync(int createdByUserId);
        Task<int> CreatePrizeAsync(LuckyPrize prize);
        Task<bool> UpdatePrizeAsync(LuckyPrize prize);
        Task<int> DeletePrizeAsync(int id);
        Task<bool> TogglePrizeStatusAsync(int id, bool isActive);
        Task<bool> ApprovePrizeAsync(int id, bool approve, int approvedByUserId, string adminNote);

        Task<List<LuckyBox>> GetActiveRoundBoxesAsync(int userId, int leagueId);
        Task<int> CreateRoundAsync(int userId, int leagueId);
        Task<(int Total, int Opened)> CountRoundOpenAsync(int roundId);

        Task<(int Result, bool IsLosing, string PrizeType, string PrizeTitle, int? PointsCredited, int? PrizeId, int PointsSpent)>
            OpenBoxAsync(int boxId, int userId, string paymentMethod);

        Task SetBoxResultCouponAsync(int boxId, int coponId);
        Task RefundBoxCostAsync(int boxId, int userId, int leagueId, string paymentMethod, int pointsSpent);

        Task<(int Result, int NewChanceBalance)> ConvertPointsToChanceAsync(int userId, int leagueId);

        Task<List<LuckyBoxOpenReportRow>> GetBoxOpenReportAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search, int skip, int take);
        Task<LuckyBoxOpenReportStats> GetBoxOpenReportStatsAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search);
    }
}
