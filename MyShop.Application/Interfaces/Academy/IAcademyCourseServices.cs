using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;
using MyShop.Core.Models.Security;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyCourseRepository
    {
        Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<List<AcademyCourseDto>> GetAllScopedAsync(DataScopeQuery scope, int? siteId = null, bool? isActive = null)
            => GetAllAsync(siteId, isActive);
        Task<AcademyCourseDto> GetByIdAsync(int id);
        Task<int> InsertAsync(CreateAcademyCourseDto dto);
        Task<bool> UpdateAsync(UpdateAcademyCourseDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId);
        Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId);
        Task SetSchedulesAsync(int courseId, IEnumerable<AcademyCourseScheduleDto> schedules);
        Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId);
        Task ClearSessionsAsync(int courseId);
        Task<int> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto);
        Task<bool> DeleteSessionAsync(int id);
    }

    public interface IAcademyCourseService
    {
        Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<List<AcademyCourseDto>> GetAllForUserAsync(int userId, int? siteId = null, bool? isActive = null);
        Task<AcademyCourseDto> GetByIdAsync(int id);
        Task<AcademyActionResult> CreateAsync(CreateAcademyCourseDto dto);
        Task<AcademyActionResult> UpdateAsync(UpdateAcademyCourseDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId);
        Task<AcademyActionResult> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto);
        Task<bool> DeleteSessionAsync(int id);
        /// <summary>تولید جلسات فیزیکی فقط از روی برنامه Weekly. Daily وارد این مسیر نمی‌شود.</summary>
        Task<AcademyActionResult> GenerateSessionsAsync(int courseId, bool clearExisting = true);
        /// <summary>جلسه منطقی Daily برای یک تاریخ؛ خارج از بازه یا غیر Daily مقدار null.</summary>
        AcademyLogicalSessionDto ResolveDailyOccurrence(AcademyCourseScheduleDto schedule, DateTime sessionDate);
        /// <summary>لیست روزهای منطقی Daily یک برنامه در بازه؛ بدون INSERT جلسه فیزیکی.</summary>
        IReadOnlyList<AcademyLogicalSessionDto> ListDailyOccurrences(AcademyCourseScheduleDto schedule, DateTime from, DateTime to);
        /// <summary>لیست روزهای منطقی Daily یک دوره فعال در بازه؛ Weekly و دوره غیرفعال خالی است.</summary>
        Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesAsync(
            int courseId, DateTime from, DateTime to, bool includeInactive = false, bool includeCancelled = false);
        /// <summary>لیست روزهای منطقی Daily یک Schedule مشخص در بازه.</summary>
        Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesForScheduleAsync(
            int scheduleId, DateTime from, DateTime to, bool includeInactive = false, bool includeCancelled = false);
        /// <summary>روزهای منطقی Daily دوره‌های فعال همان مربی. دوره دیگران خالی است.</summary>
        Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesForInstructorAsync(int instructorId, DateTime from, DateTime to);
        /// <summary>روزهای منطقی Daily دوره‌های فعال برای دانش‌آموز. روز لغو و دوره غیرفعال نیست.</summary>
        Task<List<AcademyLogicalSessionDto>> ListDailyOccurrencesForStudentAsync(DateTime from, DateTime to, int? courseId = null);
    }
}
