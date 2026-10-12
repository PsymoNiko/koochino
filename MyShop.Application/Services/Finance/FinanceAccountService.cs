using MyShop.Application.Interfaces.Finance;
using MyShop.Core.Models.Finance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Finance
{
    public class FinanceAccountService : IFinanceAccountService
    {
        private readonly IFinanceAccountRepository _repo;

        public FinanceAccountService(IFinanceAccountRepository repo)
        {
            _repo = repo;
        }

        public Task<FinanceAccountDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<FinanceAccountDto>> GetAllAsync(int? storeId = null, int? companyId = null) => _repo.GetAllAsync(storeId, companyId);

        public async Task<int> CreateAsync(CreateFinanceAccountDto dto)
        {
            var entity = new FinanceAccount
            {
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                ParentId = dto.ParentId,
                Code = dto.Code,
                Name = dto.Name ?? "",
                Type = dto.Type,
                Balance = dto.Balance,
                Currency = dto.Currency,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateFinanceAccountDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new FinanceAccount
            {
                Id = dto.Id,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                ParentId = dto.ParentId,
                Code = dto.Code,
                Name = dto.Name ?? "",
                Type = dto.Type,
                Balance = dto.Balance,
                Currency = dto.Currency,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
