using MyShop.Application.Interfaces.Contracts;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Contracts
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _repo;

        public ContractService(IContractRepository repo)
        {
            _repo = repo;
        }

        public Task<ContractDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<ContractDto>> GetAllAsync(int? storeId = null, int? companyId = null) => _repo.GetAllAsync(storeId, companyId);

        public async Task<int> CreateAsync(CreateContractDto dto, int? createdBy)
        {
            var entity = new Contract
            {
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                ContractNo = dto.ContractNo,
                Title = dto.Title ?? "",
                PartyName = dto.PartyName,
                PartyContact = dto.PartyContact,
                PartyPhone = dto.PartyPhone,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Value = dto.Value,
                Currency = dto.Currency,
                Status = dto.Status,
                Type = dto.Type,
                DocumentUrl = dto.DocumentUrl,
                Notes = dto.Notes,
                ReminderDays = dto.ReminderDays,
                RenewalCount = 0,
                ParentContractId = dto.ParentContractId,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateContractDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new Contract
            {
                Id = dto.Id,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                ContractNo = dto.ContractNo,
                Title = dto.Title ?? "",
                PartyName = dto.PartyName,
                PartyContact = dto.PartyContact,
                PartyPhone = dto.PartyPhone,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Value = dto.Value,
                Currency = dto.Currency,
                Status = dto.Status,
                Type = dto.Type,
                DocumentUrl = dto.DocumentUrl,
                Notes = dto.Notes,
                ReminderDays = dto.ReminderDays,
                ParentContractId = dto.ParentContractId,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
