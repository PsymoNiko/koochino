using MyShop.Application.Interfaces.Companies;
using MyShop.Core.Models.Companies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Companies
{
    public class HoldingService : IHoldingService
    {
        private readonly IHoldingRepository _repo;

        public HoldingService(IHoldingRepository repo) => _repo = repo;

        public Task<HoldingDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<HoldingDto>> GetAllAsync(bool? isActive = null) => _repo.GetAllAsync(isActive);

        public async Task<int> CreateAsync(CreateHoldingDto dto)
        {
            var entity = new Holding
            {
                Code = dto.Code ?? "",
                Name = dto.Name ?? "",
                Description = dto.Description,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateHoldingDto dto)
        {
            var entity = new Holding
            {
                Id = dto.Id,
                Code = dto.Code ?? "",
                Name = dto.Name ?? "",
                Description = dto.Description,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
