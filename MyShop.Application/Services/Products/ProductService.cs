using MyShop.Application.Interfaces.Products;
using MyShop.Core.Models.Products;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo)
        {
            _repo = repo;
        }

        public Task<ProductDto> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<ProductDto>> GetAllAsync(int? storeId = null, int? companyId = null) => _repo.GetAllAsync(storeId, companyId);

        public async Task<int> CreateAsync(CreateProductDto dto)
        {
            var entity = new Product
            {
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                CategoryId = dto.CategoryId,
                UnitId = dto.UnitId,
                Name = dto.Name ?? "",
                Slug = dto.Slug,
                Description = dto.Description,
                ShortDescription = dto.ShortDescription,
                Price = dto.Price,
                CompareAtPrice = dto.CompareAtPrice,
                Cost = dto.Cost,
                Sku = dto.Sku,
                Barcode = dto.Barcode,
                Quantity = dto.Quantity,
                IsActive = dto.IsActive,
                IsFeatured = dto.IsFeatured,
                ImageUrl = dto.ImageUrl,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            var id = await _repo.InsertAsync(entity);
            if (id > 0 && dto.UnitId.HasValue)
                await _repo.SetUnitIdAsync(id, dto.UnitId);
            return id;
        }

        public async Task<bool> UpdateAsync(UpdateProductDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) return false;
            var entity = new Product
            {
                Id = dto.Id,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                CategoryId = dto.CategoryId,
                UnitId = dto.UnitId,
                Name = dto.Name ?? "",
                Slug = dto.Slug,
                Description = dto.Description,
                ShortDescription = dto.ShortDescription,
                Price = dto.Price,
                CompareAtPrice = dto.CompareAtPrice,
                Cost = dto.Cost,
                Sku = dto.Sku,
                Barcode = dto.Barcode,
                Quantity = dto.Quantity,
                IsActive = dto.IsActive,
                IsFeatured = dto.IsFeatured,
                ImageUrl = dto.ImageUrl,
                SortOrder = dto.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };
            var ok = await _repo.UpdateAsync(entity);
            if (ok)
                await _repo.SetUnitIdAsync(dto.Id, dto.UnitId);
            return ok;
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
