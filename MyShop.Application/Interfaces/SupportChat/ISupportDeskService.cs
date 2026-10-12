using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SupportChat
{
    public interface ISupportDeskRepository
    {
        Task<List<SupportChatThread>> ListInboxAsync(SupportInboxFilter filter);
        Task AssignAsync(Guid chatId, int? adminUserId, int? teamId, string priority, string status, bool updateAssignee = true, bool updateTeam = false);
        Task AddTransferAsync(SupportTransferRow row);
        Task<List<SupportTransferRow>> ListTransfersAsync(Guid chatId);
        Task AddNoteAsync(SupportInternalNote note);
        Task<List<SupportInternalNote>> ListNotesAsync(Guid chatId);
        Task<List<SupportOperatorOption>> ListOperatorsAsync(int? siteId);
        Task<List<SupportTeam>> ListTeamsAsync(int? siteId);
        Task<int> CreateTeamAsync(SupportTeam team);
        Task UpdateTeamAsync(SupportTeam team);
        Task SetTeamMembersAsync(int teamId, IEnumerable<int> userIds);
        Task<List<int>> GetTeamMemberIdsAsync(int teamId);
        Task<List<SupportChatThread>> ListPreviousChatsAsync(int? siteId, int? userId, string guestToken, Guid excludeChatId, int take = 10);
        Task<string> GetUserEmailAsync(int userId);
        Task<List<SupportBlock>> ListBlocksAsync(int? siteId);
        Task AddBlockAsync(SupportBlock block);
        Task SetBlockActiveAsync(int id, bool active);
        Task<bool> IsBlockedAsync(int? siteId, int? userId, string guestToken, string phone);
        Task<List<SupportBusinessHour>> GetBusinessHoursAsync(int siteId);
        Task SaveBusinessHoursAsync(int siteId, IEnumerable<SupportBusinessHour> hours);
        Task<List<SupportHoliday>> ListHolidaysAsync(int siteId);
        Task AddHolidayAsync(SupportHoliday holiday);
        Task DeleteHolidayAsync(int id);
        Task<bool> IsHolidayAsync(int siteId, DateTime date);
    }

    public interface ISupportDeskService
    {
        Task<List<SupportChatThread>> ListInboxAsync(SupportInboxFilter filter);
        Task AssignToMeAsync(Guid chatId, int adminUserId);
        Task AssignAsync(Guid chatId, int actorUserId, int? toAdminUserId, int? toTeamId, string reason = null);
        Task TransferAsync(Guid chatId, int actorUserId, int? toAdminUserId, int? toTeamId, string reason = null);
        Task SetPriorityAsync(Guid chatId, string priority);
        Task SetStatusAsync(Guid chatId, string status);
        Task ReopenAsync(Guid chatId);
        Task AddInternalNoteAsync(Guid chatId, int authorUserId, string body);
        Task<SupportCustomerProfile> GetCustomerProfileAsync(Guid chatId);
        Task<List<SupportOperatorOption>> ListOperatorsAsync(int? siteId);
        Task<List<SupportTeam>> ListTeamsAsync(int? siteId);
        Task<List<int>> GetTeamMemberIdsAsync(int teamId);
        Task<int> SaveTeamAsync(SupportTeam team, IEnumerable<int> memberIds);
        Task<List<SupportBlock>> ListBlocksAsync(int? siteId);
        Task BlockAsync(SupportBlock block);
        Task UnblockAsync(int blockId);
        Task<bool> IsBlockedAsync(int? siteId, int? userId, string guestToken, string phone);
        Task<List<SupportBusinessHour>> GetBusinessHoursAsync(int siteId);
        Task SaveBusinessHoursAsync(int siteId, IEnumerable<SupportBusinessHour> hours);
        Task EnsureDefaultBusinessHoursAsync(int siteId);
        Task<List<SupportHoliday>> ListHolidaysAsync(int siteId);
        Task AddHolidayAsync(SupportHoliday holiday);
        Task DeleteHolidayAsync(int id);
        Task<(bool withinHours, string message)> CheckAvailabilityAsync(int siteId);
    }
}
