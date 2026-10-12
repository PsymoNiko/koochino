using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.SocialMedia
{
    public static class SocialProviderKeys
    {
        public const string Telegram = "Telegram";
        public const string Bale = "Bale";
        public const string Instagram = "Instagram";
    }

    public static class SocialAccountTypes
    {
        public const string Bot = "Bot";
        public const string Channel = "Channel";
        public const string Page = "Page";
        public const string User = "User";
    }

    [Flags]
    public enum SocialCapabilities
    {
        None = 0,
        SendText = 1,
        SendMedia = 2,
        PublishPost = 4,
        ReceiveWebhook = 8,
        Inbox = 16,
        ManageComments = 32,
        SchedulePublish = 64
    }

    public static class SocialPublishJobStatuses
    {
        public const string Draft = "Draft";
        public const string Scheduled = "Scheduled";
        public const string Queued = "Queued";
        public const string Sent = "Sent";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
    }

    public static class SocialPostStatuses
    {
        public const string Draft = "Draft";
        public const string Scheduled = "Scheduled";
        public const string Published = "Published";
        public const string Failed = "Failed";
    }

    public class SocialAccount
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string Provider { get; set; } = "";
        public string AccountType { get; set; } = SocialAccountTypes.Bot;
        public string ExternalId { get; set; }
        public string DisplayName { get; set; } = "";
        public string Username { get; set; }
        public bool IsActive { get; set; } = true;
        public int? LinkedSiteBotId { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // join / bridge
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
        public bool IsLinkedFromSiteBot { get; set; }
    }

    public class SocialCredential
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccessTokenProtected { get; set; }
        public string RefreshTokenProtected { get; set; }
        public string ApiBaseUrl { get; set; }
        public string WebhookSecretProtected { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SocialTarget
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? SiteId { get; set; }
        public string Provider { get; set; } = "";
        public string Title { get; set; } = "";
        public string Username { get; set; }
        public string ChatId { get; set; }
        public string ExternalTargetId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? LinkedOutboundChannelId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

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

    public class SocialConversation
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Provider { get; set; } = "";
        public string ExternalThreadId { get; set; } = "";
        public string ParticipantName { get; set; }
        public string ParticipantExternalId { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SocialInboundMessage
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Provider { get; set; } = "";
        public string ExternalMessageId { get; set; }
        public string Direction { get; set; } = "In";
        public string MessageType { get; set; } = "Text";
        public string BodyText { get; set; }
        public string MediaUrl { get; set; }
        public string RawPayloadJson { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    public class SocialPost
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? AccountId { get; set; }
        public string Provider { get; set; } = "";
        public string Caption { get; set; }
        public string Status { get; set; } = SocialPostStatuses.Draft;
        public string ExternalPostId { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SocialMediaAsset> Assets { get; set; } = new();
    }

    public class SocialMediaAsset
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string AssetType { get; set; } = "Image";
        public string LocalPath { get; set; }
        public string PublicUrl { get; set; }
        public string MimeType { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SocialComment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Provider { get; set; } = "";
        public string ExternalCommentId { get; set; }
        public string AuthorName { get; set; }
        public string BodyText { get; set; }
        public string ParentExternalId { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    public class SocialPublishJob
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? AccountId { get; set; }
        public int? TargetId { get; set; }
        public int? PostId { get; set; }
        public string Provider { get; set; } = "";
        public string Caption { get; set; }
        public string PhotoUrl { get; set; }
        public string Status { get; set; } = SocialPublishJobStatuses.Scheduled;
        public DateTime ScheduledAt { get; set; }
        public int AttemptCount { get; set; }
        public string LastError { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SocialDeliveryStatus
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int? TargetId { get; set; }
        public string Provider { get; set; } = "";
        public bool Success { get; set; }
        public string ExternalId { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    public class SocialOperationLog
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Provider { get; set; }
        public int? AccountId { get; set; }
        public string Operation { get; set; } = "";
        public bool Success { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SocialPublishRequest
    {
        public int? SiteId { get; set; }
        public int? AccountId { get; set; }
        public int? TargetId { get; set; }
        public string Provider { get; set; } = "";
        public string Caption { get; set; } = "";
        public string PhotoUrl { get; set; }
        public DateTime? ScheduleAtUtc { get; set; }
    }

    public class SocialResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ExternalId { get; set; }

        public static SocialResult Ok(string message = null, string externalId = null) =>
            new() { Success = true, Message = message, ExternalId = externalId };

        public static SocialResult Fail(string message) =>
            new() { Success = false, Message = message };
    }

    public class SocialWebhookIntakeResult
    {
        public bool Accepted { get; set; }
        public bool ForwardToLegacyBotProcessor { get; set; }
        public string Message { get; set; }
        public string ChallengeResponse { get; set; }
    }
}
