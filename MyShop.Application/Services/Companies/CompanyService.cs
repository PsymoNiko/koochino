using MyShop.Application.Interfaces.Companies;
using MyShop.Core.Models.Companies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Companies
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _repo;

        public CompanyService(ICompanyRepository repo) => _repo = repo;

        public Task<CompanyDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<CompanyDto>> GetAllAsync(int? holdingId = null, bool? isActive = null) => _repo.GetAllAsync(holdingId, isActive);

        public async Task<int> CreateAsync(CreateCompanyDto dto)
        {
            var entity = new Company
            {
                HoldingId = dto.HoldingId,
                Code = dto.Code ?? "",
                Name = dto.Name ?? "",
                Address = dto.Address,
                Phone = dto.Phone,
                Description = dto.Description,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateCompanyDto dto)
        {
            var entity = new Company
            {
                Id = dto.Id,
                HoldingId = dto.HoldingId,
                Code = dto.Code ?? "",
                Name = dto.Name ?? "",
                Address = dto.Address,
                Phone = dto.Phone,
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
