using Dapper;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.SupportChat
{
    public class SupportDeskRepository : ISupportDeskRepository
    {
        private const string ThreadSelect = @"
SELECT c.Id, c.SiteId, c.UserId, c.GuestToken, c.VisitorName, c.VisitorPhone, c.Audience, c.Status,
       ISNULL(c.Priority, N'Normal') AS Priority, ISNULL(c.Channel, N'Website') AS Channel,
       c.AssignedAdminUserId, c.TeamId, t.Name AS TeamName,
       c.LastMessageAt, c.LastMessagePreview, c.UnreadByAdmin, c.UnreadByUser, c.CreatedAt,
       ISNULL(c.UpdatedAt, c.CreatedAt) AS UpdatedAt, c.ClosedAt, c.CurrentPageUrl,
       LTRIM(RTRIM(ISNULL(u.FirstName,N'') + N' ' + ISNULL(u.LastName,N''))) AS UserDisplayName,
       u.PhoneNumber AS UserPhone,
       LTRIM(RTRIM(ISNULL(a.FirstName,N'') + N' ' + ISNULL(a.LastName,N''))) AS AssignedAdminName
FROM koch.SupportChats c
LEFT JOIN koch.Users u ON u.Id = c.UserId
LEFT JOIN koch.Users a ON a.Id = c.AssignedAdminUserId
LEFT JOIN koch.SupportTeams t ON t.Id = c.TeamId";

        private readonly IDbConnectionFactory _factory;
        public SupportDeskRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SupportChatThread>> ListInboxAsync(SupportInboxFilter filter)
        {
            filter ??= new SupportInboxFilter();
            using var conn = _factory.CreateConnection();
            var sql = ThreadSelect + @"
WHERE (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId)
  AND (@Status IS NULL OR @Status=N'' OR c.Status=@Status)
  AND (@Priority IS NULL OR @Priority=N'' OR ISNULL(c.Priority,N'Normal')=@Priority)
  AND (@TeamId IS NULL OR c.TeamId=@TeamId)
  AND (@OnlyUnassigned=0 OR c.AssignedAdminUserId IS NULL)
  AND (@OnlyMine=0 OR c.AssignedAdminUserId=@MineUserId)
  AND (@AssignedAdminUserId IS NULL OR c.AssignedAdminUserId=@AssignedAdminUserId)
  AND (
        @Search IS NULL OR @Search=N'' OR
        c.VisitorName LIKE N'%' + @Search + N'%' OR
        c.VisitorPhone LIKE N'%' + @Search + N'%' OR
        c.LastMessagePreview LIKE N'%' + @Search + N'%' OR
        u.FirstName LIKE N'%' + @Search + N'%' OR
        u.LastName LIKE N'%' + @Search + N'%' OR
        u.PhoneNumber LIKE N'%' + @Search + N'%'
      )
ORDER BY c.LastMessageAt DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY";
            var rows = await conn.QueryAsync<SupportChatThread>(sql, new
            {
                filter.SiteId,
                filter.Status,
                filter.Priority,
                filter.TeamId,
                OnlyUnassigned = filter.OnlyUnassigned ? 1 : 0,
                OnlyMine = filter.OnlyMine ? 1 : 0,
                MineUserId = filter.MineUserId,
                filter.AssignedAdminUserId,
                Search = filter.Search,
                Take = filter.Take <= 0 ? 80 : filter.Take
            });
            return rows?.ToList() ?? new List<SupportChatThread>();
        }

        public async Task AssignAsync(Guid chatId, int? adminUserId, int? teamId, string priority, string status, bool updateAssignee = true, bool updateTeam = false)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.SupportChats SET
  AssignedAdminUserId = CASE WHEN @UpdateAssignee=1 THEN @AdminUserId ELSE AssignedAdminUserId END,
  TeamId = CASE WHEN @UpdateTeam=1 THEN @TeamId ELSE TeamId END,
  Priority = ISNULL(@Priority, Priority),
  Status = ISNULL(@Status, Status),
  UpdatedAt = SYSDATETIME(),
  ClosedAt = CASE WHEN @Status=N'Closed' THEN ISNULL(ClosedAt, SYSDATETIME())
                  WHEN @Status IS NOT NULL AND @Status<>N'Closed' THEN NULL ELSE ClosedAt END
WHERE Id=@Id",
                new
                {
                    Id = chatId,
                    AdminUserId = adminUserId,
                    TeamId = teamId,
                    Priority = priority,
                    Status = status,
                    UpdateAssignee = updateAssignee ? 1 : 0,
                    UpdateTeam = updateTeam ? 1 : 0
                });
        }

        public async Task AddTransferAsync(SupportTransferRow row)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
