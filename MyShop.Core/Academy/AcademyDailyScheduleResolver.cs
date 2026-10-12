using System;
using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Academy;

namespace MyShop.Core.Academy
{
    /// <summary>
    /// Resolve جلسه منطقی Daily روی Schedule موجود.
    /// Weekly را تغییر نمی‌دهد و جلسه فیزیکی نمی‌سازد.
    /// </summary>
    public static class AcademyDailyScheduleResolver
    {
        /// <summary>سقف لیست منطقی در یک Query تا از انفجار حافظه جلوگیری شود. ۳۶۰ روز دوره داخل این حد است.</summary>
        public const int MaxListedDaysPerSchedule = 400;

        public static bool IsDaily(AcademyCourseScheduleDto schedule) =>
            schedule != null && schedule.IsDaily;

        public static bool IsWeekly(AcademyCourseScheduleDto schedule) =>
            schedule != null && schedule.IsWeekly;

        public static bool HasValidDailyRange(AcademyCourseScheduleDto schedule)
        {
            if (schedule == null || !schedule.IsDaily)
                return false;
            if (!schedule.StartDate.HasValue || !schedule.EndDate.HasValue)
                return false;
            return schedule.EndDate.Value.Date >= schedule.StartDate.Value.Date;
        }

        /// <summary>
        /// اگر برنامه Daily باشد و تاریخ داخل بازه باشد، جلسه منطقی برمی‌گردد.
        /// استثنای لغو → false. Weekly یا خارج بازه → false.
        /// </summary>
        public static bool TryResolve(
            AcademyCourseScheduleDto schedule,
            DateTime sessionDate,
            out AcademyLogicalSessionDto occurrence)
            => TryResolve(schedule, sessionDate, null, out occurrence);

        public static bool TryResolve(
            AcademyCourseScheduleDto schedule,
            DateTime sessionDate,
            AcademyScheduleExceptionDto exception,
            out AcademyLogicalSessionDto occurrence)
        {
            occurrence = null;
            if (!HasValidDailyRange(schedule))
                return false;

            var date = sessionDate.Date;
            var start = schedule.StartDate.Value.Date;
            var end = schedule.EndDate.Value.Date;
            if (date < start || date > end)
                return false;

            if (exception != null
                && exception.ScheduleId == schedule.Id
                && exception.ExceptionDate.Date == date
                && exception.IsCancelled)
                return false;

            occurrence = ToOccurrence(schedule, date, Matches(exception, schedule.Id, date) ? exception : null);
            return true;
        }

        /// <summary>
        /// روزهای معتبر Daily در اشتراک بازه برنامه و بازه Query.
        /// هیچ INSERT ای انجام نمی‌شود. Weekly نادیده گرفته می‌شود.
        /// </summary>
        public static IReadOnlyList<AcademyLogicalSessionDto> ListOccurrences(
            AcademyCourseScheduleDto schedule,
            DateTime from,
            DateTime to,
            int maxDays = MaxListedDaysPerSchedule)
            => ListOccurrences(schedule, from, to, null, maxDays);

        public static IReadOnlyList<AcademyLogicalSessionDto> ListOccurrences(
            AcademyCourseScheduleDto schedule,
            DateTime from,
            DateTime to,
            IEnumerable<AcademyScheduleExceptionDto> exceptions,
            int maxDays = MaxListedDaysPerSchedule,
            bool includeCancelled = false)
        {
            var result = new List<AcademyLogicalSessionDto>();
            if (!HasValidDailyRange(schedule))
                return result;
            if (maxDays <= 0)
                maxDays = MaxListedDaysPerSchedule;

            var fromDate = from.Date;
            var toDate = to.Date;
            if (toDate < fromDate)
                return result;

            var start = schedule.StartDate.Value.Date;
            var end = schedule.EndDate.Value.Date;
            var windowStart = fromDate > start ? fromDate : start;
            var windowEnd = toDate < end ? toDate : end;
            if (windowEnd < windowStart)
                return result;

            var byDate = (exceptions ?? Enumerable.Empty<AcademyScheduleExceptionDto>())
                .Where(e => e != null && e.ScheduleId == schedule.Id)
                .GroupBy(e => e.ExceptionDate.Date)
                .ToDictionary(g => g.Key, g => g.Last());

            var cursor = windowStart;
            var count = 0;
            while (cursor <= windowEnd && count < maxDays)
            {
                byDate.TryGetValue(cursor, out var ex);
                if (ex == null || !ex.IsCancelled || includeCancelled)
                    result.Add(ToOccurrence(schedule, cursor, ex));
                cursor = cursor.AddDays(1);
                count++;
            }

            return result;
        }

