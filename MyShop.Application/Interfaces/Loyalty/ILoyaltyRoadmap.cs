using MyShop.Core.Models.Loyalty;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Loyalty
{
    public interface ILoyaltyRoadmapRepository
    {
        Task<List<SmsCampaign>> ListCampaignsAsync(int? siteId, int top = 100);
        Task<SmsCampaign> GetCampaignAsync(int id);
        Task<int> UpsertCampaignAsync(SmsCampaign c);
        Task UpdateCampaignStatsAsync(int id, string status, int sent, int fail);

        Task<List<CustomerSegment>> ListSegmentsAsync(int? siteId);
        Task<int> UpsertSegmentAsync(CustomerSegment s);
        Task DeleteSegmentAsync(int id);

        Task UpsertRfmAsync(CustomerRfmScore score);
        Task<List<CustomerRfmScore>> ListRfmAsync(int? siteId, int top = 500);

        Task<List<VipTier>> ListVipTiersAsync(int? siteId);
        Task<int> UpsertVipTierAsync(VipTier t);
        Task DeleteVipTierAsync(int id);

        Task<decimal> GetWalletBalanceAsync(int userId);
        Task<List<WalletTransaction>> ListWalletTxAsync(int userId, int top = 50);
        Task AdjustWalletAsync(int userId, decimal amount, string txType, string reason, int? byUserId, string referenceId);

        Task InsertNpsAsync(NpsResponse r);
        Task<List<NpsResponse>> ListNpsAsync(int? siteId, int top = 200);

        Task<List<LoyaltyAutomation>> ListAutomationsAsync(int? siteId);
        Task<int> UpsertAutomationAsync(LoyaltyAutomation a);
        Task DeleteAutomationAsync(int id);

        Task SetUserBirthDateAsync(int userId, DateTime? birthDate);
        Task<DateTime?> GetUserBirthDateAsync(int userId);
        Task<List<(int UserId, string Phone, DateTime? BirthDate)>> ListUsersForAudienceAsync(int? siteId, int top = 5000);
    }

    public interface ILoyaltyRoadmapService
    {
        Task<List<SmsCampaign>> ListCampaignsAsync(int? siteId);
        Task<int> SaveCampaignAsync(SmsCampaign c);
        Task<(bool ok, string message)> SendCampaignAsync(int campaignId);

        Task<List<CustomerSegment>> ListSegmentsAsync(int? siteId);
        Task<int> SaveSegmentAsync(CustomerSegment s);
        Task RecalculateRfmAsync(int? siteId);
        Task<List<CustomerRfmScore>> ListRfmAsync(int? siteId);

        Task<List<VipTier>> ListVipTiersAsync(int? siteId);
        Task<int> SaveVipTierAsync(VipTier t);
        Task DeleteVipTierAsync(int id);
        Task<VipTier> ResolveVipTierAsync(int userId, int? siteId);

        Task<decimal> GetWalletBalanceAsync(int userId);
        Task<List<WalletTransaction>> ListWalletTxAsync(int userId);
        Task<(bool ok, string message)> AdjustWalletAsync(int userId, decimal amount, string reason, int? adminUserId);

        Task<(bool ok, string message)> SubmitNpsAsync(int userId, byte score, string comment, int? orderId, int? siteId);
        Task<List<NpsResponse>> ListNpsAsync(int? siteId);

        Task<List<LoyaltyAutomation>> ListAutomationsAsync(int? siteId);
        Task<int> SaveAutomationAsync(LoyaltyAutomation a);
        Task DeleteAutomationAsync(int id);

        Task SetBirthDateAsync(int userId, DateTime? birthDate);
        Task<DateTime?> GetBirthDateAsync(int userId);

        Task<string> BuildAiCampaignSuggestionAsync(int? siteId);
    }
}
