using MyShop.Application.Interfaces.Assets;
using MyShop.Core.Models.Assets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Assets
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _repo;

        public AssetService(IAssetRepository repo)
        {
            _repo = repo;
        }

        public Task<AssetDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<AssetDto>> GetAllAsync(int? storeId = null, int? companyId = null, int? categoryId = null) => _repo.GetAllAsync(storeId, companyId, categoryId);
        public Task<List<AssetCategoryItemDto>> GetCategoriesAsync() => _repo.GetCategoriesAsync();

        public async Task<int> CreateAsync(CreateAssetDto dto)
        {
            var entity = new Asset
            {
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                CategoryId = dto.CategoryId,
                Code = dto.Code,
                Name = dto.Name ?? "",
                Description = dto.Description,
                PurchaseDate = dto.PurchaseDate,
                PurchasePrice = dto.PurchasePrice,
                CurrentValue = dto.CurrentValue,
                Location = dto.Location,
                AssignedTo = dto.AssignedTo,
                PersonnelId = dto.PersonnelId,
                Status = dto.Status,
                WarrantyEndDate = dto.WarrantyEndDate,
                DepreciationRate = dto.DepreciationRate,
                ImageUrl = dto.ImageUrl,
                SerialNo = dto.SerialNo,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateAssetDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new Asset
            {
                Id = dto.Id,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                CategoryId = dto.CategoryId,
                Code = dto.Code,
                Name = dto.Name ?? "",
                Description = dto.Description,
                PurchaseDate = dto.PurchaseDate,
                PurchasePrice = dto.PurchasePrice,
                CurrentValue = dto.CurrentValue,
                Location = dto.Location,
                AssignedTo = dto.AssignedTo,
                PersonnelId = dto.PersonnelId,
                Status = dto.Status,
                WarrantyEndDate = dto.WarrantyEndDate,
                DepreciationRate = dto.DepreciationRate,
                ImageUrl = dto.ImageUrl,
                SerialNo = dto.SerialNo,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