        public static IReadOnlyList<AcademyLogicalSessionDto> ListOccurrences(
            IEnumerable<AcademyCourseScheduleDto> schedules,
            DateTime from,
            DateTime to,
            int maxDaysPerSchedule = MaxListedDaysPerSchedule)
            => ListOccurrences(schedules, from, to, null, maxDaysPerSchedule);

        public static IReadOnlyList<AcademyLogicalSessionDto> ListOccurrences(
            IEnumerable<AcademyCourseScheduleDto> schedules,
            DateTime from,
            DateTime to,
            IEnumerable<AcademyScheduleExceptionDto> exceptions,
            int maxDaysPerSchedule = MaxListedDaysPerSchedule,
            bool includeCancelled = false)
        {
            return (schedules ?? Enumerable.Empty<AcademyCourseScheduleDto>())
                .Where(IsDaily)
                .SelectMany(s => ListOccurrences(s, from, to, exceptions, maxDaysPerSchedule, includeCancelled))
                .OrderBy(x => x.SessionDate)
                .ThenBy(x => x.Hour)
                .ThenBy(x => x.Minute)
                .ThenBy(x => x.ScheduleId)
                .ToList();
        }

        static bool Matches(AcademyScheduleExceptionDto exception, int scheduleId, DateTime date) =>
            exception != null
            && exception.ScheduleId == scheduleId
            && exception.ExceptionDate.Date == date.Date;

        static AcademyLogicalSessionDto ToOccurrence(
            AcademyCourseScheduleDto schedule, DateTime date, AcademyScheduleExceptionDto exception = null)
        {
            var duration = schedule.DurationMinutes is > 0 ? schedule.DurationMinutes.Value : 60;
            var hour = schedule.Hour;
            var minute = schedule.Minute < 0 || schedule.Minute > 59 ? 0 : schedule.Minute;
            var capacity = schedule.Capacity;
            int? instructorId = null;
            var hasException = false;
            var cancelled = exception != null && exception.IsCancelled;
            string note = null;

            if (cancelled)
            {
                return new AcademyLogicalSessionDto
                {
                    ScheduleId = schedule.Id,
                    CourseId = schedule.CourseId,
                    SessionDate = date.Date,
                    Hour = hour,
                    Minute = minute,
                    DurationMinutes = duration,
                    RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                    Capacity = capacity,
                    HasException = true,
                    IsCancelled = true,
                    ExceptionNote = exception.Note
                };
            }

            if (exception != null)
            {
                hasException = true;
                note = exception.Note;
                if (exception.Hour is >= 0 and <= 23)
                    hour = exception.Hour.Value;
                if (exception.Minute is >= 0 and <= 59)
                    minute = exception.Minute.Value;
                if (exception.DurationMinutes is > 0)
                    duration = exception.DurationMinutes.Value;
                capacity = AcademyDailyCapacity.Effective(schedule.Capacity, exception.Capacity);
                if (exception.InstructorId is > 0)
                    instructorId = exception.InstructorId;
            }

            return new AcademyLogicalSessionDto
            {
                ScheduleId = schedule.Id,
                CourseId = schedule.CourseId,
                SessionDate = date.Date,
                Hour = hour,
                Minute = minute,
                DurationMinutes = duration,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Capacity = capacity,
                InstructorId = instructorId,
                HasException = hasException,
                ExceptionNote = note
            };
        }
    }
}
