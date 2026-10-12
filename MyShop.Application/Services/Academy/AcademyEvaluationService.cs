using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyEvaluationService : IAcademyEvaluationService
    {
        private readonly IAcademyEvaluationRepository _repo;
        private readonly IAcademyPointsService _points;
        private readonly IAcademyEnrollmentRepository _enrollments;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyStudentRepository _students;
        private readonly IAcademyInstructorRepository _instructors;
        private readonly ICurrentTenant _tenant;

        public AcademyEvaluationService(
            IAcademyEvaluationRepository repo,
            IAcademyPointsService points,
            IAcademyEnrollmentRepository enrollments = null,
            IAcademyCourseRepository courses = null,
            IAcademyStudentRepository students = null,
            IAcademyInstructorRepository instructors = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _points = points;
            _enrollments = enrollments;
            _courses = courses;
            _students = students;
            _instructors = instructors;
            _tenant = tenant;
        }

        public async Task<List<AcademyEvaluationDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, bool publishedOnly = false)
        {
            if (studentId.HasValue && !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId.Value))
                return new List<AcademyEvaluationDto>();
            if (courseId.HasValue && !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId.Value))
                return new List<AcademyEvaluationDto>();
            if (instructorId.HasValue && !await AcademyTenantOwnership.InstructorAsync(_tenant, _instructors, instructorId.Value))
                return new List<AcademyEvaluationDto>();
            var list = await _repo.GetAllAsync(studentId, instructorId, courseId, publishedOnly)
                ?? new List<AcademyEvaluationDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyEvaluationDto>();
            foreach (var item in list)
                if (await OwnsAsync(item)) result.Add(item);
            return result;
        }

        public async Task<AcademyEvaluationDto> GetByEnrollmentAsync(int enrollmentId)
        {
            if (!await OwnsEnrollmentAsync(enrollmentId)) return null;
            var item = await _repo.GetByEnrollmentAsync(enrollmentId);
            return await OwnsAsync(item) ? item : null;
        }

        public async Task<AcademyActionResult> UpsertAsync(UpsertAcademyEvaluationDto dto)
        {
            if (dto.EnrollmentId <= 0 || dto.StudentId <= 0)
                return AcademyActionResult.Fail("ثبت‌نام نامعتبر است.");
            if (!await OwnsEnrollmentAsync(dto.EnrollmentId)
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, dto.StudentId))
                return AcademyActionResult.Fail("ثبت‌نام نامعتبر است.");
            if (dto.CourseRating < 1 || dto.CourseRating > 5 || dto.InstructorRating < 1 || dto.InstructorRating > 5)
                return AcademyActionResult.Fail("امتیاز باید بین ۱ تا ۵ باشد.");
            if (dto.Comment != null && dto.Comment.Length > 1000)
                dto.Comment = dto.Comment.Substring(0, 1000);

            var before = await _repo.GetByEnrollmentAsync(dto.EnrollmentId);
            var id = await _repo.UpsertAsync(dto);
            if (id <= 0)
            {
                return id switch
                {
                    -2 => AcademyActionResult.Fail("ثبت‌نام یافت نشد."),
                    -3 => AcademyActionResult.Fail("این ارزیابی متعلق به شما نیست."),
                    -7 => AcademyActionResult.Fail("فقط پس از تکمیل دوره می‌توانید ارزیابی ثبت کنید."),
                    -11 => AcademyActionResult.Fail("امتیاز نامعتبر است."),
                    _ => AcademyActionResult.Fail("ثبت ارزیابی ناموفق بود.")
                };
            }

            if (before == null)
            {
                try { await _points.AwardOnceAsync(dto.StudentId, AcademyConstants.PointReason.Evaluation, "Evaluation", id, 20); }
                catch { /* points optional until Phase 16 SQL */ }
            }

            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> SetPublishedAsync(int id, bool isPublished)
        {
            var item = (await _repo.GetAllAsync()).FirstOrDefault(x => x.Id == id);
            if (!await OwnsAsync(item))
                return AcademyActionResult.Fail("ارزیابی یافت نشد.");
            var ok = await _repo.SetPublishedAsync(id, isPublished);
            return ok ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("ارزیابی یافت نشد.");
        }

        public async Task<List<AcademyEvaluationCourseSummaryDto>> GetCourseSummaryAsync(int? instructorId = null)
        {
            if (instructorId.HasValue && !await AcademyTenantOwnership.InstructorAsync(_tenant, _instructors, instructorId.Value))
                return new List<AcademyEvaluationCourseSummaryDto>();
            var list = await _repo.GetCourseSummaryAsync(instructorId) ?? new List<AcademyEvaluationCourseSummaryDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyEvaluationCourseSummaryDto>();
            foreach (var item in list)
                if (await AcademyTenantOwnership.CourseAsync(_tenant, _courses, item.CourseId)) result.Add(item);
            return result;
        }

        public async Task<List<AcademyEvaluationInstructorSummaryDto>> GetInstructorSummaryAsync()
        {
            var list = await _repo.GetInstructorSummaryAsync() ?? new List<AcademyEvaluationInstructorSummaryDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyEvaluationInstructorSummaryDto>();
            foreach (var item in list)
                if (await AcademyTenantOwnership.InstructorAsync(_tenant, _instructors, item.InstructorId)) result.Add(item);
            return result;
        }

        private Task<bool> OwnsEnrollmentAsync(int enrollmentId)
            => AcademyTenantOwnership.EnrollmentAsync(
                _tenant, _enrollments, _courses, _students, enrollmentId);

        private async Task<bool> OwnsAsync(AcademyEvaluationDto item)
        {
            if (item == null) return false;
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return true;
            return await AcademyTenantOwnership.CourseAsync(_tenant, _courses, item.CourseId)
                && await AcademyTenantOwnership.StudentAsync(_tenant, _students, item.StudentId);
        }
    }
}
