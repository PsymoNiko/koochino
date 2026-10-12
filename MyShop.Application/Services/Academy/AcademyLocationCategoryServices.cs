using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyLocationService : IAcademyLocationService
    {
        private readonly IAcademyLocationRepository _repo;
        private readonly ICurrentTenant _tenant;
        public AcademyLocationService(IAcademyLocationRepository repo, ICurrentTenant tenant = null)
        {
            _repo = repo;
            _tenant = tenant;
        }

        public Task<List<AcademyLocationDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
            => _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant) ?? siteId, isActive);

        public async Task<AcademyLocationDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return Owns(item?.SiteId) ? item : null;
        }

        public async Task<int> CreateAsync(CreateAcademyLocationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return 0;
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            return await _repo.InsertAsync(dto);
        }

        public async Task<bool> UpdateAsync(UpdateAcademyLocationDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (!Owns(existing?.SiteId)) return false;
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            return await _repo.UpdateAsync(dto);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            return Owns(existing?.SiteId) && await _repo.DeleteAsync(id);
        }

        private bool Owns(int? siteId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            return !current.HasValue || siteId == current;
        }
    }

    public class AcademyActivityCategoryService : IAcademyActivityCategoryService
    {
        private readonly IAcademyActivityCategoryRepository _repo;
        private readonly ICurrentTenant _tenant;
        public AcademyActivityCategoryService(IAcademyActivityCategoryRepository repo, ICurrentTenant tenant = null)
        {
            _repo = repo;
            _tenant = tenant;
        }

        public Task<List<AcademyActivityCategoryDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
            => _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant) ?? siteId, isActive);

        public async Task<List<AcademyActivityCategoryDto>> GetTreeAsync(int? siteId = null, bool? isActive = null)
        {
            var flat = await _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant) ?? siteId, isActive);
            var lookup = flat.ToDictionary(x => x.Id);
            foreach (var item in flat)
                item.Children = new List<AcademyActivityCategoryDto>();

            var roots = new List<AcademyActivityCategoryDto>();
            foreach (var item in flat.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
            {
                if (item.ParentId.HasValue && item.ParentId.Value > 0 && lookup.TryGetValue(item.ParentId.Value, out var parent))
                    parent.Children.Add(item);
                else
                    roots.Add(item);
            }
            return roots;
        }

        public async Task<AcademyActivityCategoryDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return Owns(item?.SiteId) ? item : null;
        }

        public async Task<AcademyActionResult> CreateAsync(CreateAcademyActivityCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return AcademyActionResult.Fail("نام دسته الزامی است.");
            if (dto.ParentId.HasValue && dto.ParentId.Value <= 0)
                dto.ParentId = null;
            if (!await OwnsParentAsync(dto.ParentId))
                return AcademyActionResult.Fail("دسته والد یافت نشد.");
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;

            var id = await _repo.InsertAsync(dto);
            if (id == -2) return AcademyActionResult.Fail("دسته والد یافت نشد.");
            if (id <= 0) return AcademyActionResult.Fail("خطا در ایجاد دسته.");
            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> UpdateAsync(UpdateAcademyActivityCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return AcademyActionResult.Fail("نام دسته الزامی است.");
            if (dto.ParentId.HasValue && dto.ParentId.Value <= 0)
                dto.ParentId = null;
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (!Owns(existing?.SiteId))
                return AcademyActionResult.Fail("دسته یافت نشد.");
            if (!await OwnsParentAsync(dto.ParentId))
                return AcademyActionResult.Fail("دسته والد یافت نشد.");
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;

            var code = await _repo.UpdateAsync(dto);
            return code switch
            {
                1 => AcademyActionResult.Success(dto.Id),
                -2 => AcademyActionResult.Fail("دسته والد یافت نشد."),
                -3 => AcademyActionResult.Fail("دسته نمی‌تواند والد خودش باشد."),
                -4 => AcademyActionResult.Fail("این انتخاب باعث حلقه در درخت می‌شود."),
                _ => AcademyActionResult.Fail("به‌روزرسانی ناموفق بود.")
            };
        }

        public async Task<AcademyActionResult> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (!Owns(existing?.SiteId))
                return AcademyActionResult.Fail("دسته یافت نشد.");
            var code = await _repo.DeleteAsync(id);
            return code switch
            {
                1 => AcademyActionResult.Success(id),
                -5 => AcademyActionResult.Fail("ابتدا زیرمجموعه‌ها را حذف کنید."),
                -6 => AcademyActionResult.Fail("این دسته به مربی تخصیص داده شده و قابل حذف نیست."),
                _ => AcademyActionResult.Fail("حذف ناموفق بود.")
            };
        }

        private bool Owns(int? siteId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            return !current.HasValue || siteId == current;
        }

        private async Task<bool> OwnsParentAsync(int? parentId)
        {
            if (!parentId.HasValue) return true;
            var parent = await _repo.GetByIdAsync(parentId.Value);
            return parent != null && Owns(parent.SiteId);
        }
    }
}
