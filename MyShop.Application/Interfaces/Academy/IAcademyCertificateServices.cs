using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyCertificateRepository
    {
        Task<List<AcademyCertificateDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, string status = null);
        Task<AcademyCertificateDto> GetByIdAsync(int id);
        Task<AcademyCertificateDto> GetByEnrollmentAsync(int enrollmentId);
        Task<int> IssueAsync(int enrollmentId, int? issuedByUserId, int? issuedByInstructorId, string notes);
        Task<bool> RevokeAsync(int id, string reason);
    }

    public interface IAcademyCertificateService
    {
        Task<List<AcademyCertificateDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, string status = null);
        Task<AcademyCertificateDto> GetByIdAsync(int id);
        Task<AcademyCertificateDto> GetByEnrollmentAsync(int enrollmentId);
        Task<AcademyActionResult> IssueAsync(int enrollmentId, int? actingAdminUserId = null, int? actingInstructorId = null, string notes = null);
        Task<AcademyActionResult> RevokeAsync(int id, string reason = null);
    }
}
