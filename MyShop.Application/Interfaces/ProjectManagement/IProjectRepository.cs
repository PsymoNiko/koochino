using MyShop.Core.Models.ProjectManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.ProjectManagement
{
    public interface IProjectRepository
    {
        Task<ProjectDto> GetByIdAsync(int id);
        Task<List<ProjectDto>> GetAllAsync(int? storeId = null, int? companyId = null);
        Task<int> InsertAsync(Project entity);
        Task<bool> UpdateAsync(Project entity);
        Task<bool> DeleteAsync(int id);
    }
}
