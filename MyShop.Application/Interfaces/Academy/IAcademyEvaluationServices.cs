using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyEvaluationRepository
    {
        Task<List<AcademyEvaluationDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, bool publishedOnly = false);
        Task<AcademyEvaluationDto> GetByEnrollmentAsync(int enrollmentId);
        Task<int> UpsertAsync(UpsertAcademyEvaluationDto dto);
        Task<bool> SetPublishedAsync(int id, bool isPublished);
        Task<List<AcademyEvaluationCourseSummaryDto>> GetCourseSummaryAsync(int? instructorId = null);
        Task<List<AcademyEvaluationInstructorSummaryDto>> GetInstructorSummaryAsync();
    }

    public interface IAcademyEvaluationService
    {
        Task<List<AcademyEvaluationDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, bool publishedOnly = false);
        Task<AcademyEvaluationDto> GetByEnrollmentAsync(int enrollmentId);
        Task<AcademyActionResult> UpsertAsync(UpsertAcademyEvaluationDto dto);
        Task<AcademyActionResult> SetPublishedAsync(int id, bool isPublished);
        Task<List<AcademyEvaluationCourseSummaryDto>> GetCourseSummaryAsync(int? instructorId = null);
        Task<List<AcademyEvaluationInstructorSummaryDto>> GetInstructorSummaryAsync();
    }
}
