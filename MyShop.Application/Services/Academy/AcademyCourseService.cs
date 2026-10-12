using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Academy;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyCourseService : IAcademyCourseService
    {
        private readonly IAcademyCourseRepository _repo;
        private readonly IAcademyScheduleExceptionRepository _exceptions;
        private readonly ICurrentTenant _tenant;
        private readonly IAcademyLocationRepository _locations;
        private readonly IAcademyInstructorRepository _instructors;
        private readonly IDataScopeResolver _scope;

        public AcademyCourseService(
            IAcademyCourseRepository repo,
            IAcademyScheduleExceptionRepository exceptions = null,
            ICurrentTenant tenant = null,
            IAcademyLocationRepository locations = null,
            IAcademyInstructorRepository instructors = null,
            IDataScopeResolver scope = null)
        {
            _repo = repo;
            _exceptions = exceptions;
            _tenant = tenant;
            _locations = locations;
            _instructors = instructors;
            _scope = scope;
        }

        public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
            => _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant) ?? siteId, isActive);

        public async Task<List<AcademyCourseDto>> GetAllForUserAsync(int userId, int? siteId = null, bool? isActive = null)
        {
            var sid = SiteScope.CurrentSiteId(_tenant) ?? siteId;
            if (_scope == null || userId <= 0)
                return await _repo.GetAllAsync(sid, isActive);
            var query = await _scope.BuildQueryAsync(userId, "Academy.View", sid);
            return await _repo.GetAllScopedAsync(query, sid, isActive);
        }

        public async Task<AcademyCourseDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return Owns(item?.SiteId) ? item : null;
        }

        public async Task<AcademyActionResult> CreateAsync(CreateAcademyCourseDto dto)
        {
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            Normalize(dto);
            if (string.IsNullOrWhiteSpace(dto.Title))
                return AcademyActionResult.Fail("عنوان دوره الزامی است.");
            var scheduleError = ValidateSchedules(dto.Schedules);
            if (scheduleError != null)
                return AcademyActionResult.Fail(scheduleError);
            if (!await OwnsReferencesAsync(dto.LocationId, dto.InstructorId))
                return AcademyActionResult.Fail("مکان یا مربی یافت نشد.");

            var id = await _repo.InsertAsync(dto);
            if (id <= 0) return AcademyActionResult.Fail("خطا در ایجاد دوره.");
            await _repo.SetSchedulesAsync(id, dto.Schedules ?? new List<AcademyCourseScheduleDto>());
            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> UpdateAsync(UpdateAcademyCourseDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null || !Owns(existing.SiteId))
                return AcademyActionResult.Fail("دوره یافت نشد یا به‌روزرسانی ناموفق بود.");
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            Normalize(dto);
            if (string.IsNullOrWhiteSpace(dto.Title))
                return AcademyActionResult.Fail("عنوان دوره الزامی است.");
            var scheduleError = ValidateSchedules(dto.Schedules);
            if (scheduleError != null)
                return AcademyActionResult.Fail(scheduleError);
            if (!await OwnsReferencesAsync(dto.LocationId, dto.InstructorId))
                return AcademyActionResult.Fail("مکان یا مربی یافت نشد.");
            if (!await _repo.UpdateAsync(dto))
                return AcademyActionResult.Fail("دوره یافت نشد یا به‌روزرسانی ناموفق بود.");
            await _repo.SetSchedulesAsync(dto.Id, dto.Schedules ?? new List<AcademyCourseScheduleDto>());
            return AcademyActionResult.Success(dto.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            return existing != null && Owns(existing.SiteId) && await _repo.DeleteAsync(id);
        }

        public async Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId)
        {
            var course = await _repo.GetByIdAsync(courseId);
            return course != null && Owns(course.SiteId)
                ? await _repo.GetSessionsAsync(courseId)
                : new List<AcademyCourseSessionDto>();
        }

        public async Task<AcademyActionResult> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto)
        {
            if (dto.CourseId <= 0 || dto.SessionNumber <= 0)
                return AcademyActionResult.Fail("شماره جلسه و دوره الزامی است.");
            var course = await _repo.GetByIdAsync(dto.CourseId);
            if (course == null || !Owns(course.SiteId))
                return AcademyActionResult.Fail("دوره یافت نشد.");
            if (dto.LocationId.HasValue && dto.LocationId <= 0) dto.LocationId = null;
            if (!await OwnsLocationAsync(dto.LocationId))
                return AcademyActionResult.Fail("مکان یافت نشد.");
            var id = await _repo.UpsertSessionAsync(dto);
            return id > 0 ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("ذخیره جلسه ناموفق بود.");
        }

        public async Task<bool> DeleteSessionAsync(int id)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue)
                return await _repo.DeleteSessionAsync(id);

            var courses = await _repo.GetAllAsync(current);
            foreach (var course in courses)
            {
                var sessions = await _repo.GetSessionsAsync(course.Id);
                if (sessions.Any(x => x.Id == id))
                    return await _repo.DeleteSessionAsync(id);
            }
            return false;
        }

        public async Task<AcademyActionResult> GenerateSessionsAsync(int courseId, bool clearExisting = true)
        {
            var course = await _repo.GetByIdAsync(courseId);
            if (course == null || !Owns(course.SiteId)) return AcademyActionResult.Fail("دوره یافت نشد.");
            if (!course.CourseStartDate.HasValue)
                return AcademyActionResult.Fail("تاریخ شروع دوره را مشخص کنید.");
            var weekly = (course.Schedules ?? new List<AcademyCourseScheduleDto>())
                .Where(AcademyDailyScheduleResolver.IsWeekly)
                .Where(s => s.DayId > 0)
                .ToList();
            if (weekly.Count == 0)
                return AcademyActionResult.Fail("حداقل یک روز/ساعت در برنامه هفتگی تعریف کنید.");

            var count = course.NumberOfSessions ?? 0;
            if (count <= 0) count = 8;
            if (count > 200) return AcademyActionResult.Fail("تعداد جلسات بیش از حد مجاز است.");

            // DayId in koch.Days is typically 1=شنبه ... 7=جمعه
            // Map DayId -> DayOfWeek: Saturday=6 in .NET for DayId 1
            static DayOfWeek MapDayId(int dayId) => dayId switch
            {
                1 => DayOfWeek.Saturday,
                2 => DayOfWeek.Sunday,
                3 => DayOfWeek.Monday,
                4 => DayOfWeek.Tuesday,
                5 => DayOfWeek.Wednesday,
                6 => DayOfWeek.Thursday,
                7 => DayOfWeek.Friday,
                _ => DayOfWeek.Saturday
            };

            // فقط Weekly — Daily هرگز به تولید فیزیکی جلسه وارد نمی‌شود
            var slots = weekly
                .OrderBy(s => s.SortOrder).ThenBy(s => s.DayId).ThenBy(s => s.Hour).ThenBy(s => s.Minute)
                .Select(s => new
                {
                    Dow = MapDayId(s.DayId),
                    Start = new TimeSpan(s.Hour, s.Minute < 0 || s.Minute > 59 ? 0 : s.Minute, 0),
                    Duration = s.DurationMinutes is > 0 ? s.DurationMinutes.Value : 60
                })
                .ToList();

            if (clearExisting)
                await _repo.ClearSessionsAsync(courseId);

            var start = course.CourseStartDate.Value.Date;
            var endLimit = course.CourseEndDate?.Date ?? start.AddDays(count * 14 + 30);
            var generated = 0;
            var cursor = start;

            while (generated < count && cursor <= endLimit)
            {
                foreach (var slot in slots)
                {
                    if (generated >= count) break;
                    if (cursor.DayOfWeek != slot.Dow)
                        continue;
                    if (cursor < start) continue;

                    generated++;
                    var endTime = slot.Start.Add(TimeSpan.FromMinutes(slot.Duration));
                    await _repo.UpsertSessionAsync(new UpsertAcademyCourseSessionDto
                    {
                        CourseId = courseId,
                        SessionNumber = generated,
                        SessionDate = cursor,
                        StartTime = slot.Start,
                        EndTime = endTime,
                        LocationId = course.LocationId,
                        Title = $"جلسه {generated}",
                        Status = AcademyConstants.SessionStatus.Scheduled
                    });
                }
                cursor = cursor.AddDays(1);
            }

            if (generated == 0)
                return AcademyActionResult.Fail("هیچ جلسه‌ای تولید نشد. روزهای برنامه با بازه تاریخ دوره همخوانی ندارد.");

            return AcademyActionResult.Success(generated);
        }

        public AcademyLogicalSessionDto ResolveDailyOccurrence(AcademyCourseScheduleDto schedule, DateTime sessionDate)
        {
            return AcademyDailyScheduleResolver.TryResolve(schedule, sessionDate, out var occurrence)
                ? occurrence
                : null;
        }

        public IReadOnlyList<AcademyLogicalSessionDto> ListDailyOccurrences(
            AcademyCourseScheduleDto schedule, DateTime from, DateTime to)
            => AcademyDailyScheduleResolver.ListOccurrences(schedule, from, to);

        public async Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesAsync(
            int courseId, DateTime from, DateTime to, bool includeInactive = false, bool includeCancelled = false)
        {
            if (courseId <= 0 || to.Date < from.Date)
                return new List<AcademyLogicalSessionDto>();

            var course = await _repo.GetByIdAsync(courseId);
            if (course == null || !Owns(course.SiteId) || (!includeInactive && !course.IsActive))
                return new List<AcademyLogicalSessionDto>();

            var exceptions = _exceptions == null
                ? new List<AcademyScheduleExceptionDto>()
                : await _exceptions.ListAsync(courseId: courseId, fromDate: from.Date, toDate: to.Date)
                    ?? new List<AcademyScheduleExceptionDto>();
            var list = AcademyDailyScheduleResolver.ListOccurrences(
                course.Schedules, from, to, exceptions, includeCancelled: includeCancelled);
            foreach (var item in list)
            {
                if (item.CourseId <= 0)
                    item.CourseId = course.Id;
                item.CourseTitle = course.Title;
            }
            return list.ToList();
        }

        public async Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesForScheduleAsync(
            int scheduleId, DateTime from, DateTime to, bool includeInactive = false, bool includeCancelled = false)
        {
            if (scheduleId <= 0 || to.Date < from.Date)
                return new List<AcademyLogicalSessionDto>();

            var schedule = await _repo.GetScheduleByIdAsync(scheduleId);
            if (schedule == null || !schedule.IsDaily)
                return new List<AcademyLogicalSessionDto>();

            var course = schedule.CourseId > 0 ? await _repo.GetByIdAsync(schedule.CourseId) : null;
            if (course == null || !Owns(course.SiteId))
                return new List<AcademyLogicalSessionDto>();
            if (course != null && !includeInactive && !course.IsActive)
                return new List<AcademyLogicalSessionDto>();

            var exceptions = _exceptions == null
                ? new List<AcademyScheduleExceptionDto>()
                : await _exceptions.ListAsync(scheduleId: scheduleId, fromDate: from.Date, toDate: to.Date)
                    ?? new List<AcademyScheduleExceptionDto>();
            var list = AcademyDailyScheduleResolver.ListOccurrences(
                schedule, from, to, exceptions, includeCancelled: includeCancelled).ToList();
            foreach (var item in list)
            {
                if (item.CourseId <= 0 && course != null)
                    item.CourseId = course.Id;
                item.CourseTitle = course?.Title;
            }
            return list;
        }

        public async Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesForInstructorAsync(
            int instructorId, DateTime from, DateTime to)
        {
            if (instructorId <= 0 || to.Date < from.Date)
                return new List<AcademyLogicalSessionDto>();

            var courses = await _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant)) ?? new List<AcademyCourseDto>();
            var result = new List<AcademyLogicalSessionDto>();
            foreach (var course in courses.Where(c => c != null && c.IsActive && c.InstructorId == instructorId))
            {
                var days = await ListDailyOccurrencesAsync(course.Id, from, to);
                result.AddRange(days);
            }
            return result
                .OrderBy(x => x.SessionDate)
                .ThenBy(x => x.Hour)
                .ThenBy(x => x.Minute)
                .ThenBy(x => x.ScheduleId)
                .ToList();
        }

        public async Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesForStudentAsync(
            DateTime from, DateTime to, int? courseId = null)
        {
            if (to.Date < from.Date)
                return new List<AcademyLogicalSessionDto>();

            var courses = await _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant), isActive: true) ?? new List<AcademyCourseDto>();
            var result = new List<AcademyLogicalSessionDto>();
            foreach (var course in courses.Where(c =>
                c != null && c.IsActive && (!courseId.HasValue || courseId.Value <= 0 || c.Id == courseId.Value)))
            {
                var days = await ListDailyOccurrencesAsync(course.Id, from, to);
                result.AddRange(days);
            }
            return result
                .OrderBy(x => x.SessionDate)
                .ThenBy(x => x.Hour)
                .ThenBy(x => x.Minute)
                .ThenBy(x => x.ScheduleId)
                .ToList();
        }

        private static void Normalize(CreateAcademyCourseDto dto)
        {
            if (dto.LocationId.HasValue && dto.LocationId <= 0) dto.LocationId = null;
            if (dto.InstructorId.HasValue && dto.InstructorId <= 0) dto.InstructorId = null;
            dto.Schedules ??= new List<AcademyCourseScheduleDto>();
            foreach (var s in dto.Schedules)
            {
                if (s.Hour < 0) s.Hour = 0;
                if (s.Hour > 23) s.Hour = 23;
                if (s.Minute < 0 || s.Minute > 59) s.Minute = 0;
                if (string.IsNullOrWhiteSpace(s.RecurrenceType))
                    s.RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly;
                if (s.IsDaily)
                    s.DayId = 0;
            }
        }

        private static string ValidateSchedules(List<AcademyCourseScheduleDto> schedules)
        {
            if (schedules == null || schedules.Count == 0)
                return null;

            foreach (var s in schedules)
            {
                if (s == null) continue;
                if (s.IsWeekly)
                    continue;
                if (!s.IsDaily)
                    return "نوع برنامه نامعتبر است.";
                if (!s.StartDate.HasValue || !s.EndDate.HasValue)
                    return "برای برنامه روزانه، تاریخ شروع و پایان الزامی است.";
                if (s.EndDate.Value.Date < s.StartDate.Value.Date)
                    return "تاریخ پایان برنامه روزانه نمی‌تواند قبل از تاریخ شروع باشد.";
                if (s.Capacity.HasValue && s.Capacity.Value <= 0)
                    return "ظرفیت روزانه باید خالی (نامحدود) یا بزرگ‌تر از صفر باشد.";
            }

            return null;
        }

        private bool Owns(int? siteId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            return !current.HasValue || siteId == current;
        }

        private async Task<bool> OwnsReferencesAsync(int? locationId, int? instructorId)
        {
            if (!await OwnsLocationAsync(locationId)) return false;
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || !instructorId.HasValue) return true;
            if (_instructors == null) return false;
            var instructor = await _instructors.GetByIdAsync(instructorId.Value);
            return instructor?.SiteId == current;
        }

        private async Task<bool> OwnsLocationAsync(int? locationId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || !locationId.HasValue) return true;
            if (_locations == null) return false;
            var location = await _locations.GetByIdAsync(locationId.Value);
            return location?.SiteId == current;
        }
    }
}
