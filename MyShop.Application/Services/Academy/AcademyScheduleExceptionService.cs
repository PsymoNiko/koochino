using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyScheduleExceptionService : IAcademyScheduleExceptionService
    {
        private readonly IAcademyScheduleExceptionRepository _repo;
        private readonly IAcademyCourseRepository _courses;
        private readonly ICurrentTenant _tenant;

        public AcademyScheduleExceptionService(
            IAcademyScheduleExceptionRepository repo,
            IAcademyCourseRepository courses,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _courses = courses;
            _tenant = tenant;
        }

        public async Task<List<AcademyScheduleExceptionDto>> ListByCourseAsync(
            int courseId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (courseId <= 0 || !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId))
                return new List<AcademyScheduleExceptionDto>();
            return await _repo.ListAsync(courseId: courseId, fromDate: fromDate, toDate: toDate);
        }

        public async Task<List<AcademyScheduleExceptionDto>> ListByScheduleAsync(
            int scheduleId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (scheduleId <= 0 || !await AcademyTenantOwnership.ScheduleAsync(_tenant, _courses, scheduleId))
                return new List<AcademyScheduleExceptionDto>();
            return await _repo.ListAsync(scheduleId: scheduleId, fromDate: fromDate, toDate: toDate);
        }

        public async Task<AcademyScheduleExceptionDto> GetByScheduleDateAsync(int scheduleId, DateTime exceptionDate)
            => await AcademyTenantOwnership.ScheduleAsync(_tenant, _courses, scheduleId)
                ? await _repo.GetByScheduleDateAsync(scheduleId, exceptionDate)
                : null;

        public async Task<AcademyActionResult> UpsertAsync(
            UpsertAcademyScheduleExceptionDto dto, int? actingInstructorId = null, int? actingAdminUserId = null)
        {
            if (dto == null || dto.ScheduleId <= 0)
                return AcademyActionResult.Fail("برنامه نامعتبر است.");
            if (!await AcademyTenantOwnership.ScheduleAsync(_tenant, _courses, dto.ScheduleId))
                return AcademyActionResult.Fail("برنامه روزانه معتبر یافت نشد.");

            var instructorId = actingInstructorId.GetValueOrDefault();
            var adminUserId = actingAdminUserId.GetValueOrDefault();
            if (instructorId <= 0 && adminUserId <= 0)
                return AcademyActionResult.Fail("شما مجاز به ثبت استثنا نیستید.");

            var schedule = await _courses.GetScheduleByIdAsync(dto.ScheduleId);
            if (schedule == null || !AcademyDailyScheduleResolver.IsDaily(schedule))
                return AcademyActionResult.Fail("برنامه روزانه معتبر یافت نشد.");
            if (!AcademyDailyScheduleResolver.HasValidDailyRange(schedule)
                || dto.ExceptionDate.Date < schedule.StartDate.Value.Date
                || dto.ExceptionDate.Date > schedule.EndDate.Value.Date)
                return AcademyActionResult.Fail("این تاریخ در بازه برنامه روزانه نیست.");

            if (instructorId > 0)
            {
                var course = await _courses.GetByIdAsync(schedule.CourseId);
                if (course == null || course.InstructorId != instructorId)
                    return AcademyActionResult.Fail("شما مجاز به ثبت استثنا برای این دوره نیستید.");
            }

            if (dto.Capacity.HasValue && dto.Capacity.Value <= 0)
                return AcademyActionResult.Fail("ظرفیت استثنا باید خالی یا بزرگ‌تر از صفر باشد.");
            if (dto.Hour.HasValue && (dto.Hour.Value < 0 || dto.Hour.Value > 23))
                return AcademyActionResult.Fail("ساعت نامعتبر است.");
            if (dto.Minute.HasValue && (dto.Minute.Value < 0 || dto.Minute.Value > 59))
                return AcademyActionResult.Fail("دقیقه نامعتبر است.");

            var id = await _repo.UpsertAsync(dto);
            if (id <= 0)
            {
                return id switch
                {
                    -2 => AcademyActionResult.Fail("این تاریخ در بازه برنامه روزانه نیست."),
                    -4 => AcademyActionResult.Fail("ظرفیت استثنا نامعتبر است."),
                    _ => AcademyActionResult.Fail("ذخیره استثنا ناموفق بود.")
                };
            }

            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> DeleteAsync(
            int id, int? actingInstructorId = null, int? actingAdminUserId = null)
        {
            if (id <= 0)
                return AcademyActionResult.Fail("شناسه نامعتبر است.");

            var instructorId = actingInstructorId.GetValueOrDefault();
            var adminUserId = actingAdminUserId.GetValueOrDefault();
            if (instructorId <= 0 && adminUserId <= 0)
                return AcademyActionResult.Fail("شما مجاز به حذف استثنا نیستید.");

            var item = await _repo.GetByIdAsync(id);
            if (item == null || !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, item.CourseId))
                return AcademyActionResult.Fail("استثنا یافت نشد.");

            if (instructorId > 0)
            {
                var course = await _courses.GetByIdAsync(item.CourseId);
                if (course == null || course.InstructorId != instructorId)
                    return AcademyActionResult.Fail("شما مجاز به حذف این استثنا نیستید.");
            }

            var ok = await _repo.DeleteAsync(id);
            return ok ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("حذف استثنا ناموفق بود.");
        }
    }
}
