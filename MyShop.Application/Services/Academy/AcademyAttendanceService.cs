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
    public class AcademyAttendanceService : IAcademyAttendanceService
    {
        private readonly IAcademyAttendanceRepository _repo;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyActivitySessionRepository _activitySessions;
        private readonly IAcademyPointsService _points;
        private readonly IAcademyScheduleExceptionRepository _exceptions;
        private readonly IAcademyActivityRepository _activities;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;

        public AcademyAttendanceService(
            IAcademyAttendanceRepository repo,
            IAcademyCourseRepository courses,
            IAcademyActivitySessionRepository activitySessions,
            IAcademyPointsService points,
            IAcademyScheduleExceptionRepository exceptions = null,
            IAcademyActivityRepository activities = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _courses = courses;
            _activitySessions = activitySessions;
            _points = points;
            _exceptions = exceptions;
            _activities = activities;
            _students = students;
            _tenant = tenant;
        }

        public async Task<List<AcademyAttendanceRosterItemDto>> GetRosterAsync(string sessionType, int sessionId)
        {
            if (sessionType == AcademyConstants.AttendanceSessionType.Course)
            {
                if (!await AcademyTenantOwnership.CourseSessionAsync(_tenant, _courses, sessionId))
                    return new List<AcademyAttendanceRosterItemDto>();
                return await _repo.GetCourseSessionRosterAsync(sessionId);
            }
            if (sessionType == AcademyConstants.AttendanceSessionType.Activity)
            {
                if (!await AcademyTenantOwnership.ActivitySessionAsync(_tenant, _activitySessions, _activities, sessionId))
                    return new List<AcademyAttendanceRosterItemDto>();
                return await _repo.GetActivitySessionRosterAsync(sessionId);
            }
            return new List<AcademyAttendanceRosterItemDto>();
        }

        public async Task<List<AcademyAttendanceDto>> GetAllAsync(int? courseSessionId = null, int? activitySessionId = null, int? studentId = null)
        {
            if (courseSessionId.HasValue
                && !await AcademyTenantOwnership.CourseSessionAsync(_tenant, _courses, courseSessionId.Value))
                return new List<AcademyAttendanceDto>();
            if (activitySessionId.HasValue
                && !await AcademyTenantOwnership.ActivitySessionAsync(_tenant, _activitySessions, _activities, activitySessionId.Value))
                return new List<AcademyAttendanceDto>();
            if (studentId.HasValue
                && !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId.Value))
                return new List<AcademyAttendanceDto>();
            var list = await _repo.GetAllAsync(courseSessionId, activitySessionId, studentId)
                ?? new List<AcademyAttendanceDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyAttendanceDto>();
            foreach (var item in list)
                if (await OwnsAsync(item)) result.Add(item);
            return result;
        }

        public async Task<List<AcademyAttendanceRosterItemDto>> GetDailyRosterAsync(
            int scheduleId, DateTime sessionDate, int? actingInstructorId = null)
        {
            if (scheduleId <= 0)
                return new List<AcademyAttendanceRosterItemDto>();
            if (!await AcademyTenantOwnership.ScheduleAsync(_tenant, _courses, scheduleId))
                return new List<AcademyAttendanceRosterItemDto>();
            if (actingInstructorId.GetValueOrDefault() > 0
                && !await InstructorOwnsDailyAsync(scheduleId, sessionDate, actingInstructorId.Value))
                return new List<AcademyAttendanceRosterItemDto>();
            return await _repo.GetDailyRosterAsync(scheduleId, sessionDate.Date)
                ?? new List<AcademyAttendanceRosterItemDto>();
        }

        public async Task<AcademyActionResult> MarkDailyAsync(
            UpsertAcademyAttendanceDto dto, int? actingInstructorId = null, int? actingAdminUserId = null)
        {
            if (dto == null || dto.StudentId <= 0)
                return AcademyActionResult.Fail("دانش‌آموز الزامی است.");
            if (!dto.ScheduleId.HasValue || dto.ScheduleId <= 0 || !dto.SessionDate.HasValue)
                return AcademyActionResult.Fail("برنامه و تاریخ الزامی است.");
            if (!await AcademyTenantOwnership.ScheduleAsync(_tenant, _courses, dto.ScheduleId.Value)
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, dto.StudentId))
                return AcademyActionResult.Fail("برنامه یا دانش‌آموز یافت نشد.");

            dto.CourseSessionId = null;
            dto.ActivitySessionId = null;
            dto.SessionDate = dto.SessionDate.Value.Date;

            var instructorId = actingInstructorId.GetValueOrDefault();
            var adminUserId = actingAdminUserId.GetValueOrDefault();
            if (instructorId <= 0 && adminUserId <= 0)
                return AcademyActionResult.Fail("شما مجاز به ثبت حضورغیاب نیستید.");

            var schedule = await _courses.GetScheduleByIdAsync(dto.ScheduleId.Value);
            if (schedule == null || !AcademyDailyScheduleResolver.IsDaily(schedule))
                return AcademyActionResult.Fail("برنامه روزانه معتبر یافت نشد.");

            var exception = _exceptions == null
                ? null
                : await _exceptions.GetByScheduleDateAsync(dto.ScheduleId.Value, dto.SessionDate.Value);
            if (exception != null && exception.IsCancelled)
                return AcademyActionResult.Fail("این روز لغو شده است.");
            if (!AcademyDailyScheduleResolver.TryResolve(schedule, dto.SessionDate.Value, exception, out _))
                return AcademyActionResult.Fail("این تاریخ در بازه برنامه روزانه نیست.");

            if (instructorId > 0)
            {
                if (!await InstructorOwnsDailyAsync(dto.ScheduleId.Value, dto.SessionDate.Value, instructorId))
                    return AcademyActionResult.Fail("شما مجاز به ثبت حضورغیاب این روز نیستید.");
                dto.MarkedByInstructorId = instructorId;
                dto.MarkedByAdminUserId = null;
            }
            else
            {
                dto.MarkedByInstructorId = null;
                dto.MarkedByAdminUserId = adminUserId;
            }

            if (string.IsNullOrWhiteSpace(dto.Source))
                dto.Source = AcademyConstants.AttendanceSource.Manual;

            var id = await _repo.UpsertDailyAsync(dto);
            if (id > 0 && string.Equals(dto.Status, AcademyConstants.AttendanceStatus.Present, StringComparison.OrdinalIgnoreCase))
            {
                try { await _points.AwardOnceAsync(dto.StudentId, AcademyConstants.PointReason.Attendance, "DailyAttendance", id, 5); }
                catch { }
            }
            return id switch
            {
                -2 => AcademyActionResult.Fail("وضعیت یا منبع نامعتبر است."),
                -3 => AcademyActionResult.Fail("برنامه و تاریخ الزامی است."),
                -4 => AcademyActionResult.Fail("دانش‌آموز فعال یافت نشد."),
                -5 => AcademyActionResult.Fail("این تاریخ در بازه برنامه روزانه نیست."),
                -6 => AcademyActionResult.Fail("این دانش‌آموز برای این روز تأیید نشده است."),
                -8 => AcademyActionResult.Fail("این روز لغو شده است."),
                > 0 => AcademyActionResult.Success(id),
                _ => AcademyActionResult.Fail("ثبت حضورغیاب ناموفق بود.")
            };
        }

        public async Task<AcademyActionResult> MarkDailyBulkAsync(
            int scheduleId,
            DateTime sessionDate,
            IEnumerable<(int StudentId, string Status)> marks,
            int? actingInstructorId = null,
            int? actingAdminUserId = null)
        {
            var list = marks?.ToList() ?? new List<(int, string)>();
            if (scheduleId <= 0 || list.Count == 0)
                return AcademyActionResult.Fail("لیست حضورغیاب خالی است.");

            var ok = 0;
            foreach (var (studentId, status) in list)
            {
                var dto = new UpsertAcademyAttendanceDto
                {
                    StudentId = studentId,
                    Status = status,
                    Source = AcademyConstants.AttendanceSource.Manual,
                    ScheduleId = scheduleId,
                    SessionDate = sessionDate.Date
                };
                var r = await MarkDailyAsync(dto, actingInstructorId, actingAdminUserId);
                if (r.Ok) ok++;
            }
            return ok > 0
                ? AcademyActionResult.Success(ok)
                : AcademyActionResult.Fail("هیچ رکوردی ذخیره نشد.");
        }

        public async Task<AcademyActionResult> MarkAsync(UpsertAcademyAttendanceDto dto, int? actingInstructorId = null, int? actingAdminUserId = null)
        {
            if (dto.StudentId <= 0) return AcademyActionResult.Fail("دانش‌آموز الزامی است.");
            NormalizeSession(dto);
            if (!dto.CourseSessionId.HasValue && !dto.ActivitySessionId.HasValue)
                return AcademyActionResult.Fail("جلسه الزامی است.");
            if (!await StudentAndSessionOwnedAsync(dto))
                return AcademyActionResult.Fail("جلسه یا دانش‌آموز یافت نشد.");

            var instructorId = actingInstructorId.GetValueOrDefault();
            var adminUserId = actingAdminUserId.GetValueOrDefault();
            if (instructorId <= 0 && adminUserId <= 0)
                return AcademyActionResult.Fail("شما مجاز به ثبت حضورغیاب نیستید.");

            if (instructorId > 0)
            {
                if (!await InstructorOwnsSessionAsync(dto.CourseSessionId, dto.ActivitySessionId, instructorId))
                    return AcademyActionResult.Fail("شما مجاز به ثبت حضورغیاب این جلسه نیستید.");
                dto.MarkedByInstructorId = instructorId;
                dto.MarkedByAdminUserId = null;
            }
            else
            {
                dto.MarkedByInstructorId = null;
                dto.MarkedByAdminUserId = adminUserId;
            }

            if (string.IsNullOrWhiteSpace(dto.Source))
                dto.Source = AcademyConstants.AttendanceSource.Manual;

            var id = await _repo.UpsertAsync(dto);
            if (id > 0 && string.Equals(dto.Status, AcademyConstants.AttendanceStatus.Present, StringComparison.OrdinalIgnoreCase))
            {
                var relatedId = dto.CourseSessionId ?? dto.ActivitySessionId ?? 0;
                try { await _points.AwardOnceAsync(dto.StudentId, AcademyConstants.PointReason.Attendance, "Attendance", relatedId, 5); }
                catch { }
            }
            return id switch
            {
                -2 => AcademyActionResult.Fail("وضعیت یا منبع نامعتبر است."),
                -3 => AcademyActionResult.Fail("باید دقیقاً یک نوع جلسه مشخص شود."),
                -4 => AcademyActionResult.Fail("دانش‌آموز فعال یافت نشد."),
                -5 => AcademyActionResult.Fail("جلسه یافت نشد."),
                > 0 => AcademyActionResult.Success(id),
                _ => AcademyActionResult.Fail("ثبت حضورغیاب ناموفق بود.")
            };
        }

        public async Task<AcademyActionResult> MarkBulkAsync(
            string sessionType,
            int sessionId,
            IEnumerable<(int StudentId, string Status)> marks,
            int? actingInstructorId = null,
            int? actingAdminUserId = null)
        {
            var list = marks?.ToList() ?? new List<(int, string)>();
            if (sessionId <= 0 || list.Count == 0)
                return AcademyActionResult.Fail("لیست حضورغیاب خالی است.");

            var ok = 0;
            foreach (var (studentId, status) in list)
            {
                var dto = new UpsertAcademyAttendanceDto
                {
                    StudentId = studentId,
                    Status = status,
                    Source = AcademyConstants.AttendanceSource.Manual,
                    CourseSessionId = sessionType == AcademyConstants.AttendanceSessionType.Course ? sessionId : null,
                    ActivitySessionId = sessionType == AcademyConstants.AttendanceSessionType.Activity ? sessionId : null
                };
                var r = await MarkAsync(dto, actingInstructorId, actingAdminUserId);
                if (r.Ok) ok++;
            }
            return ok > 0
                ? AcademyActionResult.Success(ok)
                : AcademyActionResult.Fail("هیچ رکوردی ذخیره نشد.");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = (await _repo.GetAllAsync()).FirstOrDefault(x => x.Id == id);
            return await OwnsAsync(item) && await _repo.DeleteAsync(id);
        }

        public async Task<AcademyActionResult> CreateQrAsync(
            string sessionType,
            int sessionId,
            int validMinutes = 20,
            int? actingInstructorId = null,
            int? actingAdminUserId = null)
        {
            if (sessionType != AcademyConstants.AttendanceSessionType.Course
                && sessionType != AcademyConstants.AttendanceSessionType.Activity)
                return AcademyActionResult.Fail("نوع جلسه نامعتبر است.");
            if (sessionId <= 0) return AcademyActionResult.Fail("جلسه الزامی است.");
            if (!await SessionOwnedAsync(sessionType, sessionId))
                return AcademyActionResult.Fail("جلسه یافت نشد.");
            if (validMinutes < 5) validMinutes = 5;
            if (validMinutes > 180) validMinutes = 180;

            if (actingInstructorId.HasValue && actingInstructorId.Value > 0)
            {
                int? courseSessionId = sessionType == AcademyConstants.AttendanceSessionType.Course ? sessionId : null;
                int? activitySessionId = sessionType == AcademyConstants.AttendanceSessionType.Activity ? sessionId : null;
                if (!await InstructorOwnsSessionAsync(courseSessionId, activitySessionId, actingInstructorId.Value))
                    return AcademyActionResult.Fail("شما مجاز به ساخت QR این جلسه نیستید.");
            }

            var token = Guid.NewGuid().ToString("N");
            var expires = DateTime.Now.AddMinutes(validMinutes);
            var id = await _repo.CreateQrTokenAsync(sessionType, sessionId, token, expires, actingInstructorId, actingAdminUserId);
            return id switch
            {
                -2 => AcademyActionResult.Fail("نوع جلسه نامعتبر است."),
                -5 => AcademyActionResult.Fail("جلسه یافت نشد."),
                > 0 => AcademyActionResult.Success(id),
                _ => AcademyActionResult.Fail("ساخت QR ناموفق بود.")
            };
        }

        public async Task<AcademyAttendanceQrTokenDto> GetActiveQrAsync(string sessionType, int sessionId)
            => await SessionOwnedAsync(sessionType, sessionId)
                ? await _repo.GetActiveQrAsync(sessionType, sessionId)
                : null;

        public async Task<AcademyAttendanceQrTokenDto> GetQrByTokenAsync(string token)
        {
            var qr = await _repo.GetQrByTokenAsync(token);
            if (qr != null && !await SessionOwnedAsync(qr.SessionType, qr.SessionId))
                return null;
            if (qr != null)
                qr.SessionTitle = null;
            return qr;
        }

        public async Task<AcademyActionResult> CheckInByQrAsync(string token, int studentId)
        {
            if (string.IsNullOrWhiteSpace(token) || studentId <= 0)
                return AcademyActionResult.Fail("توکن یا دانش‌آموز نامعتبر است.");

            var qr = await _repo.GetQrByTokenAsync(token);
            if (qr == null) return AcademyActionResult.Fail("کد QR نامعتبر است.");
            if (!await SessionOwnedAsync(qr.SessionType, qr.SessionId)
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId))
                return AcademyActionResult.Fail("کد QR نامعتبر است.");
            if (qr.IsRevoked) return AcademyActionResult.Fail("این QR باطل شده است.");
            if (qr.ExpiresAt < DateTime.Now) return AcademyActionResult.Fail("مهلت این QR به پایان رسیده است.");

            var roster = await GetRosterAsync(qr.SessionType, qr.SessionId);
            var row = roster.FirstOrDefault(x => x.StudentId == studentId);
            if (row == null)
                return AcademyActionResult.Fail("شما در لیست این جلسه نیستید.");

            if (string.Equals(row.Status, AcademyConstants.AttendanceStatus.Present, StringComparison.OrdinalIgnoreCase)
                && row.AttendanceId.GetValueOrDefault() > 0)
            {
                return AcademyActionResult.Success(row.AttendanceId.Value, message: qr.SessionTitle);
            }

            var dto = new UpsertAcademyAttendanceDto
            {
                StudentId = studentId,
                Status = AcademyConstants.AttendanceStatus.Present,
                Source = AcademyConstants.AttendanceSource.QR,
                CourseSessionId = qr.SessionType == AcademyConstants.AttendanceSessionType.Course ? qr.SessionId : null,
                ActivitySessionId = qr.SessionType == AcademyConstants.AttendanceSessionType.Activity ? qr.SessionId : null,
                MarkedByInstructorId = qr.CreatedByInstructorId
            };
            var id = await _repo.UpsertAsync(dto);
            if (id > 0)
            {
                var relatedId = dto.CourseSessionId ?? dto.ActivitySessionId ?? qr.SessionId;
                try { await _points.AwardOnceAsync(studentId, AcademyConstants.PointReason.Attendance, "Attendance", relatedId, 5); }
                catch { }
                return AcademyActionResult.Success(id, message: qr.SessionTitle);
            }
            return AcademyActionResult.Fail("ثبت حضور از طریق QR ناموفق بود.");
        }

        private static void NormalizeSession(UpsertAcademyAttendanceDto dto)
        {
            if (dto.CourseSessionId.HasValue && dto.CourseSessionId <= 0) dto.CourseSessionId = null;
            if (dto.ActivitySessionId.HasValue && dto.ActivitySessionId <= 0) dto.ActivitySessionId = null;
            if (dto.CourseSessionId.HasValue) dto.ActivitySessionId = null;
        }

        private async Task<bool> InstructorOwnsSessionAsync(int? courseSessionId, int? activitySessionId, int instructorId)
        {
            if (courseSessionId.HasValue)
            {
                var roster = await _repo.GetCourseSessionRosterAsync(courseSessionId.Value);
                var owner = roster.FirstOrDefault()?.CourseInstructorId;
                if (owner.HasValue) return owner.Value == instructorId;

                var courses = await _courses.GetAllAsync(AcademyTenantOwnership.SiteId(_tenant));
                foreach (var c in courses.Where(x => x.InstructorId == instructorId))
                {
                    var ss = await _courses.GetSessionsAsync(c.Id);
                    if (ss.Any(s => s.Id == courseSessionId.Value)) return true;
                }
                return false;
            }

            if (activitySessionId.HasValue)
            {
                var ses = await _activitySessions.GetByIdAsync(activitySessionId.Value);
                return ses != null && ses.InstructorId == instructorId;
            }
            return false;
        }

        private async Task<bool> InstructorOwnsDailyAsync(int scheduleId, DateTime sessionDate, int instructorId)
        {
            var schedule = await _courses.GetScheduleByIdAsync(scheduleId);
            if (schedule == null || !AcademyDailyScheduleResolver.IsDaily(schedule))
                return false;

            var course = schedule.CourseId > 0 ? await _courses.GetByIdAsync(schedule.CourseId) : null;
            if (course != null && course.InstructorId == instructorId)
                return true;

            if (_exceptions == null)
                return false;
            var exception = await _exceptions.GetByScheduleDateAsync(scheduleId, sessionDate.Date);
            return exception != null && exception.InstructorId == instructorId;
        }

        private async Task<bool> StudentAndSessionOwnedAsync(UpsertAcademyAttendanceDto dto)
        {
            if (!await AcademyTenantOwnership.StudentAsync(_tenant, _students, dto.StudentId))
                return false;
            if (dto.CourseSessionId.HasValue)
                return await AcademyTenantOwnership.CourseSessionAsync(_tenant, _courses, dto.CourseSessionId.Value);
            return dto.ActivitySessionId.HasValue
                && await AcademyTenantOwnership.ActivitySessionAsync(
                    _tenant, _activitySessions, _activities, dto.ActivitySessionId.Value);
        }

        private Task<bool> SessionOwnedAsync(string sessionType, int sessionId)
            => sessionType == AcademyConstants.AttendanceSessionType.Course
                ? AcademyTenantOwnership.CourseSessionAsync(_tenant, _courses, sessionId)
                : sessionType == AcademyConstants.AttendanceSessionType.Activity
                    ? AcademyTenantOwnership.ActivitySessionAsync(_tenant, _activitySessions, _activities, sessionId)
                    : Task.FromResult(false);

        private Task<bool> OwnsAsync(AcademyAttendanceDto item)
            => AcademyTenantOwnership.AttendanceAsync(
                _tenant, _courses, _activitySessions, _activities, _students, item);
    }
}
