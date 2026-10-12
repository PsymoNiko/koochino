using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using MyShop.Core.Models.Messaging;

namespace MyShop.Application.Services.Academy
{
    public class AcademyNotificationService : IAcademyNotificationService
    {
        private readonly IAcademyNotificationRepository _log;
        private readonly ISmsSender _sms;
        private readonly ISmsTemplateService _templates;
        private readonly IAcademyEnrollmentRepository _enrollments;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyActivityRequestRepository _requests;
        private readonly IAcademyActivitySessionRepository _activitySessions;
        private readonly IAcademyPaymentRepository _payments;
        private readonly IAcademyCertificateRepository _certificates;
        private readonly IAcademyActivityRepository _activities;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;

        public AcademyNotificationService(
            IAcademyNotificationRepository log,
            ISmsSender sms,
            ISmsTemplateService templates,
            IAcademyEnrollmentRepository enrollments,
            IAcademyCourseRepository courses,
            IAcademyActivityRequestRepository requests,
            IAcademyActivitySessionRepository activitySessions,
            IAcademyPaymentRepository payments,
            IAcademyCertificateRepository certificates,
            IAcademyActivityRepository activities = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _log = log;
            _sms = sms;
            _templates = templates;
            _enrollments = enrollments;
            _courses = courses;
            _requests = requests;
            _activitySessions = activitySessions;
            _payments = payments;
            _certificates = certificates;
            _activities = activities;
            _students = students;
            _tenant = tenant;
        }

        public async Task<List<AcademyNotificationLogDto>> GetLogsAsync(string eventCode = null, int? studentId = null, bool? success = null, int top = 200)
        {
            if (studentId.HasValue && !await OwnsStudentAsync(studentId.Value))
                return new List<AcademyNotificationLogDto>();
            var list = await _log.GetAllAsync(eventCode, studentId, success, top)
                ?? new List<AcademyNotificationLogDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyNotificationLogDto>();
            foreach (var item in list)
                if (item.StudentId.HasValue && await OwnsStudentAsync(item.StudentId.Value))
                    result.Add(item);
            return result;
        }

        public async Task NotifyEnrollmentRegisteredAsync(int enrollmentId)
        {
            var e = await _enrollments.GetByIdAsync(enrollmentId);
            if (e == null || !await OwnsCourseStudentAsync(e.CourseId, e.StudentId)) return;
            var statusFa = e.Status == AcademyConstants.EnrollmentStatus.Waitlisted ? "لیست انتظار" : "در انتظار بررسی";
            await SendAsync(
                AcademyConstants.NotificationEvent.EnrollmentRegistered,
                AcademyConstants.SmsTemplateCode.EnrollmentRegistered,
                e.StudentId, e.StudentPhone, "Enrollment", e.Id,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["NAME"] = FirstName(e.StudentName),
                    ["COURSE"] = e.CourseTitle ?? "",
                    ["STATUS"] = statusFa
                },
                $"{FirstName(e.StudentName)} عزیز، ثبت‌نام شما در دوره «{e.CourseTitle}» دریافت شد. وضعیت: {statusFa}");
        }

