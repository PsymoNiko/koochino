using MyShop.Core.Models.ProjectManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.ProjectManagement
{
    public interface IProjectService
    {
        Task<ProjectDto> GetByIdAsync(int id);
        Task<List<ProjectDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> CreateAsync(CreateProjectDto dto);
        Task<bool> UpdateAsync(UpdateProjectDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
