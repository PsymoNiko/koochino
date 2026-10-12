using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Lucky;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Lucky;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Lucky
{
    public class LuckyLeagueService : ILuckyLeagueService
    {
        private readonly ILuckyLeagueRepository _repo;
        private readonly IPointService _points;
        private readonly ICouponService _coupons;
        private readonly IUserService _users;
        private readonly ICurrentTenant _tenant;

        private static readonly Random _rng = new();

        public LuckyLeagueService(
            ILuckyLeagueRepository repo,
            IPointService points,
            ICouponService coupons,
            IUserService users,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _points = points;
            _coupons = coupons;
            _users = users;
            _tenant = tenant;
        }

        // ========== ادمین — لیگ‌ها ==========

        public Task<List<LuckyLeague>> GetAllLeaguesAsync(bool activeOnly = false) => _repo.GetAllLeaguesAsync(activeOnly);
        public Task<LuckyLeague> GetLeagueByIdAsync(int id) => _repo.GetLeagueByIdAsync(id);

        public async Task<(bool Success, string Message)> CreateLeagueAsync(LuckyLeague league)
        {
            if (string.IsNullOrWhiteSpace(league.Name)) return (false, "نام لیگ الزامی است");
            if (league.PointsPerBox <= 0) return (false, "هزینهٔ هر جعبه باید بیشتر از صفر باشد");
            if (league.TotalBoxesPerRound <= 0) return (false, "تعداد جعبهٔ هر دور باید بیشتر از صفر باشد");
            if (league.LosingBoxesPerRound < 0 || league.LosingBoxesPerRound > league.TotalBoxesPerRound)
                return (false, "تعداد پوچ نمی‌تواند از تعداد کل جعبه بیشتر باشد");

            var id = await _repo.CreateLeagueAsync(league);
            return id > 0 ? (true, "لیگ ایجاد شد") : (false, "خطا در ایجاد لیگ");
        }

        public async Task<(bool Success, string Message)> UpdateLeagueAsync(LuckyLeague league)
        {
            if (string.IsNullOrWhiteSpace(league.Name)) return (false, "نام لیگ الزامی است");
            if (league.LosingBoxesPerRound < 0 || league.LosingBoxesPerRound > league.TotalBoxesPerRound)
                return (false, "تعداد پوچ نمی‌تواند از تعداد کل جعبه بیشتر باشد");

            var ok = await _repo.UpdateLeagueAsync(league);
            return ok ? (true, "لیگ به‌روزرسانی شد") : (false, "خطا در به‌روزرسانی لیگ");
        }

        public async Task<(bool Success, string Message)> ToggleLeagueStatusAsync(int id, bool isActive)
        {
            var ok = await _repo.ToggleLeagueStatusAsync(id, isActive);
            return ok ? (true, "وضعیت لیگ تغییر کرد") : (false, "خطا در تغییر وضعیت");
        }

        // ========== ادمین — جوایز ==========

        public Task<List<LuckyPrize>> GetPrizesByLeagueIdAsync(int leagueId, bool includeInactive = false)
            => _repo.GetPrizesByLeagueIdAsync(leagueId, includeInactive);

        public Task<List<LuckyPrize>> GetPendingPrizeQueueAsync() => _repo.GetPendingPrizeQueueAsync();

        public async Task<(bool Success, string Message)> CreateAdminPrizeAsync(LuckyPrize prize, int adminUserId)
        {
            var (ok, msg) = ValidatePrize(prize);
            if (!ok) return (false, msg);

            prize.CreatedByUserId = adminUserId;
            prize.CreatedByRole = "Admin";
            prize.Status = 1; // Approved — ادمین نیاز به تایید ندارد
            prize.ApprovedByUserId = adminUserId;
            prize.ApprovedAt = DateTime.Now;

            var id = await _repo.CreatePrizeAsync(prize);
            return id > 0 ? (true, "جایزه ایجاد شد") : (false, "خطا در ایجاد جایزه");
        }

        public async Task<(bool Success, string Message)> UpdatePrizeAsync(LuckyPrize prize)
        {
            var (ok, msg) = ValidatePrize(prize);
            if (!ok) return (false, msg);

            var result = await _repo.UpdatePrizeAsync(prize);
            return result ? (true, "جایزه به‌روزرسانی شد") : (false, "خطا در به‌روزرسانی جایزه");
        }

        public async Task<(bool Success, string Message)> DeletePrizeAsync(int id)
        {
            var result = await _repo.DeletePrizeAsync(id);
            if (result == -2) return (false, "این جایزه قبلاً در یک دور استفاده شده و قابل حذف نیست؛ می‌توانید غیرفعالش کنید.");
            return result > 0 ? (true, "جایزه حذف شد") : (false, "خطا در حذف جایزه");
        }

        public async Task<(bool Success, string Message)> TogglePrizeStatusAsync(int id, bool isActive)
        {
            var ok = await _repo.TogglePrizeStatusAsync(id, isActive);
            return ok ? (true, "وضعیت جایزه تغییر کرد") : (false, "خطا در تغییر وضعیت");
        }

        public async Task<(bool Success, string Message)> ApprovePrizeAsync(int id, bool approve, int adminUserId, string note)
        {
            var ok = await _repo.ApprovePrizeAsync(id, approve, adminUserId, note);
            if (!ok) return (false, "این جایزه قبلاً بررسی شده است");
            return (true, approve ? "جایزه تایید شد" : "جایزه رد شد");
        }

        private static (bool Ok, string Message) ValidatePrize(LuckyPrize prize)
        {
            if (string.IsNullOrWhiteSpace(prize.Title)) return (false, "عنوان جایزه الزامی است");
            if (prize.PrizeType != "Points" && prize.PrizeType != "DiscountCode" && prize.PrizeType != "ExtraChance")
                return (false, "نوع جایزه نامعتبر است");
            if (prize.PrizeType == "Points" && (!prize.PointsValue.HasValue || prize.PointsValue.Value <= 0))
                return (false, "مقدار امتیاز جایزه باید بیشتر از صفر باشد");
            if (prize.PrizeType == "DiscountCode" && (!prize.DiscountValue.HasValue || prize.DiscountValue.Value <= 0))
                return (false, "مقدار تخفیف باید بیشتر از صفر باشد");
            if (prize.Weight <= 0) return (false, "وزن جایزه باید بیشتر از صفر باشد");
            return (true, "");
        }

        // ========== فروشگاه‌دار ==========

        public Task<List<LuckyPrize>> GetMyProposedPrizesAsync(int storeOwnerUserId) => _repo.GetPrizesByCreatorAsync(storeOwnerUserId);

        public async Task<(bool Success, string Message)> ProposeStorePrizeAsync(LuckyPrize prize, int storeOwnerUserId, int storeId)
        {
            if (storeId <= 0) return (false, "فروشگاه را انتخاب کنید");
            prize.PrizeType = "DiscountCode"; // فروشگاه‌دار فقط می‌تواند کد تخفیف پیشنهاد بدهد
            prize.Scope = 1; // مخصوص فروشگاه خودش
            prize.StoreId = storeId;

            var (ok, msg) = ValidatePrize(prize);
            if (!ok) return (false, msg);

            prize.CreatedByUserId = storeOwnerUserId;
            prize.CreatedByRole = "StoreOwner";
            prize.Status = 0; // در انتظار تایید ادمین
            prize.IsActive = false; // تا زمان تایید در قرعه‌کشی شرکت نمی‌کند

            var id = await _repo.CreatePrizeAsync(prize);
            return id > 0
                ? (true, "پیشنهاد جایزه ثبت شد و پس از تایید ادمین وارد قرعه‌کشی می‌شود")
                : (false, "خطا در ثبت پیشنهاد");
        }

        // ========== مشتری ==========

        public async Task<List<LuckyLeagueViewDto>> GetEligibleLeaguesAsync(int userId)
        {
            var summary = await _points.GetUserPointsSummaryAsync(userId);
            var siteId = _tenant?.HasSite == true ? (int?)_tenant.SiteId : null;
            return await _repo.GetEligibleLeaguesForUserAsync(userId, summary.AvailablePoints, siteId);
        }

        public async Task<List<LuckyBox>> GetOrStartRoundAsync(int userId, int leagueId)
        {
            var league = await _repo.GetLeagueByIdAsync(leagueId);
            if (league == null || !league.IsActive) return new List<LuckyBox>();

            var summary = await _points.GetUserPointsSummaryAsync(userId);
            var unlocked = summary.AvailablePoints >= league.MinPoints &&
                           (!league.MaxPoints.HasValue || summary.AvailablePoints <= league.MaxPoints.Value);
            if (!unlocked) return new List<LuckyBox>();

            var boxes = await _repo.GetActiveRoundBoxesAsync(userId, leagueId);
            if (boxes.Count == 0 || boxes.All(b => b.IsOpened))
            {
                await _repo.CreateRoundAsync(userId, leagueId);
                boxes = await _repo.GetActiveRoundBoxesAsync(userId, leagueId);
            }
            return boxes;
        }

        public async Task<LuckyBoxOpenResultDto> OpenBoxAsync(int userId, int leagueId, int roundId, int boxId, string paymentMethod)
        {
            if (!await _users.IsPhoneVerifiedAsync(userId))
                return new LuckyBoxOpenResultDto { Success = false, Message = "برای باز کردن جعبه ابتدا شماره تلفن خود را فعال کنید." };

            paymentMethod = paymentMethod == "Chance" ? "Chance" : "Points";

            var (result, isLosing, prizeType, prizeTitle, pointsCredited, prizeId, pointsSpent) =
                await _repo.OpenBoxAsync(boxId, userId, paymentMethod);

            if (result == -1) return new LuckyBoxOpenResultDto { Success = false, Message = "این جعبه یافت نشد یا قبلاً باز شده است." };
            if (result == -2) return new LuckyBoxOpenResultDto { Success = false, Message = "امتیاز شما برای باز کردن این جعبه کافی نیست." };
            if (result == -3) return new LuckyBoxOpenResultDto { Success = false, Message = "شانس ذخیره‌شدهٔ شما در این لیگ کافی نیست." };
            if (result != 1) return new LuckyBoxOpenResultDto { Success = false, Message = "خطا در باز کردن جعبه." };

            var dto = new LuckyBoxOpenResultDto
            {
                Success = true,
                IsLosing = isLosing,
                PrizeType = isLosing ? null : prizeType,
                PrizeTitle = isLosing ? null : prizeTitle,
                PointsCredited = pointsCredited
            };

            string overrideMessage = null;
            if (!isLosing && prizeType == "DiscountCode" && prizeId.HasValue)
            {
                var couponCode = await CreateCouponForPrizeAsync(prizeId.Value, userId, boxId);
                if (couponCode == null)
                {
                    // کیف‌پول فروشگاه کافی نبود یا کد یکتا ساخته نشد؛ هزینهٔ جعبه به کاربر برگردانده می‌شود
                    await _repo.RefundBoxCostAsync(boxId, userId, leagueId, paymentMethod, pointsSpent);
                    dto.PrizeType = null;
                    dto.PrizeTitle = null;
                    dto.IsLosing = true;
                    overrideMessage = "متاسفانه این جایزه در حال حاضر در دسترس نبود؛ هزینهٔ جعبه به شما بازگردانده شد.";
                }
                dto.CouponCode = couponCode;
            }

            var afterSummary = await _points.GetUserPointsSummaryAsync(userId);
            dto.RemainingAvailablePoints = afterSummary.AvailablePoints;

            var chanceLeagues = await _repo.GetEligibleLeaguesForUserAsync(userId, afterSummary.AvailablePoints, null);
            dto.RemainingChance = chanceLeagues.FirstOrDefault(l => l.Id == leagueId)?.ChanceBalance ?? 0;

            var (total, opened) = await _repo.CountRoundOpenAsync(roundId);
            if (total > 0 && opened >= total)
            {
                dto.RoundCompleted = true;
                dto.NewRoundId = await _repo.CreateRoundAsync(userId, leagueId);
            }

            dto.Message = overrideMessage ?? (dto.IsLosing ? "پوچ! شانس بعدی رو امتحان کن." : "تبریک! جایزه بردی.");
            return dto;
        }

        private async Task<string> CreateCouponForPrizeAsync(int prizeId, int userId, int boxId)
        {
            var prize = await _repo.GetPrizeByIdAsync(prizeId);
            if (prize == null) return null;

            var coupon = new Coupon
            {
                CoponTitle = prize.Title,
                CoponExpireDate = DateTime.Now.AddDays(prize.ExpireDays.GetValueOrDefault(30) > 0 ? prize.ExpireDays.GetValueOrDefault(30) : 30),
                CoponStock = 1,
                CoponMinBuy = prize.MinBuy ?? "",
                CoponMaxBuy = "",
                StoreID = prize.Scope == 1 ? prize.StoreId.GetValueOrDefault(0) : 0,
                UserID = userId,
                CoponStatStor = true,
                CoponStatAdmin = true,
                DiscountType = prize.DiscountType.GetValueOrDefault(0),
                DiscountValue = prize.DiscountValue
            };

            for (var attempt = 0; attempt < 5; attempt++)
            {
                coupon.CoponCode = "kLL" + prizeId + "-" + RandomAlnum(8);
                try
                {
                    var couponId = prize.Scope == 1
                        ? await _coupons.CreateAsync(coupon)
                        : await _coupons.CreateSystemCouponAsync(coupon);

                    if (couponId > 0)
                    {
                        await _repo.SetBoxResultCouponAsync(boxId, couponId);
                        return coupon.CoponCode;
                    }
                    return null; // شکست کیف‌پول فروشگاه (کد منفی) — بدون تلاش مجدد
                }
                catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_COUPON_CODE")
                {
                    // کد تکراری بود؛ با کد تصادفی جدید دوباره تلاش می‌شود
                }
            }
            return null;
        }

        private static string RandomAlnum(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var buf = new char[length];
            lock (_rng)
            {
                for (var i = 0; i < length; i++)
                    buf[i] = chars[_rng.Next(chars.Length)];
            }
            return new string(buf);
        }

        public async Task<(bool Success, string Message, int NewBalance)> ConvertPointsToChanceAsync(int userId, int leagueId)
        {
            if (!await _users.IsPhoneVerifiedAsync(userId))
                return (false, "برای این عملیات ابتدا شماره تلفن خود را فعال کنید.", 0);

            var (result, newBalance) = await _repo.ConvertPointsToChanceAsync(userId, leagueId);
            if (result == -1) return (false, "لیگ یافت نشد.", 0);
            if (result == -2) return (false, "امتیاز شما برای تبدیل به شانس کافی نیست.", 0);
            return (true, "یک شانس به موجودی شما اضافه شد.", newBalance);
        }

        public Task<List<LuckyBoxOpenReportRow>> GetBoxOpenReportAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search, int skip, int take)
            => _repo.GetBoxOpenReportAsync(leagueId, outcomeFilter, fromUtc, toUtc, search, skip, take);

        public Task<LuckyBoxOpenReportStats> GetBoxOpenReportStatsAsync(
            int? leagueId, string outcomeFilter, DateTime? fromUtc, DateTime? toUtc, string search)
            => _repo.GetBoxOpenReportStatsAsync(leagueId, outcomeFilter, fromUtc, toUtc, search);
    }
}
