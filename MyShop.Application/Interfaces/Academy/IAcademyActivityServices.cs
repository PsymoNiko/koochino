using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyActivityRepository
    {
        Task<List<AcademyActivityDto>> GetAllAsync(int? siteId = null, int? categoryId = null, bool? isActive = null);
        Task<AcademyActivityDto> GetByIdAsync(int id);
        Task<int> InsertAsync(CreateAcademyActivityDto dto);
        Task<int> UpdateAsync(UpdateAcademyActivityDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyActivitySessionRepository
    {
        Task<List<AcademyActivitySessionDto>> GetAllAsync(int? activityId = null, int? instructorId = null, string status = null);
        Task<AcademyActivitySessionDto> GetByIdAsync(int id);
        Task<int> InsertAsync(CreateAcademyActivitySessionDto dto);
        Task<bool> UpdateAsync(UpdateAcademyActivitySessionDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyActivityRequestRepository
    {
        Task<List<AcademyActivityRequestDto>> GetAllAsync(int? sessionId = null, int? studentId = null, int? instructorId = null, string status = null);
        Task<AcademyActivityRequestDto> GetByIdAsync(int id);
        Task<int> InsertAsync(int sessionId, int studentId, string studentNote);
        Task<int> ReviewAsync(ReviewAcademyActivityRequestDto dto);
    }

    public interface IAcademyActivityService
    {
        Task<List<AcademyActivityDto>> GetAllAsync(int? siteId = null, int? categoryId = null, bool? isActive = null);
        Task<AcademyActivityDto> GetByIdAsync(int id);
        Task<AcademyActionResult> CreateAsync(CreateAcademyActivityDto dto);
        Task<AcademyActionResult> UpdateAsync(UpdateAcademyActivityDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyActivitySessionService
    {
        Task<List<AcademyActivitySessionDto>> GetAllAsync(int? activityId = null, int? instructorId = null, string status = null);
        Task<AcademyActivitySessionDto> GetByIdAsync(int id);
        Task<AcademyActionResult> CreateAsync(CreateAcademyActivitySessionDto dto);
        Task<bool> UpdateAsync(UpdateAcademyActivitySessionDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyActivityRequestService
    {
        Task<List<AcademyActivityRequestDto>> GetAllAsync(int? sessionId = null, int? studentId = null, int? instructorId = null, string status = null);
        Task<AcademyActivityRequestDto> GetByIdAsync(int id);
        Task<AcademyActionResult> CreateAsync(CreateAcademyActivityRequestDto dto);
        Task<AcademyActionResult> ReviewAsync(ReviewAcademyActivityRequestDto dto, int? actingInstructorId = null);
    }
}
