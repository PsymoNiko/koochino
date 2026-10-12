using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IAnnouncementService
    {
        Task<AnnouncementDto> GetByIdAsync(int id);
        Task<List<AnnouncementDto>> GetAllAsync();
        Task<List<AnnouncementDto>> GetActiveForUserAsync(string userRole);
        Task<int> CreateAsync(CreateAnnouncementDto dto, int createdBy);
        Task<bool> UpdateAsync(UpdateAnnouncementDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id, bool isActive);
    }
}
