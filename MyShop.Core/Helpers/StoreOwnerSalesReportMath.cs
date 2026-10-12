using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MyShop.Core.Models.Shop;

namespace MyShop.Core.Helpers
{
    /// <summary>تجمیع گزارش فروش مغازه‌دار روی تقویم شمسی — مستقل از SP.</summary>
    public static class StoreOwnerSalesReportMath
    {
        static readonly string[] ShortMonths =
        {
            "", "فر", "ار", "خر", "تی", "مر", "شه", "مه", "آبا", "آذر", "دی", "به", "اس"
        };

        public static StoreOwnerSalesSummaryDto Summarize(IEnumerable<StoreOwnerSalesDayDto> days)
        {
            var list = days?.ToList() ?? new List<StoreOwnerSalesDayDto>();
            var amount = list.Sum(x => x.Amount);
            var orders = list.Sum(x => x.OrderCount);
            return new StoreOwnerSalesSummaryDto
            {
                TotalAmount = amount,
                OrderCount = orders,
                AvgOrderAmount = orders <= 0 ? 0 : amount / orders
            };
        }

        public static List<StoreOwnerSalesPeriodDto> ByPersianMonth(IEnumerable<StoreOwnerSalesDayDto> days)
        {
            var pc = new PersianCalendar();
            return (days ?? Enumerable.Empty<StoreOwnerSalesDayDto>())
                .GroupBy(d =>
                {
                    var dt = d.SaleDate.Date;
                    return (Year: pc.GetYear(dt), Month: pc.GetMonth(dt));
                })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => CreateMonth(g.Key.Year, g.Key.Month, g.Sum(x => x.Amount), g.Sum(x => x.OrderCount)))
                .ToList();
        }

        public static List<StoreOwnerSalesPeriodDto> ByPersianYear(IEnumerable<StoreOwnerSalesDayDto> days)
        {
            var pc = new PersianCalendar();
            return (days ?? Enumerable.Empty<StoreOwnerSalesDayDto>())
                .GroupBy(d => pc.GetYear(d.SaleDate.Date))
                .OrderBy(g => g.Key)
                .Select(g => new StoreOwnerSalesPeriodDto
                {
                    Year = g.Key,
                    Month = 0,
                    Label = g.Key.ToString(CultureInfo.InvariantCulture),
                    ShortLabel = g.Key.ToString(CultureInfo.InvariantCulture),
                    Amount = g.Sum(x => x.Amount),
                    OrderCount = g.Sum(x => x.OrderCount)
                })
                .ToList();
        }

        public static List<StoreOwnerSalesPeriodDto> FillPersianMonths(
            IEnumerable<StoreOwnerSalesPeriodDto> months, DateTime from, DateTime to)
        {
            if (to.Date < from.Date)
                (from, to) = (to, from);

            var pc = new PersianCalendar();
            var map = (months ?? Enumerable.Empty<StoreOwnerSalesPeriodDto>())
                .ToDictionary(m => (m.Year, m.Month), m => m);
            var result = new List<StoreOwnerSalesPeriodDto>();
            var y = pc.GetYear(from.Date);
            var m = pc.GetMonth(from.Date);
            var endY = pc.GetYear(to.Date);
            var endM = pc.GetMonth(to.Date);

            while (y < endY || (y == endY && m <= endM))
            {
                result.Add(map.TryGetValue((y, m), out var row)
                    ? row
                    : CreateMonth(y, m, 0, 0));
                m++;
                if (m > 12) { m = 1; y++; }
            }

            return result;
        }

        public static List<StoreOwnerStoreSalesDto> WithShare(IEnumerable<StoreOwnerStoreSalesDto> stores)
        {
            var list = stores?.ToList() ?? new List<StoreOwnerStoreSalesDto>();
            var total = list.Sum(x => x.Amount);
            foreach (var s in list)
                s.SharePercent = total <= 0 ? 0 : Math.Round(100.0 * s.Amount / total, 1);
            return list.OrderByDescending(x => x.Amount).ToList();
        }

        public static (DateTime From, DateTime To) PresetRange(string preset, DateTime today)
        {
            today = today.Date;
            var pc = new PersianCalendar();
            var y = pc.GetYear(today);
            var m = pc.GetMonth(today);
            var d = pc.GetDayOfMonth(today);

            return (preset ?? "").Trim() switch
            {
                "month" => (pc.ToDateTime(y, m, 1, 0, 0, 0, 0), today),
                "3m" => (AddPersianMonths(pc, y, m, d, -2), today),
                "year" => (pc.ToDateTime(y, 1, 1, 0, 0, 0, 0), today),
                "12m" => (AddPersianMonths(pc, y, m, d, -11), today),
                _ => (pc.ToDateTime(y, 1, 1, 0, 0, 0, 0), today)
            };
        }

        public static string NormalizeSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var sb = new StringBuilder(input.Length);
            foreach (var c in input.Trim())
            {
                if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
                else if (c is '%' or '_' or '[' or ']') continue;
                else sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        public static string CompactAmount(long n)
        {
            if (n >= 1_000_000_000) return (n / 1_000_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + " میلیارد";
            if (n >= 1_000_000) return (n / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + " میلیون";
            if (n >= 1_000) return (n / 1_000d).ToString("0.#", CultureInfo.InvariantCulture) + " هزار";
            return n.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static double BarPercent(long amount, long max)
        {
            if (max <= 0 || amount <= 0) return 0;
            return Math.Max(4, Math.Round(100.0 * amount / max, 1));
        }

        static StoreOwnerSalesPeriodDto CreateMonth(int year, int month, long amount, int orderCount)
            => new()
            {
                Year = year,
                Month = month,
                Label = $"{PersianDateHelper.GetPersianMonthName(month)} {year}",
                ShortLabel = month >= 1 && month <= 12 ? ShortMonths[month] : month.ToString(),
                Amount = amount,
                OrderCount = orderCount
            };

        static DateTime AddPersianMonths(PersianCalendar pc, int year, int month, int day, int delta)
        {
            var total = (year * 12 + (month - 1)) + delta;
            var y = total / 12;
            var m = total % 12 + 1;
            var maxDay = pc.GetDaysInMonth(y, m);
            var d = Math.Min(day, maxDay);
            return pc.ToDateTime(y, m, d, 0, 0, 0, 0);
        }
    }
}
