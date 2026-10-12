using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyEnrollmentRepository
    {
        Task<List<AcademyEnrollmentDto>> GetAllAsync(int? courseId = null, int? studentId = null, int? instructorId = null, string status = null);
        Task<AcademyEnrollmentDto> GetByIdAsync(int id);
        Task<AcademyEnrollmentDto> GetByCourseStudentAsync(int courseId, int studentId);
        Task<int> RegisterAsync(int courseId, int studentId, string studentNote);
        Task<int> ReviewAsync(ReviewAcademyEnrollmentDto dto);
        Task<bool> CancelByStudentAsync(int id, int studentId);
    }

    public interface IAcademyEnrollmentService
    {
        Task<List<AcademyEnrollmentDto>> GetAllAsync(int? courseId = null, int? studentId = null, int? instructorId = null, string status = null);
        Task<AcademyEnrollmentDto> GetByIdAsync(int id);
        Task<AcademyEnrollmentDto> GetByCourseStudentAsync(int courseId, int studentId);
        Task<AcademyActionResult> RegisterAsync(CreateAcademyEnrollmentDto dto);
        Task<AcademyActionResult> ReviewAsync(ReviewAcademyEnrollmentDto dto, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<AcademyActionResult> CancelByStudentAsync(int id, int studentId);
    }
}