INSERT INTO koch.SupportConversationTransfers
(ChatId, FromAdminUserId, ToAdminUserId, FromTeamId, ToTeamId, Reason, CreatedByUserId)
VALUES (@ChatId, @FromAdminUserId, @ToAdminUserId, @FromTeamId, @ToTeamId, @Reason, @CreatedByUserId)", row);
        }

        public async Task<List<SupportTransferRow>> ListTransfersAsync(Guid chatId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportTransferRow>(@"
SELECT tr.*, 
  LTRIM(RTRIM(ISNULL(f.FirstName,N'')+N' '+ISNULL(f.LastName,N''))) AS FromAdminName,
  LTRIM(RTRIM(ISNULL(t.FirstName,N'')+N' '+ISNULL(t.LastName,N''))) AS ToAdminName,
  LTRIM(RTRIM(ISNULL(c.FirstName,N'')+N' '+ISNULL(c.LastName,N''))) AS CreatedByName
FROM koch.SupportConversationTransfers tr
LEFT JOIN koch.Users f ON f.Id=tr.FromAdminUserId
LEFT JOIN koch.Users t ON t.Id=tr.ToAdminUserId
LEFT JOIN koch.Users c ON c.Id=tr.CreatedByUserId
WHERE tr.ChatId=@ChatId
ORDER BY tr.Id DESC", new { ChatId = chatId });
            return rows?.ToList() ?? new List<SupportTransferRow>();
        }

        public async Task AddNoteAsync(SupportInternalNote note)
        {
            using var conn = _factory.CreateConnection();
            var id = await conn.ExecuteScalarAsync<long>(@"
INSERT INTO koch.SupportInternalNotes (ChatId, AuthorUserId, Body)
OUTPUT INSERTED.Id VALUES (@ChatId, @AuthorUserId, @Body)", note);
            note.Id = id;
        }

        public async Task<List<SupportInternalNote>> ListNotesAsync(Guid chatId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportInternalNote>(@"
SELECT n.*, LTRIM(RTRIM(ISNULL(u.FirstName,N'')+N' '+ISNULL(u.LastName,N''))) AS AuthorName
FROM koch.SupportInternalNotes n
LEFT JOIN koch.Users u ON u.Id=n.AuthorUserId
WHERE n.ChatId=@ChatId
ORDER BY n.Id DESC", new { ChatId = chatId });
            return rows?.ToList() ?? new List<SupportInternalNote>();
        }

        public async Task<List<SupportOperatorOption>> ListOperatorsAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportOperatorOption>(@"
SELECT DISTINCT u.Id AS UserId,
  LTRIM(RTRIM(ISNULL(u.FirstName,N'')+N' '+ISNULL(u.LastName,N''))) AS DisplayName,
  u.PhoneNumber AS Phone
FROM koch.Users u
INNER JOIN koch.UserRoles ur ON ur.UserId=u.Id
INNER JOIN koch.Roles r ON r.Id=ur.RoleId AND r.RoleName=N'Admin'
WHERE ISNULL(u.IsActive,1)=1
  AND (@SiteId IS NULL OR @SiteId=0 OR u.SiteId IS NULL OR u.SiteId=@SiteId)
ORDER BY DisplayName", new { SiteId = siteId });
            return rows?.ToList() ?? new List<SupportOperatorOption>();
        }

        public async Task<List<SupportTeam>> ListTeamsAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportTeam>(@"
SELECT t.*, (SELECT COUNT(1) FROM koch.SupportTeamMembers m WHERE m.TeamId=t.Id) AS MemberCount
FROM koch.SupportTeams t
WHERE (@SiteId IS NULL OR @SiteId=0 OR t.SiteId=@SiteId OR t.SiteId IS NULL)
ORDER BY t.Name", new { SiteId = siteId });
            return rows?.ToList() ?? new List<SupportTeam>();
        }

        public async Task<int> CreateTeamAsync(SupportTeam team)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.SupportTeams (SiteId, Name, Description, IsActive)
OUTPUT INSERTED.Id VALUES (@SiteId, @Name, @Description, @IsActive)", team);
        }

        public async Task UpdateTeamAsync(SupportTeam team)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.SupportTeams SET Name=@Name, Description=@Description, IsActive=@IsActive WHERE Id=@Id", team);
        }

        public async Task SetTeamMembersAsync(int teamId, IEnumerable<int> userIds)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SupportTeamMembers WHERE TeamId=@TeamId", new { TeamId = teamId });
            foreach (var uid in userIds?.Distinct() ?? Enumerable.Empty<int>())
            {
                await conn.ExecuteAsync(
                    "INSERT INTO koch.SupportTeamMembers (TeamId, UserId) VALUES (@TeamId, @UserId)",
                    new { TeamId = teamId, UserId = uid });
            }
        }

        public async Task<List<int>> GetTeamMemberIdsAsync(int teamId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<int>(
                "SELECT UserId FROM koch.SupportTeamMembers WHERE TeamId=@TeamId", new { TeamId = teamId });
            return rows?.ToList() ?? new List<int>();
        }

        public async Task<List<SupportChatThread>> ListPreviousChatsAsync(int? siteId, int? userId, string guestToken, Guid excludeChatId, int take = 10)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportChatThread>(ThreadSelect + @"
WHERE c.Id<>@Exclude
  AND (@SiteId IS NULL OR c.SiteId=@SiteId)
  AND (
        (@UserId IS NOT NULL AND c.UserId=@UserId) OR
        (@GuestToken IS NOT NULL AND @GuestToken<>N'' AND c.GuestToken=@GuestToken)
      )
ORDER BY c.LastMessageAt DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY",
                new { Exclude = excludeChatId, SiteId = siteId, UserId = userId, GuestToken = guestToken, Take = take });
            return rows?.ToList() ?? new List<SupportChatThread>();
        }

        public async Task<string> GetUserEmailAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<string>("SELECT Email FROM koch.Users WHERE Id=@Id", new { Id = userId });
        }

        public async Task<List<SupportBlock>> ListBlocksAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportBlock>(@"
SELECT * FROM koch.SupportBlocks
WHERE (@SiteId IS NULL OR @SiteId=0 OR SiteId=@SiteId OR SiteId IS NULL)
ORDER BY Id DESC", new { SiteId = siteId });
            return rows?.ToList() ?? new List<SupportBlock>();
        }

        public async Task AddBlockAsync(SupportBlock block)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
