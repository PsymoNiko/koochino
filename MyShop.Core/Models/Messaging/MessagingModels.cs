using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Messaging
{
    public static class SmsProviders
    {
        public const string SmsIr = "SmsIr";
        public const string MeliPayamak = "MeliPayamak";

        public static readonly IReadOnlyList<(string Value, string Label)> All = new[]
        {
            (SmsIr, "sms.ir"),
            (MeliPayamak, "ملی پیامک (MeliPayamak)")
        };

        public static string DisplayName(string provider) =>
            provider switch
            {
                SmsIr => "sms.ir",
                MeliPayamak => "ملی پیامک (MeliPayamak)",
                _ => string.IsNullOrWhiteSpace(provider) ? "sms.ir" : provider
            };
    }

    public static class MessagingProviders
    {
        public const string Bale = "Bale";
        public const string Eita = "Eita";
        public const string Telegram = "Telegram";
        public const string Sms = "Sms";
    }

    public static class MessagingPurposes
    {
        public const string Register = "Register";
        public const string OrderConfirm = "OrderConfirm";
        public const string Generic = "Generic";
        public const string IdentityVerification = "IdentityVerification";
    }

    public class UserMessagingLink
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string PhoneNumber { get; set; }
        public int? UserId { get; set; }
        public string ExternalUserId { get; set; }
        public string ChatId { get; set; }
        public string Username { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>شماره‌هایی که اعلان خرید جدید در بله برایشان ارسال می‌شود.</summary>
    public class OrderNotifyRecipient
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class MessagingOtp
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string PhoneNumber { get; set; }
        public string Purpose { get; set; }
        public string CodeHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public int AttemptCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }

    public class MessagingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public static MessagingResult Ok(string message = null) => new() { Success = true, Message = message };
        public static MessagingResult Fail(string message) => new() { Success = false, Message = message };
    }

    public static class MessagingMessageTypes
    {
        public const string Otp = "Otp";
        public const string Text = "Text";
        public const string BotReply = "BotReply";
        public const string Test = "Test";
        public const string PasswordReset = "PasswordReset";
        public const string Manual = "Manual";
        public const string Template = "Template";
        public const string Academy = "Academy";
    }

    public class MessagingSendLog
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string PhoneNumber { get; set; }
        public string ChatId { get; set; }
        public string MessageType { get; set; }
        public string MessageText { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string LineNumber { get; set; }
        public string ExternalId { get; set; }
        public byte? DeliveryState { get; set; }
        public string DeliveryStateText { get; set; }
        public DateTime? DeliveryCheckedAt { get; set; }
        public string TemplateCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SmsTemplate
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public string BodyTemplate { get; set; } = "";
        public int? SmsIrTemplateId { get; set; }
        /// <summary>شناسه بادی (کد متن پترن) پنل ملی‌پیامک — اگر ست شود و خط پیش‌فرض ملی‌پیامک باشد، از SendByBaseNumber2 استفاده می‌شود.</summary>
        public int? MeliPayamakBodyId { get; set; }
        public string ParameterNames { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public bool UseForSystem { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool UsesVerifyApi => SmsIrTemplateId.HasValue && SmsIrTemplateId.Value > 0;
        public bool UsesMeliSharedApi => MeliPayamakBodyId.HasValue && MeliPayamakBodyId.Value > 0;
    }

    public class SmsSendLogFilter
    {
        public string Provider { get; set; } = "SmsIr";
        public string Phone { get; set; }
        public string MessageType { get; set; }
        public bool? Success { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Top { get; set; } = 200;
    }

    public class SmsLine
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string LineNumber { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public string Provider { get; set; } = SmsProviders.SmsIr;
        /// <summary>نام کاربری پنل ملی‌پیامک (برای SendByBaseNumber2 / BaseServiceNumber).</summary>
        public string Username { get; set; }
        public string ApiKey { get; set; }
        public string SandboxApiKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool HasUsername => !string.IsNullOrWhiteSpace(Username);
        public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
        public bool HasSandboxApiKey => !string.IsNullOrWhiteSpace(SandboxApiKey);

        public string ProviderDisplayName => SmsProviders.DisplayName(Provider);

        public string MaskedApiKey => MaskSecret(ApiKey);
        public string MaskedSandboxApiKey => MaskSecret(SandboxApiKey);

        public static string MaskSecret(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "تنظیم نشده";
            var v = value.Trim();
            if (v.Length <= 8) return "****";
            return v.Substring(0, 4) + "…" + v.Substring(v.Length - 4);
        }
    }

    public class SmsProviderSettings
    {
        public int Id { get; set; }
        public string Provider { get; set; } = "SmsIr";
        public bool UseSandbox { get; set; } = false;
        public DateTime UpdatedAt { get; set; }
    }

    public class SmsDeliveryReport
    {
        public int MessageId { get; set; }
        public string Mobile { get; set; }
        public string MessageText { get; set; }
        public long? SendDateTime { get; set; }
        public long LineNumber { get; set; }
        public decimal Cost { get; set; }
        public byte? DeliveryState { get; set; }
        public long? DeliveryDateTime { get; set; }

        public string DeliveryStateText => DeliveryState switch
        {
            1 => "رسیده به گوشی",
            2 => "نرسیده به گوشی",
            3 => "پردازش در مخابرات",
            4 => "نرسیده به مخابرات",
            5 => "رسیده به مخابرات",
            6 => "خطا",
            7 => "لیست سیاه",
            8 => "رسیده به گوشی",
            16 => "نرسیده",
            0 => "ارسال شده",
            _ => DeliveryState.HasValue ? $"نامشخص ({DeliveryState})" : "در انتظار"
        };
    }

    public class SmsBulkSendRequest
    {
        public List<string> Phones { get; set; } = new();
        public string MessageText { get; set; } = "";
        public int? LineId { get; set; }
        public string MessageType { get; set; }
        public string TemplateCode { get; set; }
    }

    /// <summary>انواع اکشن دکمه منوی بات</summary>
    public static class BotMenuActionTypes
    {
        public const string SendOtp = "SendOtp";       // ارسال کد فعال‌سازی
        public const string AskPhone = "AskPhone";     // درخواست شماره (متنی)
        public const string ShareContact = "ShareContact"; // دکمه اشتراک مخاطب (request_contact)
        public const string ReplyText = "ReplyText";   // پاسخ متنی ثابت (ActionPayload)
        public const string Feedback = "Feedback";     // بازخورد (ActionPayload = good|bad ؛ پیام تشکر از تنظیمات)
    }

    public class BotMenuItem
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string Title { get; set; }
        public string ActionType { get; set; }
        public string ActionPayload { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
