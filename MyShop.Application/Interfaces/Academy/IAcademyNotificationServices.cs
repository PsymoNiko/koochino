using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyNotificationRepository
    {
        Task<int> InsertAsync(AcademyNotificationLogDto row);
        Task<List<AcademyNotificationLogDto>> GetAllAsync(string eventCode = null, int? studentId = null, bool? success = null, int top = 200);
        Task<bool> ExistsSuccessfulAsync(string eventCode, int studentId, string relatedType, int relatedId, DateTime since);
    }

    public interface IAcademyNotificationService
    {
        Task NotifyEnrollmentRegisteredAsync(int enrollmentId);
        Task NotifyEnrollmentReviewedAsync(int enrollmentId);
        Task NotifyActivityRequestRegisteredAsync(int requestId);
        Task NotifyActivityRequestReviewedAsync(int requestId);
        Task NotifyPaymentPaidAsync(int paymentId);
        Task NotifyCertificateIssuedAsync(int certificateId);
        Task<int> SendSessionRemindersAsync(DateTime? forDate = null);
        Task<int> SendTuitionRemindersAsync(DateTime? forDate = null);
        Task<List<AcademyNotificationLogDto>> GetLogsAsync(string eventCode = null, int? studentId = null, bool? success = null, int top = 200);
    }
}
