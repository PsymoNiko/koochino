using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyEnrollmentService : IAcademyEnrollmentService
    {
        private readonly IAcademyEnrollmentRepository _repo;
        private readonly IAcademyNotificationService _notify;
        private readonly IAcademyPointsService _points;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;

        public AcademyEnrollmentService(
            IAcademyEnrollmentRepository repo,
            IAcademyNotificationService notify,
            IAcademyPointsService points,
            IAcademyCourseRepository courses = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _notify = notify;
            _points = points;
            _courses = courses;
            _students = students;
            _tenant = tenant;
        }

        public async Task<List<AcademyEnrollmentDto>> GetAllAsync(int? courseId = null, int? studentId = null, int? instructorId = null, string status = null)
        {
            if (courseId.HasValue && !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId.Value))
                return new List<AcademyEnrollmentDto>();
            if (studentId.HasValue && !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId.Value))
                return new List<AcademyEnrollmentDto>();
            var list = await _repo.GetAllAsync(courseId, studentId, instructorId, status)
                ?? new List<AcademyEnrollmentDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyEnrollmentDto>();
            foreach (var item in list)
                if (await OwnsAsync(item)) result.Add(item);
            return result;
        }

        public async Task<AcademyEnrollmentDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return await OwnsAsync(item) ? item : null;
        }

        public async Task<AcademyEnrollmentDto> GetByCourseStudentAsync(int courseId, int studentId)
        {
            if (!await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId)
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId))
                return null;
            var item = await _repo.GetByCourseStudentAsync(courseId, studentId);
            return await OwnsAsync(item) ? item : null;
        }

        public async Task<AcademyActionResult> RegisterAsync(CreateAcademyEnrollmentDto dto)
        {
            if (dto.CourseId <= 0 || dto.StudentId <= 0)
                return AcademyActionResult.Fail("دوره و دانش‌آموز الزامی است.");
            if (!await AcademyTenantOwnership.CourseAsync(_tenant, _courses, dto.CourseId)
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, dto.StudentId))
                return AcademyActionResult.Fail("دوره یا دانش‌آموز یافت نشد.");

            var id = await _repo.RegisterAsync(dto.CourseId, dto.StudentId, dto.StudentNote);
            if (id <= 0)
            {
                return id switch
                {
                    -1 => AcademyActionResult.Fail("برای این دوره قبلاً ثبت‌نام کرده‌اید."),
                    -2 => AcademyActionResult.Fail("دوره فعال یافت نشد."),
                    -3 => AcademyActionResult.Fail("دانش‌آموز فعال یافت نشد."),
                    -5 => AcademyActionResult.Fail("بازه ثبت‌نام هنوز شروع نشده است."),
                    -6 => AcademyActionResult.Fail("بازه ثبت‌نام به پایان رسیده است."),
                    _ => AcademyActionResult.Fail("خطا در ثبت‌نام.")
                };
            }
            await SafeNotify(() => _notify.NotifyEnrollmentRegisteredAsync(id));
            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> ReviewAsync(ReviewAcademyEnrollmentDto dto, int? actingInstructorId = null, int? actingAdminUserId = null)
        {
            if (dto.Id <= 0) return AcademyActionResult.Fail("شناسه نامعتبر است.");
            if (string.IsNullOrWhiteSpace(dto.Status)) return AcademyActionResult.Fail("وضعیت الزامی است.");
            var scopedItem = await _repo.GetByIdAsync(dto.Id);
            if (!await OwnsAsync(scopedItem))
                return AcademyActionResult.Fail("ثبت‌نام یافت نشد.");

            if (actingInstructorId.HasValue && actingInstructorId.Value > 0)
            {
                if (scopedItem.CourseInstructorId != actingInstructorId.Value)
                    return AcademyActionResult.Fail("فقط مربی همین دوره می‌تواند این ثبت‌نام را بررسی کند.");
                dto.ReviewedByInstructorId = actingInstructorId;
            }

            if (actingAdminUserId.HasValue && actingAdminUserId.Value > 0)
                dto.ReviewedByAdminUserId = actingAdminUserId;

            var code = await _repo.ReviewAsync(dto);
            if (code != 1)
            {
                return code switch
                {
                    -2 => AcademyActionResult.Fail("وضعیت نامعتبر است."),
                    -4 => AcademyActionResult.Fail("ظرفیت دوره تکمیل است؛ می‌توانید به لیست انتظار منتقل کنید."),
                    -7 => AcademyActionResult.Fail("فقط ثبت‌نام تأییدشده قابل تکمیل است."),
                    _ => AcademyActionResult.Fail("بررسی ثبت‌نام ناموفق بود.")
                };
            }
            await SafeNotify(() => _notify.NotifyEnrollmentReviewedAsync(dto.Id));
            if (string.Equals(dto.Status, AcademyConstants.EnrollmentStatus.Completed, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var e = await _repo.GetByIdAsync(dto.Id);
                    if (e != null)
                        await _points.AwardOnceAsync(e.StudentId, AcademyConstants.PointReason.CourseCompleted, "Enrollment", e.Id, 50);
                }
                catch { }
            }
            return AcademyActionResult.Success(dto.Id);
        }

        public async Task<AcademyActionResult> CancelByStudentAsync(int id, int studentId)
        {
            if (id <= 0 || studentId <= 0) return AcademyActionResult.Fail("شناسه نامعتبر است.");
            var item = await _repo.GetByIdAsync(id);
            if (!await OwnsAsync(item) || item.StudentId != studentId)
                return AcademyActionResult.Fail("امکان لغو این ثبت‌نام وجود ندارد.");
            var ok = await _repo.CancelByStudentAsync(id, studentId);
            return ok ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("امکان لغو این ثبت‌نام وجود ندارد.");
        }

        private static async Task SafeNotify(Func<Task> action)
        {
            try { await action(); }
            catch { /* SMS must not fail enrollment */ }
        }

        private Task<bool> OwnsAsync(AcademyEnrollmentDto item)
            => AcademyTenantOwnership.EnrollmentDtoAsync(_tenant, _courses, _students, item);
    }
}
