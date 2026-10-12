using System;

namespace MyShop.Core.Models.SupportChat
{
    public static class SupportChatStatuses
    {
        public const string Waiting = "Waiting";
        public const string Active = "Active";
        public const string Pending = "Pending";
        public const string Closed = "Closed";
    }

    public static class SupportChatSenders
    {
        public const string User = "User";
        public const string Admin = "Admin";
        public const string System = "System";
        public const string Ai = "Ai";
    }

    public static class SupportMessageTypes
    {
        public const string Text = "Text";
        public const string Image = "Image";
        public const string File = "File";
        public const string SystemEvent = "SystemEvent";
    }

    public static class SupportChannels
    {
        public const string Website = "Website";
        public const string Bale = "Bale";
        public const string Telegram = "Telegram";
    }

    public static class SupportPriorities
    {
        public const string Low = "Low";
        public const string Normal = "Normal";
        public const string High = "High";
        public const string Urgent = "Urgent";
    }

    public class SupportChatThread
    {
        public Guid Id { get; set; }
        public int? SiteId { get; set; }
        public int? UserId { get; set; }
        public string GuestToken { get; set; }
        public string VisitorName { get; set; }
        public string VisitorPhone { get; set; }
        public string Audience { get; set; } = "Guest";
        public string Status { get; set; } = SupportChatStatuses.Waiting;
        public string Priority { get; set; } = SupportPriorities.Normal;
        public string Channel { get; set; } = SupportChannels.Website;
        public int? TeamId { get; set; }
        public string TeamName { get; set; }
        public int? AssignedAdminUserId { get; set; }
        public string AssignedAdminName { get; set; }
        public DateTime LastMessageAt { get; set; }
        public string LastMessagePreview { get; set; }
        public int UnreadByAdmin { get; set; }
        public int UnreadByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string CurrentPageUrl { get; set; }
        public string ExternalThreadId { get; set; }
        public string UserDisplayName { get; set; }
        public string UserPhone { get; set; }
    }

    public class SupportChatMessage
    {
        public long Id { get; set; }
        public Guid ChatId { get; set; }
        public string SenderType { get; set; }
        public int? SenderUserId { get; set; }
        public string Body { get; set; }
        public string MessageType { get; set; } = SupportMessageTypes.Text;
        public string AttachmentUrl { get; set; }
        public string AttachmentName { get; set; }
        public int? AttachmentSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class SupportChatStartRequest
    {
        public int? SiteId { get; set; }
        public int? UserId { get; set; }
        public string GuestToken { get; set; }
        public string VisitorName { get; set; }
        public string VisitorPhone { get; set; }
        public string Audience { get; set; } = "Guest";
        public string CurrentPageUrl { get; set; }
        public string Channel { get; set; } = SupportChannels.Website;
        public string ExternalThreadId { get; set; }
    }

    public class SupportSettings
    {
        public int SiteId { get; set; }
        public bool IsChatEnabled { get; set; } = true;
        public string WelcomeMessage { get; set; } = "سلام! به پشتیبانی خوش آمدید. پیام خود را بنویسید.";
        public string OfflineMessage { get; set; } = "اکنون خارج از ساعات پاسخگویی هستیم. پیام بگذارید؛ به‌زودی پاسخ می‌دهیم.";
        public string WidgetPosition { get; set; } = "bottom-right";
        public string WidgetColor { get; set; } = "#0f766e";
        public int MaxUploadMb { get; set; } = 2;
        public string AllowedExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.webp,.pdf,.doc,.docx,.zip";
        public string TimeZoneId { get; set; } = "Iran Standard Time";
        public bool UseBusinessHours { get; set; }
        public bool WaitAlertEnabled { get; set; } = true;
        public int WaitAlertMinutes { get; set; } = 5;
        public bool SoundNotifyEnabled { get; set; } = true;
        public bool BrowserNotifyEnabled { get; set; } = true;
        public bool CsatEnabled { get; set; } = true;
        public string CsatPrompt { get; set; } = "از پشتیبانی راضی بودید؟ امتیاز دهید.";
        public bool AutoReplyEnabled { get; set; }
        public int AutoReplyMinScore { get; set; } = 40;
        public int AutoReplyMaxPerChat { get; set; } = 2;
        public string AutoReplyHandoffHint { get; set; } = "اگر این پاسخ کافی نبود، همین‌جا بنویسید تا پشتیبان انسانی ادامه دهد.";

        /// <summary>Let the live-chat auto-reply phrase the knowledge article through the LLM instead of pasting an excerpt.</summary>
        public bool AutoReplyUseAi { get; set; }

        public bool AiEnabled { get; set; }

        /// <summary>Below this knowledge-base match score the question is treated as out of scope and the LLM is never called.</summary>
        public int AiMinRetrievalScore { get; set; } = 24;
        public int AiMaxMessagesPerUserPerDay { get; set; } = 40;
        public int AiMaxTokensPerSitePerDay { get; set; } = 300000;
        public int AiMaxInputChars { get; set; } = 600;
        public int AiHistoryTurns { get; set; } = 8;
        public decimal AiTemperature { get; set; } = 0.20m;
        public string AiOutOfScopeMessage { get; set; } =
            "این سوال خارج از حوزه پشتیبانی این سایت است و پاسخی برای آن ندارم. اگر سوال شما درباره خدمات ما است، لطفاً واضح‌تر بپرسید یا با پشتیبانی انسانی گفتگو کنید.";

        /// <summary>When on, retrieval blends keyword score with cosine similarity. Default off so upgrades do not spend on embeddings.</summary>
        public bool AiEmbeddingsEnabled { get; set; }

        /// <summary>When on, closed admin chats may produce knowledge-base drafts. Drafts are never auto-published.</summary>
        public bool AiLearnFromAdminChat { get; set; }

        public bool OmnichannelEnabled { get; set; }
        public bool OmnichannelInterceptFreeText { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SupportChatRealtimeMessage
    {
        public Guid ChatId { get; set; }
        public long Id { get; set; }
        public string SenderType { get; set; }
        public int? SenderUserId { get; set; }
        public string Body { get; set; }
        public string MessageType { get; set; }
        public string AttachmentUrl { get; set; }
        public string AttachmentName { get; set; }
        public int? AttachmentSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
