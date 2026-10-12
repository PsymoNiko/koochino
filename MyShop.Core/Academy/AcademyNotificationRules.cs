using System.Collections.Generic;

namespace MyShop.Core.Academy
{
    public sealed class AcademyNotificationRule
    {
        public string EventCode { get; init; }
        public string TitleFa { get; init; }
        public string TriggerFa { get; init; }
        public bool Automatic { get; init; }
    }

    /// <summary>قوانین اعلان روی همان AcademyNotificationLog / SMS موجود. موتور جدا نیست.</summary>
    public static class AcademyNotificationRules
    {
        public static IReadOnlyList<AcademyNotificationRule> All { get; } = new[]
        {
            new AcademyNotificationRule { EventCode = "EnrollmentRegistered", TitleFa = "ثبت‌نام دوره", TriggerFa = "پس از ثبت درخواست", Automatic = true },
            new AcademyNotificationRule { EventCode = "EnrollmentApproved", TitleFa = "تأیید ثبت‌نام", TriggerFa = "پس از بررسی ادمین", Automatic = true },
            new AcademyNotificationRule { EventCode = "EnrollmentRejected", TitleFa = "رد ثبت‌نام", TriggerFa = "پس از بررسی ادمین", Automatic = true },
            new AcademyNotificationRule { EventCode = "ActivityRequestRegistered", TitleFa = "درخواست فعالیت", TriggerFa = "پس از ثبت", Automatic = true },
            new AcademyNotificationRule { EventCode = "ActivityRequestApproved", TitleFa = "تأیید فعالیت", TriggerFa = "پس از بررسی", Automatic = true },
            new AcademyNotificationRule { EventCode = "PaymentPaid", TitleFa = "پرداخت شهریه", TriggerFa = "پس از وصول", Automatic = true },
            new AcademyNotificationRule { EventCode = "CertificateIssued", TitleFa = "صدور گواهی", TriggerFa = "پس از صدور", Automatic = true },
            new AcademyNotificationRule { EventCode = "SessionReminder", TitleFa = "یادآوری جلسه امروز", TriggerFa = "هر روز صبح یا دکمه ادمین", Automatic = true },
            new AcademyNotificationRule { EventCode = "PaymentDue", TitleFa = "یادآوری شهریه معوق", TriggerFa = "هر روز صبح یا دکمه ادمین", Automatic = true }
        };
    }
}
