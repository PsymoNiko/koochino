using System;
using System.Globalization;

namespace MyShop.Core.Helpers
{
    /// <summary>
    /// Helper class for converting Gregorian dates to Persian (Shamsi) dates
    /// </summary>
    public static class PersianDateHelper
    {
        private static readonly PersianCalendar _persianCalendar = new PersianCalendar();

        /// <summary>
        /// Converts a DateTime to Persian date string (yyyy/MM/dd)
        /// </summary>
        public static string ToPersianDateString(DateTime date)
        {
            if (date == default(DateTime))
                return "";

            int year = _persianCalendar.GetYear(date);
            int month = _persianCalendar.GetMonth(date);
            int day = _persianCalendar.GetDayOfMonth(date);

            return $"{year:0000}/{month:00}/{day:00}";
        }

        /// <summary>Nullable overload: returns empty string when null.</summary>
        public static string ToPersianDateString(DateTime? date) => date.HasValue ? ToPersianDateString(date.Value) : "";

        /// <summary>
        /// Converts a DateTime to Persian date and time string (yyyy/MM/dd HH:mm)
        /// </summary>
        public static string ToPersianDateTimeString(DateTime date)
        {
            if (date == default(DateTime))
                return "";

            int year = _persianCalendar.GetYear(date);
            int month = _persianCalendar.GetMonth(date);
            int day = _persianCalendar.GetDayOfMonth(date);
            int hour = _persianCalendar.GetHour(date);
            int minute = _persianCalendar.GetMinute(date);

            return $"{year:0000}/{month:00}/{day:00} {hour:00}:{minute:00}";
        }

        /// <summary>Nullable overload: returns empty string when null.</summary>
        public static string ToPersianDateTimeString(DateTime? date) => date.HasValue ? ToPersianDateTimeString(date.Value) : "";

        /// <summary>
        /// Converts a DateTime to Persian date string with time (yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public static string ToPersianDateTimeFullString(DateTime date)
        {
            if (date == default(DateTime))
                return "";

            int year = _persianCalendar.GetYear(date);
            int month = _persianCalendar.GetMonth(date);
            int day = _persianCalendar.GetDayOfMonth(date);
            int hour = _persianCalendar.GetHour(date);
            int minute = _persianCalendar.GetMinute(date);
            int second = _persianCalendar.GetSecond(date);

            return $"{year:0000}/{month:00}/{day:00} {hour:00}:{minute:00}:{second:00}";
        }

        /// <summary>
        /// Converts a Persian date string (yyyy/MM/dd) to DateTime
        /// </summary>
        public static DateTime? FromPersianDateString(string persianDate)
        {
            if (string.IsNullOrWhiteSpace(persianDate))
                return null;

            try
            {
                var parts = persianDate.Split('/');
                if (parts.Length != 3)
                    return null;

                int year = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);
                int day = int.Parse(parts[2]);

                return _persianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets Persian month name
        /// </summary>
        public static string GetPersianMonthName(int month)
        {
            string[] months = {
                "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
                "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
            };

            if (month >= 1 && month <= 12)
                return months[month];
            return "";
        }

        /// <summary>
        /// Gets Persian day of week name
        /// </summary>
        public static string GetPersianDayOfWeekName(DateTime date)
        {
            string[] days = {
                "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه", "شنبه"
            };

            DayOfWeek dayOfWeek = _persianCalendar.GetDayOfWeek(date);
            return days[(int)dayOfWeek];
        }
    }
}
