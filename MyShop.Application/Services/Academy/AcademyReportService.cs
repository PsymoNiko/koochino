using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyReportService : IAcademyReportService
    {
        private readonly IAcademyReportRepository _repo;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyActivityRepository _activities;
        private readonly IAcademyInstructorRepository _instructors;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;
        public AcademyReportService(
            IAcademyReportRepository repo,
            IAcademyCourseRepository courses = null,
            IAcademyActivityRepository activities = null,
            IAcademyInstructorRepository instructors = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _courses = courses;
            _activities = activities;
            _instructors = instructors;
            _students = students;
            _tenant = tenant;
        }

        public Task<AcademyAdminSummaryDto> GetAdminSummaryAsync()
            => AcademyTenantOwnership.SiteId(_tenant).HasValue
                ? _repo.GetAdminSummaryBySiteAsync(AcademyTenantOwnership.SiteId(_tenant).Value)
                : _repo.GetAdminSummaryAsync();

        public async Task<List<AcademyCourseReportDto>> GetCoursesAsync(int? instructorId = null)
            => await FilterCoursesAsync(await _repo.GetCoursesAsync(instructorId));

        public async Task<List<AcademyTuitionReportDto>> GetTuitionAsync(int? instructorId = null)
            => await FilterCoursesAsync(await _repo.GetTuitionAsync(instructorId), x => x.CourseId);

        public async Task<List<AcademyAttendanceReportDto>> GetAttendanceByCourseAsync(int? instructorId = null)
            => await FilterCoursesAsync(await _repo.GetAttendanceByCourseAsync(instructorId), x => x.CourseId);

        public async Task<List<AcademyActivityReportDto>> GetActivitiesAsync(int? instructorId = null)
        {
            var list = await _repo.GetActivitiesAsync(instructorId) ?? new List<AcademyActivityReportDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyActivityReportDto>();
            foreach (var item in list)
                if (await AcademyTenantOwnership.ActivityAsync(_tenant, _activities, item.ActivityId)) result.Add(item);
            return result;
        }

        public async Task<List<AcademyInstructorReportDto>> GetInstructorsAsync()
        {
            var list = await _repo.GetInstructorsAsync() ?? new List<AcademyInstructorReportDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyInstructorReportDto>();
            foreach (var item in list)
                if (await AcademyTenantOwnership.InstructorAsync(_tenant, _instructors, item.InstructorId)) result.Add(item);
            return result;
        }

        public async Task<AcademyInstructorSummaryDto> GetInstructorSummaryAsync(int instructorId)
            => await AcademyTenantOwnership.InstructorAsync(_tenant, _instructors, instructorId)
                ? await _repo.GetInstructorSummaryAsync(instructorId)
                : new AcademyInstructorSummaryDto();

        public async Task<AcademyStudentSummaryDto> GetStudentSummaryAsync(int studentId)
            => await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId)
                ? await _repo.GetStudentSummaryAsync(studentId)
                : new AcademyStudentSummaryDto();

        public async Task<AcademyDailyReportSummaryDto> GetDailySummaryAsync(int? instructorId = null)
        {
            var siteId = AcademyTenantOwnership.SiteId(_tenant);
            if (siteId.HasValue)
                return await _repo.GetDailySummaryBySiteAsync(siteId.Value, instructorId);
            return await _repo.GetDailySummaryAsync(instructorId);
        }

        public async Task<List<AcademyDailyByDateReportDto>> GetDailyByDateAsync(
            DateTime fromDate, DateTime toDate, int? courseId = null, int? instructorId = null)
            => await FilterCoursesAsync(
                await _repo.GetDailyByDateAsync(fromDate, toDate, courseId, instructorId), x => x.CourseId);

        public async Task<List<AcademyDailyByCourseReportDto>> GetDailyByCourseAsync(int? instructorId = null)
            => await FilterCoursesAsync(await _repo.GetDailyByCourseAsync(instructorId), x => x.CourseId);

        private async Task<List<AcademyCourseReportDto>> FilterCoursesAsync(List<AcademyCourseReportDto> list)
            => await FilterCoursesAsync(list, x => x.CourseId);

        private async Task<List<T>> FilterCoursesAsync<T>(List<T> list, System.Func<T, int> courseId)
        {
            list ??= new List<T>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<T>();
            foreach (var item in list)
                if (await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId(item))) result.Add(item);
            return result;
        }
    }
}
