using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IAnnouncementRepository _repository;

        public AnnouncementService(IAnnouncementRepository repository)
        {
            _repository = repository;
        }

        public Task<AnnouncementDto> GetByIdAsync(int id)
            => _repository.GetByIdAsync(id);

        public Task<List<AnnouncementDto>> GetAllAsync()
            => _repository.GetAllAsync();

        public Task<List<AnnouncementDto>> GetActiveForUserAsync(string userRole)
            => _repository.GetActiveForUserAsync(userRole);

        public async Task<int> CreateAsync(CreateAnnouncementDto dto, int createdBy)
        {
            var announcement = new Announcement
            {
                Title = dto.Title,
                Message = HtmlSanitize.Sanitize(dto.Message),
                IsActive = dto.IsActive,
                AlertColor = dto.AlertColor,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                TargetAudience = dto.TargetAudience,
                CreatedAt = DateTime.Now
            };

            return await _repository.CreateAsync(announcement, createdBy);
        }

        public async Task<bool> UpdateAsync(UpdateAnnouncementDto dto)
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing == null)
                return false;

            var announcement = new Announcement
            {
                Id = dto.Id,
                Title = dto.Title,
                Message = HtmlSanitize.Sanitize(dto.Message),
                IsActive = dto.IsActive,
                AlertColor = dto.AlertColor,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                TargetAudience = dto.TargetAudience,
                UpdatedAt = DateTime.Now
            };

            return await _repository.UpdateAsync(announcement);
        }

        public Task<bool> DeleteAsync(int id)
            => _repository.DeleteAsync(id);

        public Task<bool> ToggleActiveAsync(int id, bool isActive)
            => _repository.ToggleActiveAsync(id, isActive);
    }
}
