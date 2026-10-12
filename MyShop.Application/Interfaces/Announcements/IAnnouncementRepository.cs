using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IAnnouncementRepository
    {
        Task<AnnouncementDto> GetByIdAsync(int id);
        Task<List<AnnouncementDto>> GetAllAsync();
        Task<List<AnnouncementDto>> GetActiveForUserAsync(string userRole);
        Task<int> CreateAsync(Announcement announcement, int createdBy);
        Task<bool> UpdateAsync(Announcement announcement);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id, bool isActive);
    }
}
