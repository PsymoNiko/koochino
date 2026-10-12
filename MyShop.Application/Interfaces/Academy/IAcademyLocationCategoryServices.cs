using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyLocationRepository
    {
        Task<List<AcademyLocationDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<AcademyLocationDto> GetByIdAsync(int id);
        Task<int> InsertAsync(CreateAcademyLocationDto dto);
        Task<bool> UpdateAsync(UpdateAcademyLocationDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyLocationService
    {
        Task<List<AcademyLocationDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<AcademyLocationDto> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateAcademyLocationDto dto);
        Task<bool> UpdateAsync(UpdateAcademyLocationDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyActivityCategoryRepository
    {
        Task<List<AcademyActivityCategoryDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<AcademyActivityCategoryDto> GetByIdAsync(int id);
        Task<int> InsertAsync(CreateAcademyActivityCategoryDto dto);
        Task<int> UpdateAsync(UpdateAcademyActivityCategoryDto dto);
        Task<int> DeleteAsync(int id);
    }

    public interface IAcademyActivityCategoryService
    {
        Task<List<AcademyActivityCategoryDto>> GetAllAsync(int? siteId = null, bool? isActive = null);
        Task<List<AcademyActivityCategoryDto>> GetTreeAsync(int? siteId = null, bool? isActive = null);
        Task<AcademyActivityCategoryDto> GetByIdAsync(int id);
        Task<AcademyActionResult> CreateAsync(CreateAcademyActivityCategoryDto dto);
        Task<AcademyActionResult> UpdateAsync(UpdateAcademyActivityCategoryDto dto);
        Task<AcademyActionResult> DeleteAsync(int id);
    }
}
