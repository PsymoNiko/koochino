using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IPointRepository
    {
        // Point Missions
        Task<List<PointMission>> GetMissionsAsync(bool activeOnly = false, int? siteId = null);
        Task<PointMission> GetMissionByIdAsync(int id);
        Task<int> CreateMissionAsync(PointMission mission);
        Task<bool> UpdateMissionAsync(PointMission mission);
        Task<bool> DeleteMissionAsync(int id);
        Task<bool> ToggleMissionStatusAsync(int id, bool isActive);

        // User Points
        Task<UserPoints> GetUserPointsAsync(int userId);
        Task<bool> UpdateUserPointsAsync(int userId, int totalPoints, int availablePoints, int pendingPoints);
        Task<List<UserPoints>> GetAllUserPointsAsync(int skip = 0, int take = 100);

        // Point Transactions
        Task<int> CreateTransactionAsync(PointTransaction transaction);
        Task<bool> UpdateTransactionStatusAsync(int transactionId, string status, int? reviewedByUserId, string reviewNotes = null);
        Task<PointTransaction> GetTransactionByIdAsync(int id);
        Task<List<PointTransactionDto>> GetUserTransactionsAsync(int userId, int count = 10);
        Task<List<PointTransactionDto>> GetPendingTransactionsAsync(int skip = 0, int take = 50);
        Task<List<PointTransactionDto>> GetAllTransactionsAsync(int skip = 0, int take = 100, string status = null);
        Task<List<PointTransactionDto>> GetLatestTransactionsAsync(int count = 10);
        
        // Check Mission Eligibility
        Task<(bool IsEligible, string Message)> CheckMissionEligibilityAsync(int userId, int missionId);
        
        // Get User Claim Count
        Task<int> GetUserClaimCountAsync(int userId, int missionId);

        Task<PointsLoyaltyDashboardDto> GetLoyaltyDashboardAsync();
    }
}
