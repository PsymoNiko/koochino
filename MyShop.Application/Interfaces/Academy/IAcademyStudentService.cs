using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyStudentRepository
    {
        Task<List<AcademyStudentDto>> GetAllAsync(int? siteId = null, string status = null);
        Task<AcademyStudentDto> GetByIdAsync(int id);
        Task<AcademyStudentDto> GetByUserIdAsync(int userId);
        Task<int> InsertAsync(int? siteId, int userId, string status, string notes, bool mustChangePassword);
        Task<bool> UpdateAsync(UpdateAcademyStudentDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyInstructorRepository
    {
        Task<List<AcademyInstructorDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<AcademyInstructorDto> GetByIdAsync(int id);
        Task<AcademyInstructorDto> GetByUserIdAsync(int userId);
        Task<int> InsertAsync(int? siteId, int personnelId, int userId, string bio, bool isActive, bool mustChangePassword);
        Task<bool> UpdateAsync(UpdateAcademyInstructorDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<int>> GetCategoryIdsAsync(int instructorId);
        Task SetCategoryIdsAsync(int instructorId, IEnumerable<int> categoryIds);
        Task<int> GetRoleIdByNameAsync(string roleName);
    }

    public interface IAcademyStudentService
    {
        Task<List<AcademyStudentDto>> GetAllAsync(int? siteId = null, string status = null);
        Task<AcademyStudentDto> GetByIdAsync(int id);
        Task<AcademyStudentDto> GetByUserIdAsync(int userId);
        Task<AcademyActionResult> CreateAsync(CreateAcademyStudentDto dto);
        Task<AcademyActionResult> RegisterSelfAsync(string phoneNumber, string password, int? siteId);
        Task<bool> UpdateAsync(UpdateAcademyStudentDto dto);
        Task<bool> ClearMustChangePasswordAsync(int studentId);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyInstructorService
    {
        Task<List<AcademyInstructorDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<AcademyInstructorDto> GetByIdAsync(int id);
        Task<AcademyInstructorDto> GetByUserIdAsync(int userId);
        Task<AcademyActionResult> CreateFromPersonnelAsync(CreateAcademyInstructorDto dto);
        Task<AcademyActionResult> RegisterSelfAsync(string phoneNumber, string password, int? siteId);
        Task<bool> UpdateAsync(UpdateAcademyInstructorDto dto);
        Task<bool> ClearMustChangePasswordAsync(int instructorId);
        Task<bool> DeleteAsync(int id);
    }
}
