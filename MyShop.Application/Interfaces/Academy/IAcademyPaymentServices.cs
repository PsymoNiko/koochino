using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyPaymentRepository
    {
        Task<List<AcademyPaymentDto>> GetAllAsync(int? enrollmentId = null, int? studentId = null, string status = null);
        Task<AcademyPaymentDto> GetByIdAsync(int id);
        Task<AcademyPaymentDto> GetByAuthorityAsync(string authority);
        Task<int> InsertAsync(int enrollmentId, decimal amount, string status, string source, string gateway, string authority, string gatewayRef);
        Task<bool> SetAuthorityAsync(int id, string authority, string gateway);
        Task<int> MarkPaidAsync(int id, string gatewayRef);
        Task<bool> MarkFailedAsync(int id);
    }

    public interface IAcademyPaymentService
    {
        Task<List<AcademyPaymentDto>> GetAllAsync(int? enrollmentId = null, int? studentId = null, string status = null);
        Task<AcademyPaymentDto> GetByIdAsync(int id);
        /// <summary>شروع پرداخت شهریه — فقط بعد از Approve ثبت‌نام.</summary>
        Task<AcademyActionResult> StartAsync(int enrollmentId, int studentId, string provider, string callbackUrl);
        Task<AcademyActionResult> CompleteOnlineAsync(string authority, string statusQuery, int studentId);
        Task<AcademyActionResult> MarkPaidManualAsync(int enrollmentId);
    }
}
