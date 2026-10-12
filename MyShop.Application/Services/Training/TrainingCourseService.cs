using MyShop.Application.Interfaces.Training;
using MyShop.Core.Models.Training;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Training
{
    public class TrainingCourseService : ITrainingCourseService
    {
        private readonly ITrainingCourseRepository _repo;

        public TrainingCourseService(ITrainingCourseRepository repo)
        {
            _repo = repo;
        }

        public Task<TrainingCourseDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<TrainingCourseDto>> GetAllAsync(int? storeId = null, int? companyId = null) => _repo.GetAllAsync(storeId, companyId);

        public async Task<int> CreateAsync(CreateTrainingCourseDto dto)
        {
            var entity = new TrainingCourse
            {
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                Code = dto.Code,
                Title = dto.Title ?? "",
                Description = dto.Description,
                DurationHours = dto.DurationHours,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MaxParticipants = dto.MaxParticipants,
                Location = dto.Location,
                InstructorName = dto.InstructorName,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateTrainingCourseDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new TrainingCourse
            {
                Id = dto.Id,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                Code = dto.Code,
                Title = dto.Title ?? "",
                Description = dto.Description,
                DurationHours = dto.DurationHours,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MaxParticipants = dto.MaxParticipants,
                Location = dto.Location,
                InstructorName = dto.InstructorName,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
