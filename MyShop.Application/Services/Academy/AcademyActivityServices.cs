using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyActivityService : IAcademyActivityService
    {
        private readonly IAcademyActivityRepository _repo;
        private readonly ICurrentTenant _tenant;
        private readonly IAcademyActivityCategoryRepository _categories;
        public AcademyActivityService(
            IAcademyActivityRepository repo,
            ICurrentTenant tenant = null,
            IAcademyActivityCategoryRepository categories = null)
        {
            _repo = repo;
            _tenant = tenant;
            _categories = categories;
        }

        public Task<List<AcademyActivityDto>> GetAllAsync(int? siteId = null, int? categoryId = null, bool? isActive = null)
            => _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant) ?? siteId, categoryId, isActive);

        public async Task<AcademyActivityDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return Owns(item?.SiteId) ? item : null;
        }

        public async Task<AcademyActionResult> CreateAsync(CreateAcademyActivityDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return AcademyActionResult.Fail("عنوان الزامی است.");
            if (dto.CategoryId <= 0) return AcademyActionResult.Fail("دسته فعالیت الزامی است.");
            if (!await OwnsCategoryAsync(dto.CategoryId)) return AcademyActionResult.Fail("دسته یافت نشد.");
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            var id = await _repo.InsertAsync(dto);
            if (id == -2) return AcademyActionResult.Fail("دسته یافت نشد.");
            if (id <= 0) return AcademyActionResult.Fail("خطا در ایجاد فعالیت.");
            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> UpdateAsync(UpdateAcademyActivityDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return AcademyActionResult.Fail("عنوان الزامی است.");
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (!Owns(existing?.SiteId)) return AcademyActionResult.Fail("فعالیت یافت نشد.");
            if (!await OwnsCategoryAsync(dto.CategoryId)) return AcademyActionResult.Fail("دسته یافت نشد.");
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            var code = await _repo.UpdateAsync(dto);
            if (code == -2) return AcademyActionResult.Fail("دسته یافت نشد.");
            if (code != 1) return AcademyActionResult.Fail("به‌روزرسانی ناموفق بود.");
            return AcademyActionResult.Success(dto.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            return Owns(existing?.SiteId) && await _repo.DeleteAsync(id);
        }

        private bool Owns(int? siteId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            return !current.HasValue || siteId == current;
        }

        private async Task<bool> OwnsCategoryAsync(int categoryId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue) return true;
            if (_categories == null || categoryId <= 0) return false;
            var category = await _categories.GetByIdAsync(categoryId);
            return category?.SiteId == current;
        }
    }

    public class AcademyActivitySessionService : IAcademyActivitySessionService
    {
        private readonly IAcademyActivitySessionRepository _repo;
        private readonly IAcademyActivityRepository _activities;
        private readonly ICurrentTenant _tenant;
        private readonly IAcademyLocationRepository _locations;
        private readonly IAcademyInstructorRepository _instructors;
        public AcademyActivitySessionService(
            IAcademyActivitySessionRepository repo,
            IAcademyActivityRepository activities = null,
            ICurrentTenant tenant = null,
            IAcademyLocationRepository locations = null,
            IAcademyInstructorRepository instructors = null)
        {
            _repo = repo;
            _activities = activities;
            _tenant = tenant;
            _locations = locations;
            _instructors = instructors;
        }

        public async Task<List<AcademyActivitySessionDto>> GetAllAsync(int? activityId = null, int? instructorId = null, string status = null)
        {
            var list = await _repo.GetAllAsync(activityId, instructorId, status);
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || _activities == null) return list;
            var allowed = (await _activities.GetAllAsync(current)).Select(x => x.Id).ToHashSet();
            return list.Where(x => allowed.Contains(x.ActivityId)).ToList();
        }

        public async Task<AcademyActivitySessionDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return await OwnsActivityAsync(item?.ActivityId) ? item : null;
        }

        public async Task<AcademyActionResult> CreateAsync(CreateAcademyActivitySessionDto dto)
        {
            if (dto.ActivityId <= 0) return AcademyActionResult.Fail("فعالیت الزامی است.");
            if (dto.StartsAt == default) return AcademyActionResult.Fail("زمان شروع الزامی است.");
            if (!await OwnsActivityAsync(dto.ActivityId)) return AcademyActionResult.Fail("فعالیت یافت نشد.");
            if (dto.LocationId.HasValue && dto.LocationId <= 0) dto.LocationId = null;
            if (dto.InstructorId.HasValue && dto.InstructorId <= 0) dto.InstructorId = null;
            if (!await OwnsReferencesAsync(dto.LocationId, dto.InstructorId))
                return AcademyActionResult.Fail("مکان یا مربی یافت نشد.");
            var id = await _repo.InsertAsync(dto);
            if (id == -2) return AcademyActionResult.Fail("فعالیت یافت نشد.");
            if (id <= 0) return AcademyActionResult.Fail("خطا در ایجاد جلسه.");
            return AcademyActionResult.Success(id);
        }

        public async Task<bool> UpdateAsync(UpdateAcademyActivitySessionDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (!await OwnsActivityAsync(existing?.ActivityId) || !await OwnsActivityAsync(dto.ActivityId))
                return false;
            if (dto.LocationId.HasValue && dto.LocationId <= 0) dto.LocationId = null;
            if (dto.InstructorId.HasValue && dto.InstructorId <= 0) dto.InstructorId = null;
            if (!await OwnsReferencesAsync(dto.LocationId, dto.InstructorId))
                return false;
            return await _repo.UpdateAsync(dto);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            return await OwnsActivityAsync(existing?.ActivityId) && await _repo.DeleteAsync(id);
        }

        private async Task<bool> OwnsActivityAsync(int? activityId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || _activities == null) return true;
            if (!activityId.HasValue || activityId.Value <= 0) return false;
            var activity = await _activities.GetByIdAsync(activityId.Value);
            return activity?.SiteId == current;
        }

        private async Task<bool> OwnsReferencesAsync(int? locationId, int? instructorId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue) return true;
            if (locationId.HasValue)
            {
                if (_locations == null) return false;
                var location = await _locations.GetByIdAsync(locationId.Value);
                if (location?.SiteId != current) return false;
            }
            if (instructorId.HasValue)
            {
                if (_instructors == null) return false;
                var instructor = await _instructors.GetByIdAsync(instructorId.Value);
                if (instructor?.SiteId != current) return false;
            }
            return true;
        }
    }

    public class AcademyActivityRequestService : IAcademyActivityRequestService
    {
        private readonly IAcademyActivityRequestRepository _repo;
        private readonly IAcademyInstructorRepository _instructors;
        private readonly IAcademyNotificationService _notify;
        private readonly IAcademyActivitySessionRepository _sessions;
        private readonly IAcademyActivityRepository _activities;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;

        public AcademyActivityRequestService(
            IAcademyActivityRequestRepository repo,
            IAcademyInstructorRepository instructors,
            IAcademyNotificationService notify,
            IAcademyActivitySessionRepository sessions = null,
            IAcademyActivityRepository activities = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _instructors = instructors;
            _notify = notify;
            _sessions = sessions;
            _activities = activities;
            _students = students;
            _tenant = tenant;
        }

        public async Task<List<AcademyActivityRequestDto>> GetAllAsync(int? sessionId = null, int? studentId = null, int? instructorId = null, string status = null)
        {
            var list = await _repo.GetAllAsync(sessionId, studentId, instructorId, status);
            var allowed = await AllowedActivityIdsAsync();
            return allowed == null ? list : list.Where(x => allowed.Contains(x.ActivityId)).ToList();
        }

        public async Task<AcademyActivityRequestDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item != null && await OwnsActivityAsync(item.ActivityId) ? item : null;
        }

        public async Task<AcademyActionResult> CreateAsync(CreateAcademyActivityRequestDto dto)
        {
            if (dto.SessionId <= 0 || dto.StudentId <= 0)
                return AcademyActionResult.Fail("جلسه و دانش‌آموز الزامی است.");
            if (!await OwnsSessionAsync(dto.SessionId) || !await OwnsStudentAsync(dto.StudentId))
                return AcademyActionResult.Fail("جلسه یا دانش‌آموز یافت نشد.");
            var id = await _repo.InsertAsync(dto.SessionId, dto.StudentId, dto.StudentNote);
            if (id <= 0)
            {
                return id switch
                {
                    -1 => AcademyActionResult.Fail("برای این جلسه قبلاً درخواست ثبت شده است."),
                    -2 => AcademyActionResult.Fail("جلسه برای ثبت‌نام باز نیست."),
                    -3 => AcademyActionResult.Fail("دانش‌آموز فعال یافت نشد."),
                    -4 => AcademyActionResult.Fail("ظرفیت جلسه تکمیل است."),
                    _ => AcademyActionResult.Fail("خطا در ثبت درخواست.")
                };
            }
            try { await _notify.NotifyActivityRequestRegisteredAsync(id); } catch { }
            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> ReviewAsync(ReviewAcademyActivityRequestDto dto, int? actingInstructorId = null)
        {
            if (dto.Id <= 0) return AcademyActionResult.Fail("شناسه نامعتبر است.");
            if (string.IsNullOrWhiteSpace(dto.Status)) return AcademyActionResult.Fail("وضعیت الزامی است.");
            var scopedRequest = await _repo.GetByIdAsync(dto.Id);
            if (scopedRequest == null || !await OwnsActivityAsync(scopedRequest.ActivityId))
                return AcademyActionResult.Fail("درخواست یافت نشد.");

            if (actingInstructorId.HasValue && actingInstructorId.Value > 0)
            {
                var instructor = await _instructors.GetByIdAsync(actingInstructorId.Value);
                if (!Owns(instructor?.SiteId))
                    return AcademyActionResult.Fail("شما مجاز به بررسی این درخواست نیستید.");

                var cats = await _instructors.GetCategoryIdsAsync(actingInstructorId.Value);
                var allowed = (scopedRequest.SessionInstructorId == actingInstructorId)
                    || cats.Contains(scopedRequest.CategoryId);
                // parent category skill also allowed via SQL list filter; double-check leaf or parent:
                if (!allowed)
                {
                    // allow if instructor has parent of category — re-query via list scoped to instructor
                    var visible = await _repo.GetAllAsync(instructorId: actingInstructorId.Value);
                    allowed = visible.Exists(x => x.Id == dto.Id);
                }
                if (!allowed)
                    return AcademyActionResult.Fail("شما مجاز به بررسی این درخواست نیستید.");

                dto.ReviewedByInstructorId = actingInstructorId;
            }

            var code = await _repo.ReviewAsync(dto);
            if (code != 1)
            {
                return code switch
                {
                    -2 => AcademyActionResult.Fail("وضعیت نامعتبر است."),
                    -4 => AcademyActionResult.Fail("ظرفیت جلسه تکمیل است."),
                    _ => AcademyActionResult.Fail("بررسی درخواست ناموفق بود.")
                };
            }
            try { await _notify.NotifyActivityRequestReviewedAsync(dto.Id); } catch { }
            return AcademyActionResult.Success(dto.Id);
        }

        private async Task<HashSet<int>> AllowedActivityIdsAsync()
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || _activities == null) return null;
            return (await _activities.GetAllAsync(current)).Select(x => x.Id).ToHashSet();
        }

        private async Task<bool> OwnsActivityAsync(int activityId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || _activities == null) return true;
            var activity = await _activities.GetByIdAsync(activityId);
            return activity?.SiteId == current;
        }

        private async Task<bool> OwnsSessionAsync(int sessionId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || _sessions == null || _activities == null) return true;
            var session = await _sessions.GetByIdAsync(sessionId);
            return session != null && await OwnsActivityAsync(session.ActivityId);
        }

        private async Task<bool> OwnsStudentAsync(int studentId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue || _students == null) return true;
            var student = await _students.GetByIdAsync(studentId);
            return student?.SiteId == current;
        }

        private bool Owns(int? siteId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            return !current.HasValue || siteId == current;
        }
    }
}
