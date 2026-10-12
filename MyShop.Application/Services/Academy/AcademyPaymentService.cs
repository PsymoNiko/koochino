using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyPaymentService : IAcademyPaymentService
    {
        private readonly IAcademyPaymentRepository _repo;
        private readonly IAcademyEnrollmentRepository _enrollments;
        private readonly IPaymentGatewayRegistry _gateways;
        private readonly IAcademyNotificationService _notify;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;

        public AcademyPaymentService(
            IAcademyPaymentRepository repo,
            IAcademyEnrollmentRepository enrollments,
            IPaymentGatewayRegistry gateways,
            IAcademyNotificationService notify,
            IAcademyCourseRepository courses = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _enrollments = enrollments;
            _gateways = gateways;
            _notify = notify;
            _courses = courses;
            _students = students;
            _tenant = tenant;
        }

        public async Task<List<AcademyPaymentDto>> GetAllAsync(int? enrollmentId = null, int? studentId = null, string status = null)
        {
            if (enrollmentId.HasValue && !await OwnsEnrollmentAsync(enrollmentId.Value))
                return new List<AcademyPaymentDto>();
            if (studentId.HasValue
                && !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId.Value))
                return new List<AcademyPaymentDto>();
            var list = await _repo.GetAllAsync(enrollmentId, studentId, status)
                ?? new List<AcademyPaymentDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyPaymentDto>();
            foreach (var item in list)
                if (await OwnsAsync(item)) result.Add(item);
            return result;
        }

        public async Task<AcademyPaymentDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return await OwnsAsync(item) ? item : null;
        }

        public async Task<AcademyActionResult> StartAsync(int enrollmentId, int studentId, string provider, string callbackUrl)
        {
            if (enrollmentId <= 0 || studentId <= 0)
                return AcademyActionResult.Fail("ثبت‌نام نامعتبر است.");
            if (string.IsNullOrWhiteSpace(callbackUrl))
                return AcademyActionResult.Fail("آدرس بازگشت پرداخت مشخص نیست.");

            var enrollment = await _enrollments.GetByIdAsync(enrollmentId);
            if (enrollment == null || !await OwnsEnrollmentDtoAsync(enrollment))
                return AcademyActionResult.Fail("ثبت‌نام یافت نشد.");
            if (enrollment.StudentId != studentId)
                return AcademyActionResult.Fail("این ثبت‌نام متعلق به شما نیست.");
            if (enrollment.Status != AcademyConstants.EnrollmentStatus.Approved
                && enrollment.Status != AcademyConstants.EnrollmentStatus.Completed)
                return AcademyActionResult.Fail("پرداخت شهریه فقط بعد از تأیید ثبت‌نام ممکن است.");
            if (string.Equals(enrollment.PaymentStatus, AcademyConstants.PaymentStatus.Paid, StringComparison.OrdinalIgnoreCase))
                return AcademyActionResult.Fail("شهریه این دوره قبلاً پرداخت شده است.");

            var amount = enrollment.CoursePrice.GetValueOrDefault();
            if (amount <= 0)
                return AcademyActionResult.Fail("این دوره شهریه ندارد.");

            var gateway = ResolveGateway(provider);
            if (gateway == null)
                return AcademyActionResult.Fail("درگاه پرداخت در دسترس نیست.");

            var id = await _repo.InsertAsync(
                enrollmentId,
                amount,
                AcademyConstants.PaymentStatus.Pending,
                AcademyConstants.PaymentSource.Gateway,
                gateway.Provider,
                null,
                null);
            if (id == -1) return AcademyActionResult.Fail("شهریه این دوره قبلاً پرداخت شده است.");
            if (id == -2) return AcademyActionResult.Fail("ثبت‌نام تأییدشده یافت نشد.");
            if (id <= 0) return AcademyActionResult.Fail("خطا در ایجاد پرداخت.");

            var start = await gateway.StartPaymentAsync(
                (long)amount,
                "شهریه دوره " + (enrollment.CourseTitle ?? enrollmentId.ToString()),
                callbackUrl,
                "ACP-" + id);

            if (!start.Ok)
            {
                await _repo.MarkFailedAsync(id);
                return AcademyActionResult.Fail(start.Error ?? "شروع پرداخت ناموفق بود.");
            }

            await _repo.SetAuthorityAsync(id, start.Authority, gateway.Provider);
            return AcademyActionResult.Success(id, start.RedirectUrl);
        }

        public async Task<AcademyActionResult> CompleteOnlineAsync(string authority, string statusQuery, int studentId)
        {
            if (string.IsNullOrWhiteSpace(authority))
                return AcademyActionResult.Fail("کد پیگیری درگاه دریافت نشد.");
            if (studentId <= 0)
                return AcademyActionResult.Fail("ورود لازم است.");

            var payment = await _repo.GetByAuthorityAsync(authority);
            if (payment == null || !await OwnsAsync(payment))
                return AcademyActionResult.Fail("پرداخت متناظر یافت نشد.");
            if (payment.StudentId != studentId)
                return AcademyActionResult.Fail("این پرداخت متعلق به شما نیست.");
            if (string.Equals(payment.Status, AcademyConstants.PaymentStatus.Paid, StringComparison.OrdinalIgnoreCase))
                return AcademyActionResult.Success(payment.Id);

            var okStatus = string.Equals(statusQuery, "OK", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(statusQuery, "OK", StringComparison.Ordinal);
            if (!okStatus)
            {
                await _repo.MarkFailedAsync(payment.Id);
                return AcademyActionResult.Fail("پرداخت توسط درگاه لغو یا ناموفق شد.");
            }

            var gateway = ResolveGateway(payment.Gateway);
            if (gateway == null)
                return AcademyActionResult.Fail("درگاه پرداخت در دسترس نیست.");

            var verify = await gateway.VerifyAsync(authority, (long)payment.Amount);
            if (!verify.Ok)
            {
                await _repo.MarkFailedAsync(payment.Id);
                return AcademyActionResult.Fail(verify.Error ?? "تأیید پرداخت ناموفق بود.");
            }

            var code = await _repo.MarkPaidAsync(payment.Id, verify.RefId);
            if (code == 1)
            {
                try { await _notify.NotifyPaymentPaidAsync(payment.Id); } catch { }
                return AcademyActionResult.Success(payment.Id);
            }
            return code switch
            {
                -1 => AcademyActionResult.Fail("شهریه این دوره قبلاً پرداخت شده است."),
                _ => AcademyActionResult.Fail("ثبت پرداخت ناموفق بود.")
            };
        }

        public async Task<AcademyActionResult> MarkPaidManualAsync(int enrollmentId)
        {
            if (enrollmentId <= 0) return AcademyActionResult.Fail("ثبت‌نام نامعتبر است.");
            var enrollment = await _enrollments.GetByIdAsync(enrollmentId);
            if (enrollment == null || !await OwnsEnrollmentDtoAsync(enrollment))
                return AcademyActionResult.Fail("ثبت‌نام یافت نشد.");
            if (enrollment.Status != AcademyConstants.EnrollmentStatus.Approved
                && enrollment.Status != AcademyConstants.EnrollmentStatus.Completed)
                return AcademyActionResult.Fail("پرداخت دستی فقط برای ثبت‌نام تأییدشده مجاز است.");
            if (string.Equals(enrollment.PaymentStatus, AcademyConstants.PaymentStatus.Paid, StringComparison.OrdinalIgnoreCase))
                return AcademyActionResult.Fail("شهریه قبلاً پرداخت شده است.");

            var amount = enrollment.CoursePrice.GetValueOrDefault();
            if (amount <= 0) amount = 0;

            var id = await _repo.InsertAsync(
                enrollmentId,
                amount,
                AcademyConstants.PaymentStatus.Paid,
                AcademyConstants.PaymentSource.Manual,
                "Manual",
                null,
                "MANUAL");
            if (id <= 0)
            {
                return id switch
                {
                    -1 => AcademyActionResult.Fail("شهریه قبلاً پرداخت شده است."),
                    -2 => AcademyActionResult.Fail("ثبت‌نام تأییدشده یافت نشد."),
                    _ => AcademyActionResult.Fail("ثبت پرداخت دستی ناموفق بود.")
                };
            }
            try { await _notify.NotifyPaymentPaidAsync(id); } catch { }
            return AcademyActionResult.Success(id);
        }

        private IPaymentGateway ResolveGateway(string provider)
        {
            if (!string.IsNullOrWhiteSpace(provider))
            {
                var named = _gateways.Get(provider);
                if (named != null) return named;
            }
            var zarinpal = _gateways.Get("Zarinpal");
            if (zarinpal != null) return zarinpal;
            return _gateways.GetAll().Count > 0 ? _gateways.GetAll()[0] : null;
        }

        private Task<bool> OwnsEnrollmentAsync(int enrollmentId)
            => AcademyTenantOwnership.EnrollmentAsync(
                _tenant, _enrollments, _courses, _students, enrollmentId);

        private Task<bool> OwnsEnrollmentDtoAsync(AcademyEnrollmentDto item)
            => AcademyTenantOwnership.EnrollmentDtoAsync(_tenant, _courses, _students, item);

        private async Task<bool> OwnsAsync(AcademyPaymentDto item)
        {
            if (item == null) return false;
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return true;
            return await AcademyTenantOwnership.CourseAsync(_tenant, _courses, item.CourseId)
                && await AcademyTenantOwnership.StudentAsync(_tenant, _students, item.StudentId);
        }
    }
}
