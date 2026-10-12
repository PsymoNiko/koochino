using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyCourseScheduleRequestService : IAcademyCourseScheduleRequestService
    {
        private readonly IAcademyCourseScheduleRequestRepository _repo;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyStudentRepository _students;
        private readonly IAcademyScheduleExceptionRepository _exceptions;
        private readonly ICurrentTenant _tenant;

        public AcademyCourseScheduleRequestService(
            IAcademyCourseScheduleRequestRepository repo,
            IAcademyCourseRepository courses,
            IAcademyStudentRepository students,
            IAcademyScheduleExceptionRepository exceptions = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _courses = courses;
            _students = students;
            _exceptions = exceptions;
            _tenant = tenant;
        }

        public async Task<List<AcademyCourseScheduleRequestDto>> ListMineAsync(
            int actingStudentId, int? courseId = null, DateTime? fromDate = null, DateTime? toDate = null, string status = null)
        {
            if (actingStudentId <= 0)
                return new List<AcademyCourseScheduleRequestDto>();
            if (!await AcademyTenantOwnership.StudentAsync(_tenant, _students, actingStudentId))
                return new List<AcademyCourseScheduleRequestDto>();
            var list = await _repo.ListAsync(
                studentId: actingStudentId,
                courseId: courseId,
                status: status,
                fromDate: fromDate,
                toDate: toDate) ?? new List<AcademyCourseScheduleRequestDto>();
            return await FilterOwnedAsync(list);
        }

        public async Task<AcademyCourseScheduleRequestDto> GetMineByIdAsync(int id, int actingStudentId)
        {
            if (id <= 0 || actingStudentId <= 0)
                return null;
            var item = await _repo.GetByIdAsync(id);
            if (item == null || item.StudentId != actingStudentId)
                return null;
            if (!await OwnsAsync(item))
                return null;
            return item;
        }

        public async Task<AcademyActionResult> CreateAsync(CreateAcademyCourseScheduleRequestDto dto, int actingStudentId)
        {
            if (dto == null || actingStudentId <= 0 || dto.StudentId <= 0 || dto.ScheduleId <= 0)
                return AcademyActionResult.Fail("درخواست نامعتبر است.");
            if (dto.StudentId != actingStudentId)
                return AcademyActionResult.Fail("شما مجاز به ثبت درخواست برای دانش‌آموز دیگر نیستید.");

            var student = await _students.GetByIdAsync(actingStudentId);
            if (student == null
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, actingStudentId)
                || !string.Equals(student.Status, AcademyConstants.StudentStatus.Active, StringComparison.OrdinalIgnoreCase))
                return AcademyActionResult.Fail("دانش‌آموز فعال یافت نشد.");

            var schedule = await _courses.GetScheduleByIdAsync(dto.ScheduleId);
            if (schedule == null || !AcademyDailyScheduleResolver.IsDaily(schedule))
                return AcademyActionResult.Fail("برنامه روزانه معتبر یافت نشد.");

            var course = await _courses.GetByIdAsync(schedule.CourseId);
            if (course == null
                || !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, course.Id)
                || !course.IsActive)
                return AcademyActionResult.Fail("دوره فعال یافت نشد.");

            var exception = _exceptions == null
                ? null
                : await _exceptions.GetByScheduleDateAsync(dto.ScheduleId, dto.SessionDate.Date);
            if (exception != null && exception.IsCancelled)
                return AcademyActionResult.Fail("این روز لغو شده است.");
            if (!AcademyDailyScheduleResolver.TryResolve(schedule, dto.SessionDate, exception, out var occurrence))
                return AcademyActionResult.Fail("این تاریخ در بازه برنامه روزانه نیست.");

            var approved = await _repo.CountApprovedAsync(dto.ScheduleId, dto.SessionDate.Date);
            var waitlisted = AcademyDailyCapacity.IsFull(occurrence.Capacity, approved);
            var insertStatus = waitlisted
                ? AcademyConstants.RequestStatus.Waitlisted
                : AcademyConstants.RequestStatus.Pending;

            var id = await _repo.InsertAsync(
                dto.ScheduleId, actingStudentId, dto.SessionDate.Date, dto.StudentNote, insertStatus);
            if (id <= 0)
            {
                return id switch
                {
                    -1 => AcademyActionResult.Fail("برای این روز قبلاً درخواست ثبت شده است."),
                    -2 => AcademyActionResult.Fail("این تاریخ در بازه برنامه روزانه نیست."),
                    -3 => AcademyActionResult.Fail("دانش‌آموز فعال یافت نشد."),
                    -5 => AcademyActionResult.Fail("برنامه روزانه معتبر یافت نشد."),
                    -6 => AcademyActionResult.Fail("دوره فعال یافت نشد."),
                    -4 => AcademyActionResult.Fail("ظرفیت این روز تکمیل است. اسکریپت sql/Academy_Phase27_DailyWaitlist.sql را اجرا کنید."),
                    -8 => AcademyActionResult.Fail("این روز لغو شده است."),
                    _ => AcademyActionResult.Fail("خطا در ثبت درخواست.")
                };
            }

            return waitlisted
                ? AcademyActionResult.Success(id, message: "ظرفیت این روز تکمیل است؛ در لیست انتظار ثبت شدید.")
                : AcademyActionResult.Success(id);
        }

        public async Task<List<AcademyCourseScheduleRequestDto>> ListForInstructorAsync(
            int actingInstructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (actingInstructorId <= 0)
                return new List<AcademyCourseScheduleRequestDto>();
            var list = await _repo.ListByInstructorAsync(actingInstructorId, status, fromDate, toDate)
                ?? new List<AcademyCourseScheduleRequestDto>();
            return await FilterOwnedAsync(list);
        }

        public async Task<List<AcademyCourseScheduleRequestDto>> ListForAdminAsync(
            int? courseId = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (courseId.HasValue && !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId.Value))
                return new List<AcademyCourseScheduleRequestDto>();
            var list = await _repo.ListAsync(courseId: courseId, status: status, fromDate: fromDate, toDate: toDate)
                ?? new List<AcademyCourseScheduleRequestDto>();
            return await FilterOwnedAsync(list);
        }

        public async Task<AcademyActionResult> ReviewAsync(
            ReviewAcademyCourseScheduleRequestDto dto, int? actingInstructorId = null, int? actingAdminUserId = null)
        {
            if (dto == null || dto.Id <= 0)
                return AcademyActionResult.Fail("شناسه نامعتبر است.");

            var status = (dto.Status ?? "").Trim();
            var isApproved = string.Equals(status, AcademyConstants.RequestStatus.Approved, StringComparison.OrdinalIgnoreCase);
            var isRejected = string.Equals(status, AcademyConstants.RequestStatus.Rejected, StringComparison.OrdinalIgnoreCase);
            var isWaitlisted = string.Equals(status, AcademyConstants.RequestStatus.Waitlisted, StringComparison.OrdinalIgnoreCase);
            if (!isApproved && !isRejected && !isWaitlisted)
                return AcademyActionResult.Fail("فقط تأیید، رد یا لیست انتظار مجاز است.");

            if (isRejected && string.IsNullOrWhiteSpace(dto.ReviewerNote))
                return AcademyActionResult.Fail("برای رد درخواست، ذکر دلیل الزامی است.");

            var instructorId = actingInstructorId.GetValueOrDefault();
            var adminUserId = actingAdminUserId.GetValueOrDefault();
            if (instructorId <= 0 && adminUserId <= 0)
                return AcademyActionResult.Fail("شما مجاز به بررسی این درخواست نیستید.");

            var item = await _repo.GetByIdAsync(dto.Id);
            if (item == null || !await OwnsAsync(item))
                return AcademyActionResult.Fail("درخواست یافت نشد.");

            var open = string.Equals(item.Status, AcademyConstants.RequestStatus.Pending, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Status, AcademyConstants.RequestStatus.Waitlisted, StringComparison.OrdinalIgnoreCase);
            if (!open)
                return AcademyActionResult.Fail("این درخواست قبلاً بررسی شده است.");
            if (isWaitlisted && !string.Equals(item.Status, AcademyConstants.RequestStatus.Pending, StringComparison.OrdinalIgnoreCase))
                return AcademyActionResult.Fail("این درخواست قبلاً بررسی شده است.");

            if (instructorId > 0)
            {
                var course = await _courses.GetByIdAsync(item.CourseId);
                if (course == null || course.InstructorId != instructorId)
                    return AcademyActionResult.Fail("شما مجاز به بررسی این درخواست نیستید.");
                dto.ReviewedByInstructorId = instructorId;
                dto.ReviewedByAdminUserId = null;
            }
            else
            {
                dto.ReviewedByInstructorId = null;
                dto.ReviewedByAdminUserId = adminUserId;
            }

            dto.Status = isApproved
                ? AcademyConstants.RequestStatus.Approved
                : isWaitlisted
                    ? AcademyConstants.RequestStatus.Waitlisted
                    : AcademyConstants.RequestStatus.Rejected;
            if (isApproved)
            {
                var schedule = await _courses.GetScheduleByIdAsync(item.ScheduleId);
                var exception = _exceptions == null
                    ? null
                    : await _exceptions.GetByScheduleDateAsync(item.ScheduleId, item.SessionDate.Date);
                if (exception != null && exception.IsCancelled)
                    return AcademyActionResult.Fail("این روز لغو شده است.");
                var cap = AcademyDailyCapacity.Effective(schedule?.Capacity, exception?.Capacity);
                var approved = await _repo.CountApprovedAsync(item.ScheduleId, item.SessionDate.Date);
                if (AcademyDailyCapacity.IsFull(cap, approved))
                    return AcademyActionResult.Fail("ظرفیت این روز تکمیل است.");
            }

            var code = await _repo.ReviewAsync(dto);
            if (code != 1)
            {
                return code switch
                {
                    -2 => AcademyActionResult.Fail("وضعیت نامعتبر است."),
                    -4 => AcademyActionResult.Fail("ظرفیت این روز تکمیل است."),
                    -7 => AcademyActionResult.Fail("این درخواست قبلاً بررسی شده است."),
                    -8 => AcademyActionResult.Fail("این روز لغو شده است."),
                    _ => AcademyActionResult.Fail("بررسی درخواست ناموفق بود.")
                };
            }

            return AcademyActionResult.Success(dto.Id);
        }

        public async Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(
            int requestId, int? actingInstructorId = null, int? actingAdminUserId = null)
        {
            if (requestId <= 0)
                return new List<AcademyCourseScheduleRequestAuditDto>();

            var instructorId = actingInstructorId.GetValueOrDefault();
            var adminUserId = actingAdminUserId.GetValueOrDefault();
            if (instructorId <= 0 && adminUserId <= 0)
                return new List<AcademyCourseScheduleRequestAuditDto>();

            var item = await _repo.GetByIdAsync(requestId);
            if (item == null || !await OwnsAsync(item))
                return new List<AcademyCourseScheduleRequestAuditDto>();

            if (instructorId > 0)
            {
                var course = await _courses.GetByIdAsync(item.CourseId);
                if (course == null || course.InstructorId != instructorId)
                    return new List<AcademyCourseScheduleRequestAuditDto>();
            }

            return await _repo.ListAuditsAsync(requestId) ?? new List<AcademyCourseScheduleRequestAuditDto>();
        }

        public async Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime fromDate, DateTime toDate)
        {
            if (!await AcademyTenantOwnership.ScheduleAsync(_tenant, _courses, scheduleId))
                return new List<AcademyDailyApprovedCountDto>();
            return await _repo.ListApprovedCountsAsync(scheduleId, fromDate, toDate)
                ?? new List<AcademyDailyApprovedCountDto>();
        }

        public async Task HydrateStudentBoardAsync(int actingStudentId, IList<AcademyLogicalSessionDto> days, DateTime from, DateTime to)
        {
            if (days == null || days.Count == 0)
                return;

            var mine = actingStudentId > 0
                ? await ListMineAsync(actingStudentId, fromDate: from.Date, toDate: to.Date) ?? new List<AcademyCourseScheduleRequestDto>()
                : new List<AcademyCourseScheduleRequestDto>();

            foreach (var sid in days.Select(x => x.ScheduleId).Distinct())
            {
                var counts = await ListApprovedCountsAsync(sid, from.Date, to.Date) ?? new List<AcademyDailyApprovedCountDto>();
                foreach (var day in days.Where(d => d.ScheduleId == sid))
                {
                    var hit = counts.FirstOrDefault(c => c.SessionDate.Date == day.SessionDate.Date);
                    day.ApprovedCount = hit?.ApprovedCount ?? 0;
                    var req = mine.FirstOrDefault(x =>
                        x.ScheduleId == sid && x.SessionDate.Date == day.SessionDate.Date);
                    day.MyRequestStatus = req?.Status;
                    day.MyReviewerNote = req?.ReviewerNote;
                }
            }
        }

        private async Task<bool> OwnsAsync(AcademyCourseScheduleRequestDto item)
        {
            if (item == null) return false;
            return await AcademyTenantOwnership.CourseAsync(_tenant, _courses, item.CourseId)
                && await AcademyTenantOwnership.StudentAsync(_tenant, _students, item.StudentId);
        }

        private async Task<List<AcademyCourseScheduleRequestDto>> FilterOwnedAsync(
            IEnumerable<AcademyCourseScheduleRequestDto> items)
        {
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue)
                return items?.ToList() ?? new List<AcademyCourseScheduleRequestDto>();
            var result = new List<AcademyCourseScheduleRequestDto>();
            foreach (var item in items ?? Enumerable.Empty<AcademyCourseScheduleRequestDto>())
                if (await OwnsAsync(item)) result.Add(item);
            return result;
        }
    }
}
