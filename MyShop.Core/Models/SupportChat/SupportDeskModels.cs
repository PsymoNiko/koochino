using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.SupportChat
{
    public class SupportTeam
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }

    public class SupportOperatorOption
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Phone { get; set; }
    }

    public class SupportTransferRow
    {
        public long Id { get; set; }
        public Guid ChatId { get; set; }
        public int? FromAdminUserId { get; set; }
        public int? ToAdminUserId { get; set; }
        public int? FromTeamId { get; set; }
        public int? ToTeamId { get; set; }
        public string Reason { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FromAdminName { get; set; }
        public string ToAdminName { get; set; }
        public string CreatedByName { get; set; }
    }

    public class SupportInternalNote
    {
        public long Id { get; set; }
        public Guid ChatId { get; set; }
        public int AuthorUserId { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; }
    }

    public class SupportBlock
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? UserId { get; set; }
        public string GuestToken { get; set; }
        public string Phone { get; set; }
        public string IpAddress { get; set; }
        public string Reason { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupportBusinessHour
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public byte DayOfWeek { get; set; }
        public bool IsOpen { get; set; } = true;
        public TimeSpan OpenTime { get; set; } = TimeSpan.FromHours(9);
        public TimeSpan CloseTime { get; set; } = TimeSpan.FromHours(18);
    }

    public class SupportHoliday
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public DateTime HolidayDate { get; set; }
        public string Title { get; set; }
    }

    public class SupportCustomerProfile
    {
        public Guid ChatId { get; set; }
        public int? UserId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Audience { get; set; }
        public string CurrentPageUrl { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastActivity { get; set; }
        public int PreviousConversationCount { get; set; }
        public List<SupportChatThread> PreviousConversations { get; set; } = new();
        public List<SupportInternalNote> Notes { get; set; } = new();
        public List<SupportTransferRow> Transfers { get; set; } = new();
        public bool IsBlocked { get; set; }
    }

    public class SupportInboxFilter
    {
        public int? SiteId { get; set; }
        public string Status { get; set; }
        public int? AssignedAdminUserId { get; set; }
        public bool OnlyUnassigned { get; set; }
        public bool OnlyMine { get; set; }
        public int? MineUserId { get; set; }
        public int? TeamId { get; set; }
        public string Priority { get; set; }
        public string Search { get; set; }
        public int Take { get; set; } = 80;
    }
}
