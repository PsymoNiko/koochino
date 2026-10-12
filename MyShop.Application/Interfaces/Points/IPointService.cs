using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IPointService
    {
        // Point Missions
        Task<List<PointMission>> GetMissionsAsync(bool activeOnly = false, int? siteId = null);
        Task<PointMission> GetMissionByIdAsync(int id);
        Task<(bool Success, string Message, int MissionId)> CreateMissionAsync(PointMission mission, int adminUserId);
        Task<(bool Success, string Message)> UpdateMissionAsync(PointMission mission);
        Task<(bool Success, string Message)> DeleteMissionAsync(int id);
        Task<(bool Success, string Message)> ToggleMissionStatusAsync(int id, bool isActive);

        // User Points
        Task<UserPointsSummaryDto> GetUserPointsSummaryAsync(int userId);
        Task<List<UserPoints>> GetAllUserPointsAsync(int skip = 0, int take = 100);

        // Point Transactions
        Task<(bool Success, string Message, int TransactionId)> CreateTransactionAsync(
            int userId, 
            string missionType, 
            int points, 
            string reason, 
            int? missionId = null, 
            string referenceId = null, 
            string referenceType = null);
        
        Task<(bool Success, string Message)> ApproveTransactionAsync(int transactionId, int adminUserId, string notes = null);
        Task<(bool Success, string Message)> RejectTransactionAsync(int transactionId, int adminUserId, string notes = null);
        
        Task<List<PointTransactionDto>> GetUserTransactionsAsync(int userId, int count = 10);
        Task<List<PointTransactionDto>> GetPendingTransactionsAsync(int skip = 0, int take = 50);
        Task<List<PointTransactionDto>> GetAllTransactionsAsync(int skip = 0, int take = 100, string status = null);
        Task<List<PointTransactionDto>> GetLatestTransactionsAsync(int count = 10);
        
        // Check Mission Eligibility
        Task<(bool IsEligible, string Message)> CheckMissionEligibilityAsync(int userId, int missionId);

        Task<PointsLoyaltyDashboardDto> GetLoyaltyDashboardAsync();
    }
}
