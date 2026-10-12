using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Ai;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Ai
{
    public class LoyaltySkill : IAiSkill
    {
        private readonly IPointService _points;
        public LoyaltySkill(IPointService points) => _points = points;
        public string Intent => "Loyalty";
        public int Priority => AiSkillPriorities.PersonalData;

        public bool CanHandle(string text) =>
            AiKeywordHelper.Has(text, "امتیاز", "point", "پوینت") &&
            !AiKeywordHelper.IsDefinitionQuery(text) &&
            (AiKeywordHelper.IsPersonalQuery(text) || AiKeywordHelper.Has(text, "موجود", "کل ", "در انتظار"));

        public async Task<string> HandleAsync(string message, AiChatContext context)
        {
            if (context?.UserId == null || context.UserId <= 0)
                return "برای مشاهده امتیازهای خود ابتدا وارد حساب کاربری شوید. درباره «امتیاز چیست» هم می‌توانید بپرسید.";
            try
            {
                var s = await _points.GetUserPointsSummaryAsync(context.UserId.Value);
                if (s == null) return "هنوز امتیاز ثبت‌شده‌ای ندارید. از بخش مأموریت‌ها امتیاز بگیرید: /customer/missions";
                return $"امتیاز شما: موجود {s.AvailablePoints:N0} | کل {s.TotalPoints:N0} | در انتظار {s.PendingPoints:N0}. جزئیات: /customer/points";
            }
            catch
            {
                return "خلاصه امتیاز از بخش «امتیازهای من» (/customer/points) قابل مشاهده است.";
            }
        }
    }

    public class MissionSkill : IAiSkill
    {
        private readonly IPointService _points;
        public MissionSkill(IPointService points) => _points = points;
        public string Intent => "Mission";
        public int Priority => AiSkillPriorities.PersonalData;

        public bool CanHandle(string text) =>
            AiKeywordHelper.Has(text, "ماموریت", "مأموریت", "mission", "چالش") &&
            (AiKeywordHelper.Has(text, "فعال", "لیست", "انجام", "موجود") ||
             AiKeywordHelper.IsPersonalQuery(text) ||
             !AiKeywordHelper.IsDefinitionQuery(text));

        public async Task<string> HandleAsync(string message, AiChatContext context)
        {
            try
            {
                var list = await _points.GetMissionsAsync(activeOnly: true) ?? new();
                if (list.Count == 0)
                    return "فعلاً مأموریت فعالی در سیستم ثبت نشده. بعداً دوباره بررسی کنید یا از /customer/missions ببینید.";
                var top = list.Take(5).Select(m => $"• {m.Title} (+{m.Points} امتیاز)");
                return "مأموریت‌های فعال در سیستم:\n" + string.Join("\n", top) + "\nمشاهده همه: /customer/missions";
            }
            catch
            {
                return "لیست مأموریت‌های فعال را از /customer/missions ببینید.";
            }
        }
    }

    public class CouponSkill : IAiSkill
    {
        private readonly ICouponService _coupons;
        public CouponSkill(ICouponService coupons) => _coupons = coupons;
        public string Intent => "Coupon";
        public int Priority => AiSkillPriorities.PersonalData;

        public bool CanHandle(string text) =>
            AiKeywordHelper.Has(text, "کوپن", "coupon", "gift", "هدایا") ||
            (AiKeywordHelper.Has(text, "تخفیف", "کد تخفیف") && AiKeywordHelper.IsPersonalQuery(text));

        public async Task<string> HandleAsync(string message, AiChatContext context)
        {
            if (context?.UserId == null || context.UserId <= 0)
                return "برای دیدن کد تخفیف‌های خود وارد شوید. درباره «کد تخفیف چیست» هم می‌توانید بپرسید.";
            try
            {
                var mine = await _coupons.GetByUserIdAsync(context.UserId.Value) ?? new();
                if (mine.Count == 0)
                    return "کد تخفیف فعالی برای شما ثبت نشده. پس از خرید و ثبت در کوچینو، طبق شرایط فروشگاه ممکن است کد تخفیف دریافت کنید.";
                var lines = mine.Take(5).Select(c => $"• {c.CoponCode} - {c.CoponTitle}");
                return "کد تخفیف‌های شما:\n" + string.Join("\n", lines);
            }
            catch
            {
                return "کد تخفیف‌ها را از بخش هدایا /customer/gifts ببینید.";
            }
        }
    }
}
