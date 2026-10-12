using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models;
using MyShop.Core.Models.Messaging;

namespace MyShop.Web.Services
{
    /// <summary>
    /// پس از ثبت هر سفارش مشتری، جزئیات را به شماره‌های ناظر در بله می‌فرستد.
    /// شماره‌ها از پنل ادمین (لینک پیام‌رسان) مدیریت می‌شوند و باید به بات لینک شده باشند.
    /// </summary>
    public class BaleOrderPurchaseNotifier : IOrderPurchaseNotifier
    {
        private readonly IMessagingOtpService _messaging;
        private readonly IStoreService _stores;
        private readonly IOrderNotifyRecipientRepository _recipients;
        private readonly IConfiguration _config;
        private readonly ILogger<BaleOrderPurchaseNotifier> _logger;

        public BaleOrderPurchaseNotifier(
            IMessagingOtpService messaging,
            IStoreService stores,
            IOrderNotifyRecipientRepository recipients,
            IConfiguration config,
            ILogger<BaleOrderPurchaseNotifier> logger)
        {
            _messaging = messaging;
            _stores = stores;
            _recipients = recipients;
            _config = config;
            _logger = logger;
        }

        public async Task NotifyNewOrderAsync(Order order)
        {
            if (order == null) return;

            try
            {
                var phones = await ResolvePhonesAsync();
                if (phones.Count == 0)
                {
                    _logger.LogWarning("Bale order notify: no recipient phones configured");
                    return;
                }

                var storeName = order.StoreName;
                if (string.IsNullOrWhiteSpace(storeName) && order.StoreId.HasValue && order.StoreId > 0)
                {
                    try
                    {
                        var store = await _stores.GetByIdAsync(order.StoreId.Value);
                        storeName = store?.StoreName;
                    }
                    catch { /* ignore */ }
                }
                storeName ??= "—";

                var when = order.OrderDate != default ? order.OrderDate : DateTime.Now;
                var text =
                    "<b>🛒 خرید جدید در کوچینو</b>\n\n" +
                    $"📄 شماره فاکتور: <code>{Escape(order.OrderNumber ?? "#" + order.Id)}</code>\n" +
                    $"🏪 فروشگاه: {Escape(storeName)}\n" +
                    $"🕐 زمان: {ToPersianDateTime(when)}\n" +
                    $"💰 مبلغ کل: {order.TotalAmount:N0} تومان\n" +
                    $"🏷 تخفیف: {order.DiscountAmount:N0} تومان\n" +
                    $"💵 مبلغ نهایی: {order.FinalAmount:N0} تومان\n" +
                    $"📦 نوع سفارش: {OrderTypeTitle(order.OrderType)}";

                foreach (var phone in phones)
                {
                    try
                    {
                        var result = await _messaging.SendMessageAsync(phone, text, MessagingProviders.Bale);
                        if (!result.Success)
                            _logger.LogWarning("Bale order notify failed for {Phone}: {Msg}", phone, result.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bale order notify failed for {Phone}", phone);
                    }
                }

                // تلگرام (اگر توکن و لینک کاربر موجود باشد)
                try
                {
                    var tgPhones = await _recipients.ListActivePhonesAsync(MessagingProviders.Telegram);
                    if (tgPhones == null || tgPhones.Count == 0)
                        tgPhones = phones;
                    var tgText = text.Replace("<b>", "").Replace("</b>", "").Replace("<code>", "").Replace("</code>", "");
                    foreach (var phone in tgPhones.Distinct())
                    {
                        try
                        {
                            var result = await _messaging.SendMessageAsync(phone, tgText, MessagingProviders.Telegram);
                            if (!result.Success)
                                _logger.LogWarning("Telegram order notify failed for {Phone}: {Msg}", phone, result.Message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Telegram order notify failed for {Phone}", phone);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Telegram order notify skipped");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bale order notify exception");
            }
        }

        private async Task<List<string>> ResolvePhonesAsync()
        {
            try
            {
                var fromDb = await _recipients.ListActivePhonesAsync(MessagingProviders.Bale);
                if (fromDb != null && fromDb.Count > 0)
                    return fromDb;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load OrderNotifyRecipients; falling back to config");
            }

            var fallback = _config["Bale:OrderNotifyPhone"];
            if (string.IsNullOrWhiteSpace(fallback))
                return new List<string>();

            return fallback
                .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length >= 10)
                .Distinct()
                .ToList();
        }

        private static string OrderTypeTitle(byte type) => type switch
        {
            0 => "عادی",
            1 => "با کوپن",
            2 => "جعبه‌شانس",
            _ => "نامشخص"
        };

        private static string ToPersianDateTime(DateTime dt)
        {
            var pc = new PersianCalendar();
            return $"{pc.GetYear(dt):0000}/{pc.GetMonth(dt):00}/{pc.GetDayOfMonth(dt):00} {dt:HH:mm}";
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
