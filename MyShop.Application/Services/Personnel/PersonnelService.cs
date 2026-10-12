using MyShop.Application.Interfaces.Personnel;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Personnel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Personnel
{
    public class PersonnelService : IPersonnelService
    {
        private readonly IPersonnelRepository _repo;
        private readonly ICurrentTenant _tenant;

        public PersonnelService(IPersonnelRepository repo, ICurrentTenant tenant)
        {
            _repo = repo;
            _tenant = tenant;
        }

        public Task<PersonnelDto> GetByIdAsync(int id)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue ? _repo.GetByIdAsync(id, siteId.Value) : Task.FromResult<PersonnelDto>(null);
        }

        public Task<List<PersonnelDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue
                ? _repo.GetAllAsync(siteId.Value, storeId, companyId, isActive)
                : Task.FromResult(new List<PersonnelDto>());
        }

        public async Task<int> CreateAsync(CreatePersonnelDto dto)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (!siteId.HasValue) return 0;
            var entity = new MyShop.Core.Models.Personnel.Personnel
            {
                SiteId = siteId.Value,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                FullName = dto.FullName ?? "",
                NationalCode = dto.NationalCode,
                Phone = dto.Phone,
                Email = dto.Email,
                Position = dto.Position,
                Department = dto.Department,
                ResumeText = dto.ResumeText,
                ResumeFileUrl = dto.ResumeFileUrl,
                HireDate = dto.HireDate,
                LeaveDate = dto.LeaveDate,
                BirthDate = dto.BirthDate,
                Address = dto.Address,
                Specifications = dto.Specifications,
                DefaultWorkStartTime = dto.DefaultWorkStartTime,
                DefaultWorkEndTime = dto.DefaultWorkEndTime,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdatePersonnelDto dto)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (!siteId.HasValue) return false;
            var existing = await _repo.GetByIdAsync(dto.Id, siteId.Value);
            if (existing == null) return false;
            var entity = new MyShop.Core.Models.Personnel.Personnel
            {
                Id = dto.Id,
                SiteId = siteId.Value,
                StoreId = dto.StoreId,
                CompanyId = dto.CompanyId,
                FullName = dto.FullName ?? "",
                NationalCode = dto.NationalCode,
                Phone = dto.Phone,
                Email = dto.Email,
                Position = dto.Position,
                Department = dto.Department,
                ResumeText = dto.ResumeText,
                ResumeFileUrl = dto.ResumeFileUrl,
                HireDate = dto.HireDate,
                LeaveDate = dto.LeaveDate,
                BirthDate = dto.BirthDate,
                Address = dto.Address,
                Specifications = dto.Specifications,
                DefaultWorkStartTime = dto.DefaultWorkStartTime,
                DefaultWorkEndTime = dto.DefaultWorkEndTime,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue ? _repo.DeleteAsync(id, siteId.Value) : Task.FromResult(false);
        }
    }
}
