namespace MyShop.Core.Models.Academy
{
    /// <summary>ثابت‌های ماژول آموزشگاه.</summary>
    public static class AcademyConstants
    {
        public const string ModuleKey = "Academy";
        public const string RoleStudent = "Student";
        public const string RoleInstructor = "Instructor";
        public const string RoleAdmin = "Admin";

        /// <summary>کدهای پایدار taxonomy روی AcademyActivityCategories؛ ماژول جدا نیستند.</summary>
        public static class Taxonomy
        {
            public const string Education = "Education";
            public const string Sport = "Sport";
            public const string Culture = "Culture";
            public const string Ceremony = "Ceremony";
        }

        public static class StudentStatus
        {
            public const string Active = "Active";
            public const string Inactive = "Inactive";
        }

        public static class RequestStatus
        {
            public const string Pending = "Pending";
            public const string Waitlisted = "Waitlisted";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Cancelled = "Cancelled";
        }

        public static class SessionStatus
        {
            public const string Scheduled = "Scheduled";
            public const string Open = "Open";
            public const string Closed = "Closed";
            public const string Cancelled = "Cancelled";
        }

        /// <summary>نوع تکرار برنامه دوره. Weekly رفتار فعلی است؛ Daily جلسه منطقی است نه رکورد فیزیکی.</summary>
        public static class ScheduleRecurrenceType
        {
            public const string Weekly = "Weekly";
            public const string Daily = "Daily";
        }

        public static class EnrollmentStatus
        {
            public const string Registered = "Registered";
            public const string Waitlisted = "Waitlisted";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Cancelled = "Cancelled";
            public const string Completed = "Completed";
        }

        public static class AttendanceStatus
        {
            public const string Present = "Present";
            public const string Absent = "Absent";
            public const string Late = "Late";
            public const string Excused = "Excused";
        }

        public static class AttendanceSource
        {
            public const string Manual = "Manual";
            public const string QR = "QR";
        }

        public static class AttendanceSessionType
        {
            public const string Course = "Course";
            public const string Activity = "Activity";
            public const string Daily = "Daily";
        }

        public static class PaymentStatus
        {
            public const string Pending = "Pending";
            public const string Paid = "Paid";
            public const string Failed = "Failed";
            public const string Cancelled = "Cancelled";
        }

        public static class PaymentSource
        {
            public const string Gateway = "Gateway";
            public const string Manual = "Manual";
        }

        public static class CertificateStatus
        {
            public const string Issued = "Issued";
            public const string Revoked = "Revoked";
        }

        public static class NotificationEvent
        {
            public const string EnrollmentRegistered = "EnrollmentRegistered";
            public const string EnrollmentApproved = "EnrollmentApproved";
            public const string EnrollmentRejected = "EnrollmentRejected";
            public const string ActivityRequestRegistered = "ActivityRequestRegistered";
            public const string ActivityRequestApproved = "ActivityRequestApproved";
            public const string ActivityRequestRejected = "ActivityRequestRejected";
            public const string PaymentPaid = "PaymentPaid";
            public const string SessionReminder = "SessionReminder";
            public const string ActivitySessionReminder = "ActivitySessionReminder";
            public const string CertificateIssued = "CertificateIssued";
            public const string PaymentDue = "PaymentDue";
        }

        public static class SmsTemplateCode
        {
            public const string EnrollmentRegistered = "AcademyEnrollmentRegistered";
            public const string EnrollmentApproved = "AcademyEnrollmentApproved";
            public const string EnrollmentRejected = "AcademyEnrollmentRejected";
            public const string ActivityRequestRegistered = "AcademyActivityRequestRegistered";
            public const string ActivityRequestApproved = "AcademyActivityRequestApproved";
            public const string ActivityRequestRejected = "AcademyActivityRequestRejected";
            public const string PaymentPaid = "AcademyPaymentPaid";
            public const string SessionReminder = "AcademySessionReminder";
            public const string CertificateIssued = "AcademyCertificateIssued";
        }

        public static class PointReason
        {
            public const string Evaluation = "Evaluation";
            public const string QuizPass = "QuizPass";
            public const string CourseCompleted = "CourseCompleted";
            public const string Attendance = "Attendance";
        }
    }
}
