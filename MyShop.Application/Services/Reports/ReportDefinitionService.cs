using MyShop.Application.Interfaces.Reports;
using MyShop.Core.Models.Reports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Reports
{
    public class ReportDefinitionService : IReportDefinitionService
    {
        private readonly IReportDefinitionRepository _repo;

        public ReportDefinitionService(IReportDefinitionRepository repo)
        {
            _repo = repo;
        }

        public Task<ReportDefinitionDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<ReportDefinitionDto>> GetAllAsync() => _repo.GetAllAsync();

        public async Task<int> CreateAsync(CreateReportDefinitionDto dto)
        {
            var entity = new ReportDefinition
            {
                Code = dto.Code,
                Name = dto.Name ?? "",
                Description = dto.Description,
                ModuleKey = dto.ModuleKey,
                QueryOrConfig = dto.QueryOrConfig,
                OutputType = dto.OutputType,
                ParametersJson = dto.ParametersJson,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateReportDefinitionDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new ReportDefinition
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name ?? "",
                Description = dto.Description,
                ModuleKey = dto.ModuleKey,
                QueryOrConfig = dto.QueryOrConfig,
                OutputType = dto.OutputType,
                ParametersJson = dto.ParametersJson,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
