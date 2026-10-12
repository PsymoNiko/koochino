using MyShop.Application.Interfaces.Inventory;
using MyShop.Core.Models.Inventory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Inventory
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _repo;

        public WarehouseService(IWarehouseRepository repo) => _repo = repo;

        public Task<WarehouseDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<WarehouseDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null) => _repo.GetAllAsync(storeId, companyId, isActive);

        public async Task<int> CreateAsync(CreateWarehouseDto dto)
        {
            var entity = new Warehouse
            {
                StoreId = dto.StoreId, CompanyId = dto.CompanyId, Code = dto.Code, Name = dto.Name ?? "",
                Address = dto.Address, Phone = dto.Phone, ManagerName = dto.ManagerName,
                IsActive = dto.IsActive, SortOrder = dto.SortOrder, CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateWarehouseDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new Warehouse
            {
                Id = dto.Id, StoreId = dto.StoreId, CompanyId = dto.CompanyId, Code = dto.Code, Name = dto.Name ?? "",
                Address = dto.Address, Phone = dto.Phone, ManagerName = dto.ManagerName,
                IsActive = dto.IsActive, SortOrder = dto.SortOrder, UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