        public async Task NotifyEnrollmentReviewedAsync(int enrollmentId)
        {
            var e = await _enrollments.GetByIdAsync(enrollmentId);
            if (e == null || !await OwnsCourseStudentAsync(e.CourseId, e.StudentId)) return;
            if (e.Status == AcademyConstants.EnrollmentStatus.Approved)
            {
                var pay = e.NeedsTuitionPayment ? " برای پرداخت شهریه وارد پنل دانش‌آموز شوید." : "";
                await SendAsync(
                    AcademyConstants.NotificationEvent.EnrollmentApproved,
                    AcademyConstants.SmsTemplateCode.EnrollmentApproved,
                    e.StudentId, e.StudentPhone, "Enrollment", e.Id,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["NAME"] = FirstName(e.StudentName),
                        ["COURSE"] = e.CourseTitle ?? "",
                        ["PAY"] = pay
                    },
                    $"{FirstName(e.StudentName)} عزیز، ثبت‌نام شما در دوره «{e.CourseTitle}» تأیید شد.{pay}");
            }
            else if (e.Status == AcademyConstants.EnrollmentStatus.Rejected)
            {
                await SendAsync(
                    AcademyConstants.NotificationEvent.EnrollmentRejected,
                    AcademyConstants.SmsTemplateCode.EnrollmentRejected,
                    e.StudentId, e.StudentPhone, "Enrollment", e.Id,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["NAME"] = FirstName(e.StudentName),
                        ["COURSE"] = e.CourseTitle ?? ""
                    },
                    $"{FirstName(e.StudentName)} عزیز، ثبت‌نام شما در دوره «{e.CourseTitle}» رد شد.");
            }
        }

        public async Task NotifyActivityRequestRegisteredAsync(int requestId)
        {
            var r = await _requests.GetByIdAsync(requestId);
            if (r == null || !await OwnsActivityStudentAsync(r.ActivityId, r.StudentId)) return;
            await SendAsync(
                AcademyConstants.NotificationEvent.ActivityRequestRegistered,
                AcademyConstants.SmsTemplateCode.ActivityRequestRegistered,
                r.StudentId, r.StudentPhone, "ActivityRequest", r.Id,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["NAME"] = FirstName(r.StudentName),
                    ["ACTIVITY"] = r.ActivityTitle ?? r.SessionTitle ?? ""
                },
                $"{FirstName(r.StudentName)} عزیز، درخواست شرکت شما در «{r.ActivityTitle}» ثبت شد و در انتظار بررسی است.");
        }

        public async Task NotifyActivityRequestReviewedAsync(int requestId)
        {
            var r = await _requests.GetByIdAsync(requestId);
            if (r == null || !await OwnsActivityStudentAsync(r.ActivityId, r.StudentId)) return;
            var title = r.ActivityTitle ?? r.SessionTitle ?? "";
            if (r.Status == AcademyConstants.RequestStatus.Approved)
            {
                var when = r.StartsAt.ToString("yyyy/MM/dd HH:mm");
                await SendAsync(
                    AcademyConstants.NotificationEvent.ActivityRequestApproved,
                    AcademyConstants.SmsTemplateCode.ActivityRequestApproved,
                    r.StudentId, r.StudentPhone, "ActivityRequest", r.Id,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["NAME"] = FirstName(r.StudentName),
                        ["ACTIVITY"] = title,
                        ["WHEN"] = when
                    },
                    $"{FirstName(r.StudentName)} عزیز، درخواست شرکت شما در «{title}» تأیید شد. زمان: {when}");
            }
            else if (r.Status == AcademyConstants.RequestStatus.Rejected)
            {
                await SendAsync(
                    AcademyConstants.NotificationEvent.ActivityRequestRejected,
                    AcademyConstants.SmsTemplateCode.ActivityRequestRejected,
                    r.StudentId, r.StudentPhone, "ActivityRequest", r.Id,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["NAME"] = FirstName(r.StudentName),
                        ["ACTIVITY"] = title
                    },
                    $"{FirstName(r.StudentName)} عزیز، درخواست شرکت شما در «{title}» رد شد.");
            }
        }

        public async Task NotifyPaymentPaidAsync(int paymentId)
        {
            var p = await _payments.GetByIdAsync(paymentId);
            if (p == null || !await OwnsCourseStudentAsync(p.CourseId, p.StudentId)) return;
            await SendAsync(
                AcademyConstants.NotificationEvent.PaymentPaid,
                AcademyConstants.SmsTemplateCode.PaymentPaid,
                p.StudentId, p.StudentPhone, "Payment", p.Id,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["NAME"] = FirstName(p.StudentName),
                    ["COURSE"] = p.CourseTitle ?? "",
                    ["AMOUNT"] = p.Amount.ToString("N0")
                },
                $"{FirstName(p.StudentName)} عزیز، شهریه دوره «{p.CourseTitle}» به مبلغ {p.Amount:N0} ریال با موفقیت ثبت شد.");
        }

        public async Task NotifyCertificateIssuedAsync(int certificateId)
        {
            var c = await _certificates.GetByIdAsync(certificateId);
            if (c == null || !await OwnsCourseStudentAsync(c.CourseId, c.StudentId) || !c.IsIssued) return;
            await SendAsync(
                AcademyConstants.NotificationEvent.CertificateIssued,
                AcademyConstants.SmsTemplateCode.CertificateIssued,
                c.StudentId, c.StudentPhone, "Certificate", c.Id,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["NAME"] = FirstName(c.StudentName),
                    ["COURSE"] = c.CourseTitle ?? "",
                    ["NO"] = c.CertificateNo ?? ""
                },
                $"{FirstName(c.StudentName)} عزیز، گواهی پایان دوره «{c.CourseTitle}» با شماره {c.CertificateNo} صادر شد. از پنل دانش‌آموز دانلود کنید.");
        }

        public async Task<int> SendSessionRemindersAsync(DateTime? forDate = null)
        {
            var date = (forDate ?? DateTime.Today).Date;
            var since = date;
            var sent = 0;

            var courses = await _courses.GetAllAsync(
                AcademyTenantOwnership.SiteId(_tenant), isActive: true) ?? new List<AcademyCourseDto>();
            foreach (var course in courses)
            {
                var sessions = await _courses.GetSessionsAsync(course.Id) ?? new List<AcademyCourseSessionDto>();
                foreach (var session in sessions.Where(s =>
                    s.SessionDate.Date == date &&
                    s.Status != AcademyConstants.SessionStatus.Cancelled))
                {
                    var enrollments = await _enrollments.GetAllAsync(courseId: course.Id, status: AcademyConstants.EnrollmentStatus.Approved)
                                      ?? new List<AcademyEnrollmentDto>();
                    var completed = await _enrollments.GetAllAsync(courseId: course.Id, status: AcademyConstants.EnrollmentStatus.Completed)
                                    ?? new List<AcademyEnrollmentDto>();
                    foreach (var e in enrollments.Concat(completed))
                    {
                        if (await _log.ExistsSuccessfulAsync(
                            AcademyConstants.NotificationEvent.SessionReminder, e.StudentId, "CourseSession", session.Id, since))
                            continue;
                        var time = session.StartTime?.ToString(@"hh\:mm") ?? "—";
                        var title = session.Title ?? course.Title;
                        var ok = await SendAsync(
                            AcademyConstants.NotificationEvent.SessionReminder,
                            AcademyConstants.SmsTemplateCode.SessionReminder,
                            e.StudentId, e.StudentPhone, "CourseSession", session.Id,
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["NAME"] = FirstName(e.StudentName),
                                ["TITLE"] = title,
                                ["DATE"] = date.ToString("yyyy/MM/dd"),
                                ["TIME"] = time
                            },
                            $"{FirstName(e.StudentName)} عزیز، یادآوری: «{title}» در تاریخ {date:yyyy/MM/dd} ساعت {time} برگزار می‌شود.");
                        if (ok) sent++;
                    }
                }
            }

            var activitySessions = await _activitySessions.GetAllAsync() ?? new List<AcademyActivitySessionDto>();
            foreach (var session in activitySessions.Where(s =>
                s.StartsAt.Date == date &&
                s.Status != AcademyConstants.SessionStatus.Cancelled))
            {
                if (!await AcademyTenantOwnership.ActivityAsync(_tenant, _activities, session.ActivityId))
                    continue;
                var reqs = await _requests.GetAllAsync(sessionId: session.Id, status: AcademyConstants.RequestStatus.Approved)
                           ?? new List<AcademyActivityRequestDto>();
                foreach (var r in reqs)
                {
                    if (await _log.ExistsSuccessfulAsync(
                        AcademyConstants.NotificationEvent.ActivitySessionReminder, r.StudentId, "ActivitySession", session.Id, since))
                        continue;
                    var time = session.StartsAt.ToString("HH:mm");
                    var title = session.Title ?? session.ActivityTitle ?? "جلسه فعالیت";
                    var ok = await SendAsync(
                        AcademyConstants.NotificationEvent.ActivitySessionReminder,
                        AcademyConstants.SmsTemplateCode.SessionReminder,
                        r.StudentId, r.StudentPhone, "ActivitySession", session.Id,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["NAME"] = FirstName(r.StudentName),
                            ["TITLE"] = title,
                            ["DATE"] = date.ToString("yyyy/MM/dd"),
                            ["TIME"] = time
                        },
                        $"{FirstName(r.StudentName)} عزیز، یادآوری: «{title}» در تاریخ {date:yyyy/MM/dd} ساعت {time} برگزار می‌شود.");
                    if (ok) sent++;
                }
            }

            return sent;
        }

        public async Task<int> SendTuitionRemindersAsync(DateTime? forDate = null)
        {
            var date = (forDate ?? DateTime.Today).Date;
            var sent = 0;
            var pending = await _payments.GetAllAsync(status: AcademyConstants.PaymentStatus.Pending)
                          ?? new List<AcademyPaymentDto>();
            foreach (var p in pending)
            {
                if (!await OwnsStudentAsync(p.StudentId)) continue;
                if (await _log.ExistsSuccessfulAsync(
                    AcademyConstants.NotificationEvent.PaymentDue, p.StudentId, "Payment", p.Id, date))
                    continue;
                var ok = await SendAsync(
                    AcademyConstants.NotificationEvent.PaymentDue,
                    AcademyConstants.SmsTemplateCode.PaymentPaid,
                    p.StudentId, p.StudentPhone, "Payment", p.Id,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["NAME"] = FirstName(p.StudentName),
                        ["COURSE"] = p.CourseTitle ?? "",
                        ["AMOUNT"] = p.Amount.ToString("N0")
                    },
                    $"{FirstName(p.StudentName)} عزیز، شهریه دوره «{p.CourseTitle}» به مبلغ {p.Amount:N0} هنوز پرداخت نشده است.");
                if (ok) sent++;
            }
            return sent;
        }

        private async Task<bool> SendAsync(
            string eventCode,
            string templateCode,
            int studentId,
            string phone,
            string relatedType,
            int relatedId,
            IDictionary<string, string> tokens,
            string fallback)
        {
            if (!await OwnsStudentAsync(studentId))
                return false;
            phone = (phone ?? "").Trim();
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
            {
                await _log.InsertAsync(new AcademyNotificationLogDto
                {
                    EventCode = eventCode,
                    StudentId = studentId,
                    Phone = phone,
                    RelatedType = relatedType,
                    RelatedId = relatedId,
                    TemplateCode = templateCode,
                    MessageText = fallback,
                    Success = false,
                    Error = "شماره موبایل نامعتبر است."
                });
                return false;
            }

            var text = fallback;
            try
            {
                var template = await _templates.GetByCodeAsync(templateCode);
                if (template != null && template.IsActive && !string.IsNullOrWhiteSpace(template.BodyTemplate))
                    text = _templates.Render(template.BodyTemplate, tokens);
            }
            catch { /* fallback text */ }

            SmsSendResult sms;
            try
            {
                sms = await _sms.SendAsync(phone, text, null, MessagingMessageTypes.Academy, templateCode);
            }
            catch (Exception ex)
            {
                sms = new SmsSendResult(false, null, ex.Message);
            }

            await _log.InsertAsync(new AcademyNotificationLogDto
            {
                EventCode = eventCode,
                StudentId = studentId,
                Phone = phone,
                RelatedType = relatedType,
                RelatedId = relatedId,
                TemplateCode = templateCode,
                MessageText = text,
                Success = sms.Ok,
                Error = sms.Ok ? null : (sms.Error ?? "ارسال ناموفق"),
                ExternalId = sms.MessageId
            });
            return sms.Ok;
        }

        private Task<bool> OwnsStudentAsync(int studentId)
            => AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId);

        private async Task<bool> OwnsCourseStudentAsync(int courseId, int studentId)
            => await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId)
                && await OwnsStudentAsync(studentId);

        private async Task<bool> OwnsActivityStudentAsync(int activityId, int studentId)
            => await AcademyTenantOwnership.ActivityAsync(_tenant, _activities, activityId)
                && await OwnsStudentAsync(studentId);

        private static string FirstName(string full)
        {
            var t = (full ?? "").Trim();
            if (string.IsNullOrEmpty(t)) return "دانش‌آموز";
            var space = t.IndexOf(' ');
            return space > 0 ? t.Substring(0, space) : t;
        }
    }
}
