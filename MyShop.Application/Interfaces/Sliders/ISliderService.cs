using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface ISliderService
    {
        Task<List<SliderDto>> ListAsync(string? placeKey = null, bool? isActive = null);
        Task<SliderDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateSliderDto dto);
        Task<bool> UpdateAsync(UpdateSliderDto dto);
        Task<bool> DeleteAsync(int id);

        Task<List<SliderSlideDto>> GetSlidesBySliderIdAsync(int sliderId, bool? isActive = null);
        Task<List<SliderSlideDto>> GetActiveSlidesByPlaceKeyAsync(string placeKey);
        Task<SliderSlideDto?> GetSlideByIdAsync(int id);
        Task<int> CreateSlideAsync(CreateSliderSlideDto dto);
        Task<bool> UpdateSlideAsync(UpdateSliderSlideDto dto);
        Task<bool> DeleteSlideAsync(int id);
    }
}
