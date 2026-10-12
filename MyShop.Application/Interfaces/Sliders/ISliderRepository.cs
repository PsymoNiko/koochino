using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface ISliderRepository
    {
        Task<List<SliderDto>> ListAsync(string? placeKey = null, bool? isActive = null);
        Task<SliderDto?> GetByIdAsync(int id);
        Task<int> InsertAsync(SliderDto dto);
        Task<bool> UpdateAsync(SliderDto dto);
        Task<bool> DeleteAsync(int id);

        Task<List<SliderSlideDto>> GetSlidesBySliderIdAsync(int sliderId, bool? isActive = null);
        Task<List<SliderSlideDto>> GetActiveSlidesByPlaceKeyAsync(string placeKey);
        Task<SliderSlideDto?> GetSlideByIdAsync(int id);
        Task<int> InsertSlideAsync(SliderSlideDto dto);
        Task<bool> UpdateSlideAsync(SliderSlideDto dto);
        Task<bool> DeleteSlideAsync(int id);
    }
}
