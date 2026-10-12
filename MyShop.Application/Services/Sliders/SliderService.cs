using MyShop.Application.Interfaces;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class SliderService : ISliderService
    {
        private readonly ISliderRepository _repository;

        public SliderService(ISliderRepository repository)
        {
            _repository = repository;
        }

        public Task<List<SliderDto>> ListAsync(string? placeKey = null, bool? isActive = null)
            => _repository.ListAsync(placeKey, isActive);

        public Task<SliderDto?> GetByIdAsync(int id)
            => _repository.GetByIdAsync(id);

        public async Task<int> CreateAsync(CreateSliderDto dto)
        {
            var entity = new SliderDto
            {
                Name = dto.Name,
                PlaceKey = dto.PlaceKey,
                Description = dto.Description,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            };
            return await _repository.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateSliderDto dto)
        {
            var entity = new SliderDto
            {
                Id = dto.Id,
                Name = dto.Name,
                PlaceKey = dto.PlaceKey,
                Description = dto.Description,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            };
            return await _repository.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id)
            => _repository.DeleteAsync(id);

        public Task<List<SliderSlideDto>> GetSlidesBySliderIdAsync(int sliderId, bool? isActive = null)
            => _repository.GetSlidesBySliderIdAsync(sliderId, isActive);

        public Task<List<SliderSlideDto>> GetActiveSlidesByPlaceKeyAsync(string placeKey)
            => _repository.GetActiveSlidesByPlaceKeyAsync(placeKey);

        public Task<SliderSlideDto?> GetSlideByIdAsync(int id)
            => _repository.GetSlideByIdAsync(id);

        public async Task<int> CreateSlideAsync(CreateSliderSlideDto dto)
        {
            var entity = new SliderSlideDto
            {
                SliderId = dto.SliderId,
                Title = dto.Title,
                Subtitle = dto.Subtitle,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                ImageAltText = dto.ImageAltText,
                LinkUrl = dto.LinkUrl,
                LinkTarget = dto.LinkTarget ?? "_self",
                ButtonText = dto.ButtonText,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };
            return await _repository.InsertSlideAsync(entity);
        }

        public async Task<bool> UpdateSlideAsync(UpdateSliderSlideDto dto)
        {
            var entity = new SliderSlideDto
            {
                Id = dto.Id,
                SliderId = dto.SliderId,
                Title = dto.Title,
                Subtitle = dto.Subtitle,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                ImageAltText = dto.ImageAltText,
                LinkUrl = dto.LinkUrl,
                LinkTarget = dto.LinkTarget ?? "_self",
                ButtonText = dto.ButtonText,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };
            return await _repository.UpdateSlideAsync(entity);
        }

        public Task<bool> DeleteSlideAsync(int id)
            => _repository.DeleteSlideAsync(id);
    }
}
