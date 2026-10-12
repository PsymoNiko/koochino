using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyCertificateService : IAcademyCertificateService
    {
        private readonly IAcademyCertificateRepository _repo;
        private readonly IAcademyNotificationService _notify;
        private readonly IAcademyEnrollmentRepository _enrollments;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;

        public AcademyCertificateService(
            IAcademyCertificateRepository repo,
            IAcademyNotificationService notify,
            IAcademyEnrollmentRepository enrollments = null,
            IAcademyCourseRepository courses = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _notify = notify;
            _enrollments = enrollments;
            _courses = courses;
            _students = students;
            _tenant = tenant;
        }

        public async Task<List<AcademyCertificateDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, string status = null)
        {
            if (studentId.HasValue && !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId.Value))
                return new List<AcademyCertificateDto>();
            if (courseId.HasValue && !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId.Value))
                return new List<AcademyCertificateDto>();
            var list = await _repo.GetAllAsync(studentId, instructorId, courseId, status)
                ?? new List<AcademyCertificateDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyCertificateDto>();
            foreach (var item in list)
                if (await OwnsAsync(item)) result.Add(item);
            return result;
        }

        public async Task<AcademyCertificateDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return await OwnsAsync(item) ? item : null;
        }

        public async Task<AcademyCertificateDto> GetByEnrollmentAsync(int enrollmentId)
        {
            if (!await OwnsEnrollmentAsync(enrollmentId)) return null;
            var item = await _repo.GetByEnrollmentAsync(enrollmentId);
            return await OwnsAsync(item) ? item : null;
        }

        public async Task<AcademyActionResult> IssueAsync(int enrollmentId, int? actingAdminUserId = null, int? actingInstructorId = null, string notes = null)
        {
            if (enrollmentId <= 0) return AcademyActionResult.Fail("ثبت‌نام نامعتبر است.");
            if (!await OwnsEnrollmentAsync(enrollmentId))
                return AcademyActionResult.Fail("ثبت‌نام یافت نشد.");

            var before = await _repo.GetByEnrollmentAsync(enrollmentId);
            var id = await _repo.IssueAsync(enrollmentId, actingAdminUserId, actingInstructorId, notes);
            if (id <= 0)
            {
                return id switch
                {
                    -2 => AcademyActionResult.Fail("ثبت‌نام یافت نشد."),
                    -7 => AcademyActionResult.Fail("فقط دوره تکمیل‌شده گواهی می‌گیرد. ابتدا وضعیت ثبت‌نام را «تکمیل» کنید."),
                    -8 => AcademyActionResult.Fail("شهریه این دوره پرداخت نشده است."),
                    -10 => AcademyActionResult.Fail("فقط مربی همین دوره می‌تواند گواهی صادر کند."),
                    _ => AcademyActionResult.Fail("صدور گواهی ناموفق بود.")
                };
            }

            var isNew = before == null || !before.IsIssued;
            if (isNew)
                await SafeNotify(id);

            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> RevokeAsync(int id, string reason = null)
        {
            if (id <= 0) return AcademyActionResult.Fail("شناسه نامعتبر است.");
            var item = await _repo.GetByIdAsync(id);
            if (!await OwnsAsync(item))
                return AcademyActionResult.Fail("گواهی صادرشده‌ای برای ابطال یافت نشد.");
            var ok = await _repo.RevokeAsync(id, reason);
            return ok ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("گواهی صادرشده‌ای برای ابطال یافت نشد.");
        }

        private async Task SafeNotify(int certificateId)
        {
            try { await _notify.NotifyCertificateIssuedAsync(certificateId); }
            catch { /* SMS must not block issue */ }
        }

        private Task<bool> OwnsEnrollmentAsync(int enrollmentId)
            => AcademyTenantOwnership.EnrollmentAsync(
                _tenant, _enrollments, _courses, _students, enrollmentId);

        private async Task<bool> OwnsAsync(AcademyCertificateDto item)
        {
            if (item == null) return false;
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return true;
            return await AcademyTenantOwnership.CourseAsync(_tenant, _courses, item.CourseId)
                && await AcademyTenantOwnership.StudentAsync(_tenant, _students, item.StudentId);
        }
    }
}
