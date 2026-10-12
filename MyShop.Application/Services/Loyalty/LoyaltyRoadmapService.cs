using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Loyalty;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models;
using MyShop.Core.Models.Loyalty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Loyalty
{
    public class LoyaltyRoadmapService : ILoyaltyRoadmapService
    {
        private readonly ILoyaltyRoadmapRepository _repo;
        private readonly ISmsSender _sms;
        private readonly IOrderService _orders;
        private readonly IPointService _points;

        public LoyaltyRoadmapService(
            ILoyaltyRoadmapRepository repo,
            ISmsSender sms,
            IOrderService orders,
            IPointService points)
        {
            _repo = repo;
            _sms = sms;
            _orders = orders;
            _points = points;
        }

        public Task<List<SmsCampaign>> ListCampaignsAsync(int? siteId) => _repo.ListCampaignsAsync(siteId);
        public Task<int> SaveCampaignAsync(SmsCampaign c) => _repo.UpsertCampaignAsync(c);

        public async Task<(bool ok, string message)> SendCampaignAsync(int campaignId)
        {
            var camp = await _repo.GetCampaignAsync(campaignId);
            if (camp == null) return (false, "کمپین یافت نشد.");
            if (string.IsNullOrWhiteSpace(camp.MessageText)) return (false, "متن پیامک خالی است.");

            var phones = new List<string>();
            if (string.Equals(camp.AudienceType, "ManualPhones", StringComparison.OrdinalIgnoreCase))
            {
                phones = (camp.ManualPhones ?? "")
                    .Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).Where(x => x.Length >= 10).Distinct().ToList();
            }
            else if (string.Equals(camp.AudienceType, "Segment", StringComparison.OrdinalIgnoreCase) && camp.SegmentId.HasValue)
            {
                var segs = await _repo.ListSegmentsAsync(camp.SiteId);
                var seg = segs?.FirstOrDefault(s => s.Id == camp.SegmentId.Value);
                var rfm = await _repo.ListRfmAsync(camp.SiteId, 5000) ?? new List<CustomerRfmScore>();
                var filtered = FilterBySegmentRule(rfm, seg?.RuleJson);
                var users = await _repo.ListUsersForAudienceAsync(camp.SiteId);
                var ids = filtered.Select(x => x.UserId).ToHashSet();
                phones = users.Where(u => ids.Contains(u.UserId)).Select(u => u.Phone)
                    .Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            }
            else
            {
                var users = await _repo.ListUsersForAudienceAsync(camp.SiteId);
                phones = users.Select(u => u.Phone).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            }

            if (phones.Count == 0) return (false, "مخاطبی برای ارسال یافت نشد.");

            await _repo.UpdateCampaignStatsAsync(camp.Id, "Sending", 0, 0);
            int sent = 0, fail = 0;
            foreach (var phone in phones.Take(500))
            {
                try
                {
                    var r = await _sms.SendAsync(phone, camp.MessageText);
                    if (r.Ok) sent++; else fail++;
                }
                catch { fail++; }
            }
            await _repo.UpdateCampaignStatsAsync(camp.Id, fail > 0 && sent == 0 ? "Failed" : "Sent", sent, fail);
            return (true, $"ارسال شد: {sent} موفق، {fail} ناموفق از {phones.Count} مخاطب.");
        }

        public Task<List<CustomerSegment>> ListSegmentsAsync(int? siteId) => _repo.ListSegmentsAsync(siteId);
        public Task<int> SaveSegmentAsync(CustomerSegment s) => _repo.UpsertSegmentAsync(s);

        public async Task RecalculateRfmAsync(int? siteId)
        {
            var from = DateTime.Today.AddYears(-2);
            var orders = await _orders.GetListAsync(from, DateTime.Now.AddDays(1), null, null, null, null, 5000, 0)
                         ?? new List<Order>();
            // status completed often = 3 or similar; include all with amount
            var byUser = orders
                .Where(o => o.UserId > 0)
                .GroupBy(o => o.UserId)
                .ToList();

            var spends = byUser.Select(g => (decimal)g.Sum(x => x.FinalAmount > 0 ? x.FinalAmount : x.TotalAmount)).OrderBy(x => x).ToList();
            var freqs = byUser.Select(g => g.Count()).OrderBy(x => x).ToList();

            foreach (var g in byUser)
            {
                var last = g.Max(x => x.OrderDate);
                var days = (DateTime.Today - last.Date).TotalDays;
                byte r = days <= 30 ? (byte)5 : days <= 60 ? (byte)4 : days <= 120 ? (byte)3 : days <= 240 ? (byte)2 : (byte)1;
                var freq = g.Count();
                byte f = ScoreQuintile(freq, freqs);
                var spend = (decimal)g.Sum(x => x.FinalAmount > 0 ? x.FinalAmount : x.TotalAmount);
                byte m = ScoreQuintile(spend, spends);

                await _repo.UpsertRfmAsync(new CustomerRfmScore
                {
                    UserId = g.Key,
                    SiteId = siteId,
                    RecencyScore = r,
                    FrequencyScore = f,
                    MonetaryScore = m,
                    LastOrderAt = last,
                    OrderCount = freq,
                    TotalSpend = spend
                });
            }
        }

        static byte ScoreQuintile(decimal value, List<decimal> sortedAsc)
        {
            if (sortedAsc == null || sortedAsc.Count == 0) return 1;
            int idx = sortedAsc.FindIndex(x => x >= value);
            if (idx < 0) idx = sortedAsc.Count - 1;
            double p = (double)idx / Math.Max(1, sortedAsc.Count - 1);
            if (p >= 0.8) return 5;
            if (p >= 0.6) return 4;
            if (p >= 0.4) return 3;
            if (p >= 0.2) return 2;
            return 1;
        }

        static byte ScoreQuintile(int value, List<int> sortedAsc)
        {
            if (sortedAsc == null || sortedAsc.Count == 0) return 1;
            int idx = sortedAsc.FindIndex(x => x >= value);
            if (idx < 0) idx = sortedAsc.Count - 1;
            double p = (double)idx / Math.Max(1, sortedAsc.Count - 1);
            if (p >= 0.8) return 5;
            if (p >= 0.6) return 4;
            if (p >= 0.4) return 3;
            if (p >= 0.2) return 2;
            return 1;
        }

        static List<CustomerRfmScore> FilterBySegmentRule(List<CustomerRfmScore> rfm, string ruleJson)
        {
            if (rfm == null || rfm.Count == 0) return new List<CustomerRfmScore>();
            if (string.IsNullOrWhiteSpace(ruleJson)) return rfm;
            // RuleJson نمونه: {"minR":4,"maxR":5,"minF":1,"maxF":5,"minM":1,"maxM":5}
            byte minR = 1, maxR = 5, minF = 1, maxF = 5, minM = 1, maxM = 5;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(ruleJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("minR", out var a)) minR = a.GetByte();
                if (root.TryGetProperty("maxR", out var b)) maxR = b.GetByte();
                if (root.TryGetProperty("minF", out var c)) minF = c.GetByte();
                if (root.TryGetProperty("maxF", out var d)) maxF = d.GetByte();
                if (root.TryGetProperty("minM", out var e)) minM = e.GetByte();
                if (root.TryGetProperty("maxM", out var f)) maxM = f.GetByte();
            }
            catch { return rfm; }

            return rfm.Where(x =>
                x.RecencyScore >= minR && x.RecencyScore <= maxR &&
                x.FrequencyScore >= minF && x.FrequencyScore <= maxF &&
                x.MonetaryScore >= minM && x.MonetaryScore <= maxM).ToList();
        }

        public Task<List<CustomerRfmScore>> ListRfmAsync(int? siteId) => _repo.ListRfmAsync(siteId);

        public Task<List<VipTier>> ListVipTiersAsync(int? siteId) => _repo.ListVipTiersAsync(siteId);
        public Task<int> SaveVipTierAsync(VipTier t) => _repo.UpsertVipTierAsync(t);
        public Task DeleteVipTierAsync(int id) => _repo.DeleteVipTierAsync(id);

        public async Task<VipTier> ResolveVipTierAsync(int userId, int? siteId)
        {
            var summary = await _points.GetUserPointsSummaryAsync(userId);
            var pts = summary?.AvailablePoints ?? summary?.TotalPoints ?? 0;
            var tiers = await _repo.ListVipTiersAsync(siteId);
            return tiers.Where(t => t.MinPoints <= pts).OrderByDescending(t => t.MinPoints).FirstOrDefault()
                   ?? tiers.OrderBy(t => t.MinPoints).FirstOrDefault();
        }

        public Task<decimal> GetWalletBalanceAsync(int userId) => _repo.GetWalletBalanceAsync(userId);
        public Task<List<WalletTransaction>> ListWalletTxAsync(int userId) => _repo.ListWalletTxAsync(userId);

        public async Task<(bool ok, string message)> AdjustWalletAsync(int userId, decimal amount, string reason, int? adminUserId)
        {
            if (userId <= 0) return (false, "کاربر نامعتبر است.");
            if (amount == 0) return (false, "مبلغ صفر است.");
            var bal = await _repo.GetWalletBalanceAsync(userId);
            if (bal + amount < 0) return (false, "موجودی کافی نیست.");
            var type = amount >= 0 ? "Credit" : "Debit";
            await _repo.AdjustWalletAsync(userId, amount, type, reason ?? "تعدیل ادمین", adminUserId, null);
            return (true, "موجودی کیف پول به‌روز شد.");
        }

        public async Task<(bool ok, string message)> SubmitNpsAsync(int userId, byte score, string comment, int? orderId, int? siteId)
        {
            if (userId <= 0) return (false, "ورود لازم است.");
            if (score > 10) return (false, "امتیاز باید ۰ تا ۱۰ باشد.");
            await _repo.InsertNpsAsync(new NpsResponse
            {
                UserId = userId,
                OrderId = orderId,
                Score = score,
                Comment = comment,
                SiteId = siteId
            });
            return (true, "نظر شما ثبت شد. متشکریم.");
        }

        public Task<List<NpsResponse>> ListNpsAsync(int? siteId) => _repo.ListNpsAsync(siteId);
        public Task<List<LoyaltyAutomation>> ListAutomationsAsync(int? siteId) => _repo.ListAutomationsAsync(siteId);
        public Task<int> SaveAutomationAsync(LoyaltyAutomation a) => _repo.UpsertAutomationAsync(a);
        public Task DeleteAutomationAsync(int id) => _repo.DeleteAutomationAsync(id);
        public Task SetBirthDateAsync(int userId, DateTime? birthDate) => _repo.SetUserBirthDateAsync(userId, birthDate);
        public Task<DateTime?> GetBirthDateAsync(int userId) => _repo.GetUserBirthDateAsync(userId);

        public async Task<string> BuildAiCampaignSuggestionAsync(int? siteId)
        {
            var rfm = await _repo.ListRfmAsync(siteId, 200);
            var nps = await _repo.ListNpsAsync(siteId, 100);
            var atRisk = rfm.Count(x => x.RecencyScore <= 2);
            var champions = rfm.Count(x => x.RecencyScore >= 4 && x.FrequencyScore >= 4);
            var avgNps = nps.Count == 0 ? 0 : nps.Average(x => (double)x.Score);
            return
                $"پیشنهاد کمپین:\n" +
                $"• {atRisk} مشتری در خطر ریزش (Recency پایین) — پیامک یادآوری با تخفیف محدود.\n" +
                $"• {champions} مشتری وفادار — پیام تشکر + دعوت دوستان.\n" +
                $"• میانگین NPS: {avgNps:0.0} — " + (avgNps < 7 ? "اولویت بهبود تجربه و نظرسنجی جبرانی." : "فرصت آپسل با آفر اختصاصی.") + "\n" +
                $"از مسیر /admin/sms/campaigns کمپین بسازید و مخاطب Manual یا AllCustomers را انتخاب کنید.";
        }
    }
}
