using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Messaging
{
    public interface IUserMessagingLinkRepository
    {
        Task<int> UpsertAsync(UserMessagingLink link);
        Task<UserMessagingLink> GetByPhoneAsync(string provider, string phoneNumber);
        Task<UserMessagingLink> GetByChatAsync(string provider, string chatId);
        Task<List<UserMessagingLink>> ListAsync(string provider = null);
    }

    public interface IMessagingOtpRepository
    {
        Task<int> InsertAsync(MessagingOtp otp);
        Task<MessagingOtp> GetActiveAsync(string provider, string phoneNumber, string purpose);
        Task<bool> MarkUsedAsync(int id);
        Task IncrementAttemptAsync(int id);
        Task<int> CountRecentAsync(string provider, string phoneNumber, int minutes);
    }

    /// <summary>کانال ارسال (بله / ایتا / تلگرام)</summary>
    public interface IMessagingChannel
    {
        string Provider { get; }
        Task<(bool Ok, string Error)> SendTextAsync(string chatId, string text);
    }

    public interface IMessagingOtpService
    {
        /// <summary>ارسال کد OTP به چت لینک‌شده کاربر</summary>
        Task<MessagingResult> SendOtpAsync(string phoneNumber, string purpose, string provider = null);

        /// <summary>اعتبارسنجی کد</summary>
        Task<MessagingResult> VerifyOtpAsync(string phoneNumber, string purpose, string code, string provider = null);

        /// <summary>ارسال پیام آزاد به کاربر لینک‌شده (تایید سفارش و ...)</summary>
        Task<MessagingResult> SendMessageAsync(string phoneNumber, string text, string provider = null);
    }

    public interface IMessagingSendLogRepository
    {
        Task<int> InsertAsync(MessagingSendLog log);
        Task<List<MessagingSendLog>> ListAsync(string provider = null, int top = 200);
        Task<List<MessagingSendLog>> SearchAsync(SmsSendLogFilter filter);
        Task UpdateDeliveryAsync(int id, byte? deliveryState, string deliveryStateText);
    }

    public interface ISmsTemplateRepository
    {
        Task<List<SmsTemplate>> ListAsync(bool activeOnly = false);
        Task<SmsTemplate> GetByCodeAsync(string code);
        Task<int> UpsertAsync(SmsTemplate template);
        Task DeleteAsync(int id);
    }

    public interface IBotMenuItemRepository
    {
        Task<List<BotMenuItem>> ListAsync(string provider = null, bool onlyActive = false);
        Task<BotMenuItem> GetByIdAsync(int id);
        Task<int> UpsertAsync(BotMenuItem item);
        Task DeleteAsync(int id);
    }

    public interface IOrderNotifyRecipientRepository
    {
        Task<List<OrderNotifyRecipient>> ListAsync(string provider = null);
        Task<List<string>> ListActivePhonesAsync(string provider = null);
        Task<int> AddAsync(string phoneNumber, string provider = null);
        Task SetActiveAsync(int id, bool isActive);
        Task DeleteAsync(int id);
    }
}
