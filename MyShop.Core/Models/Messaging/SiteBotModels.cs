using System;

namespace MyShop.Core.Models.Messaging
{
    /// <summary>اعتبارنامه یک بات برای یک سایت (بله / تلگرام / ...).</summary>
    public class SiteBot
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string Provider { get; set; } = "";
        public string DisplayName { get; set; }
        public string BotToken { get; set; } = "";
        public string ApiBaseUrl { get; set; }
        public string ChatId { get; set; }
        public string BotLink { get; set; }
        public string WebhookSecret { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // join
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
    }

    /// <summary>اعتبارنامه resolve‌شده برای ارسال/وب‌هوک.</summary>
    public class BotCredentials
    {
        public int SiteId { get; set; }
        public string SiteCode { get; set; } = "";
        public string Provider { get; set; } = "";
        public string BotToken { get; set; } = "";
        public string ApiBaseUrl { get; set; } = "";
        public string ChatId { get; set; }
        public string BotLink { get; set; }
        public string WebhookSecret { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }

        public bool HasToken => !string.IsNullOrWhiteSpace(BotToken);
    }

    /// <summary>کانال خروجی برای ارسال پیام گروهی/کانال (بله یا تلگرام).</summary>
    public class MessagingOutboundChannel
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Provider { get; set; } = "";
        public string Title { get; set; } = "";
        /// <summary>یوزرنیم کانال بدون @ (مثلاً hoseinthings)</summary>
        public string Username { get; set; } = "";
        /// <summary>شناسه ارسال: معمولاً @username یا عدد منفی کانال</summary>
        public string ChatId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }

        public string EffectiveChatId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ChatId)) return ChatId.Trim();
                var u = (Username ?? "").Trim().TrimStart('@');
                return string.IsNullOrEmpty(u) ? "" : "@" + u;
            }
        }
    }
}
