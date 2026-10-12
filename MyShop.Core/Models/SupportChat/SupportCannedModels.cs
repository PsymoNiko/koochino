using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.SupportChat
{
    public static class SupportNotifyKinds
    {
        public const string NewMessage = "NewMessage";
        public const string WaitAlert = "WaitAlert";
        public const string Assigned = "Assigned";
        public const string Transfer = "Transfer";
    }

    public class SupportCannedReply
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Title { get; set; }
        public string Shortcut { get; set; }
        public string Body { get; set; }
        public string Category { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SupportOperatorNotification
    {
        public long Id { get; set; }
        public int? SiteId { get; set; }
        public int? UserId { get; set; }
        public Guid? ChatId { get; set; }
        public string Kind { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupportWaitAlertItem
    {
        public Guid ChatId { get; set; }
        public int? SiteId { get; set; }
        public string DisplayName { get; set; }
        public int WaitMinutes { get; set; }
        public DateTime LastMessageAt { get; set; }
    }
}
