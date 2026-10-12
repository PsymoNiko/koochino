using MyShop.Application.Interfaces.ProjectManagement;
using MyShop.Core.Models.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.ProjectManagement
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repo;

        public ProjectService(IProjectRepository repo)
        {
            _repo = repo;
        }

        public Task<ProjectDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<ProjectDto>> GetAllAsync(int? storeId = null, int? companyId = null) => _repo.GetAllAsync(storeId, companyId);

        public async Task<int> CreateAsync(CreateProjectDto dto)
        {
            var entity = new Project
            {
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                SiteId = dto.SiteId,
                Title = dto.Title ?? "",
                Description = dto.Description,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Planning" : dto.Status,
                Priority = dto.Priority,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Progress = dto.Progress,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateProjectDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new Project
            {
                Id = dto.Id,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                Title = dto.Title ?? "",
                Description = dto.Description,
                Status = dto.Status,
                Priority = dto.Priority,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Progress = dto.Progress,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