INSERT INTO koch.SupportBlocks (SiteId, UserId, GuestToken, Phone, IpAddress, Reason, IsActive, CreatedByUserId)
VALUES (@SiteId, @UserId, @GuestToken, @Phone, @IpAddress, @Reason, 1, @CreatedByUserId)", block);
        }

        public async Task SetBlockActiveAsync(int id, bool active)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.SupportBlocks SET IsActive=@A WHERE Id=@Id", new { Id = id, A = active });
        }

        public async Task<bool> IsBlockedAsync(int? siteId, int? userId, string guestToken, string phone)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM koch.SupportBlocks
WHERE IsActive=1
  AND (@SiteId IS NULL OR SiteId IS NULL OR SiteId=@SiteId)
  AND (
        (@UserId IS NOT NULL AND UserId=@UserId) OR
        (@GuestToken IS NOT NULL AND @GuestToken<>N'' AND GuestToken=@GuestToken) OR
        (@Phone IS NOT NULL AND @Phone<>N'' AND Phone=@Phone)
      )", new { SiteId = siteId, UserId = userId, GuestToken = guestToken, Phone = phone });
            return n > 0;
        }

        public async Task<List<SupportBusinessHour>> GetBusinessHoursAsync(int siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportBusinessHour>(
                "SELECT * FROM koch.SupportBusinessHours WHERE SiteId=@SiteId ORDER BY DayOfWeek", new { SiteId = siteId });
            return rows?.ToList() ?? new List<SupportBusinessHour>();
        }

        public async Task SaveBusinessHoursAsync(int siteId, IEnumerable<SupportBusinessHour> hours)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SupportBusinessHours WHERE SiteId=@SiteId", new { SiteId = siteId });
            foreach (var h in hours ?? Enumerable.Empty<SupportBusinessHour>())
            {
                h.SiteId = siteId;
                await conn.ExecuteAsync(@"
INSERT INTO koch.SupportBusinessHours (SiteId, DayOfWeek, IsOpen, OpenTime, CloseTime)
VALUES (@SiteId, @DayOfWeek, @IsOpen, @OpenTime, @CloseTime)", h);
            }
        }

        public async Task<List<SupportHoliday>> ListHolidaysAsync(int siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SupportHoliday>(
                "SELECT * FROM koch.SupportHolidays WHERE SiteId=@SiteId ORDER BY HolidayDate DESC", new { SiteId = siteId });
            return rows?.ToList() ?? new List<SupportHoliday>();
        }

        public async Task AddHolidayAsync(SupportHoliday holiday)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM koch.SupportHolidays WHERE SiteId=@SiteId AND HolidayDate=@HolidayDate)
INSERT INTO koch.SupportHolidays (SiteId, HolidayDate, Title) VALUES (@SiteId, @HolidayDate, @Title)", holiday);
        }

        public async Task DeleteHolidayAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SupportHolidays WHERE Id=@Id", new { Id = id });
        }

        public async Task<bool> IsHolidayAsync(int siteId, DateTime date)
        {
            using var conn = _factory.CreateConnection();
            var n = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM koch.SupportHolidays WHERE SiteId=@SiteId AND HolidayDate=@D",
                new { SiteId = siteId, D = date.Date });
            return n > 0;
        }
    }
}
