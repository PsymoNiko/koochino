using MyShop.Application.Interfaces.Crm;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Crm
{
    public class CrmContactService : ICrmContactService
    {
        private readonly ICrmContactRepository _repo;
        private readonly ICurrentTenant _tenant;
        private readonly IDataScopeResolver _scope;

        public CrmContactService(ICrmContactRepository repo, ICurrentTenant tenant, IDataScopeResolver scope = null)
        {
            _repo = repo;
            _tenant = tenant;
            _scope = scope;
        }

        public Task<CrmContactDto> GetByIdAsync(int id)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue ? _repo.GetByIdAsync(id, siteId.Value) : Task.FromResult<CrmContactDto>(null);
        }

        public Task<CrmContactDto> FindByUserIdAsync(int userId)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue && userId > 0
                ? _repo.GetByUserIdAsync(siteId.Value, userId)
                : Task.FromResult<CrmContactDto>(null);
        }

        public Task<CrmContactDto> FindByPhoneAsync(string phone)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            var normalizedPhone = PhoneNormalizer.Normalize(phone);
            return siteId.HasValue && !string.IsNullOrWhiteSpace(normalizedPhone)
                ? _repo.GetByPhoneAsync(siteId.Value, normalizedPhone)
                : Task.FromResult<CrmContactDto>(null);
        }

        public Task<List<CrmContactDto>> GetAllAsync(int? storeId = null, int? companyId = null, bool? isActive = null)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue
                ? _repo.GetAllAsync(siteId.Value, storeId, companyId, isActive)
                : Task.FromResult(new List<CrmContactDto>());
        }

        public async Task<List<CrmContactDto>> GetAllForUserAsync(int userId, int? storeId = null, int? companyId = null, bool? isActive = null, int? siteId = null)
        {
            var sid = siteId ?? SiteScope.CurrentSiteId(_tenant);
            if (!sid.HasValue) return new List<CrmContactDto>();
            if (_scope == null || userId <= 0)
                return await _repo.GetAllAsync(sid.Value, storeId, companyId, isActive);
            var query = await _scope.BuildQueryAsync(userId, "Users.View", sid);
            return await _repo.GetAllScopedAsync(query, sid.Value, storeId, companyId, isActive);
        }

        public async Task<int> CreateAsync(CreateCrmContactDto dto, int? createdBy)
            => await UpsertAsync(dto, createdBy);

        public async Task<int> UpsertAsync(CreateCrmContactDto dto, int? createdBy)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (!siteId.HasValue) return 0;

            dto ??= new CreateCrmContactDto();
            dto.Phone = PhoneNormalizer.Normalize(dto.Phone);
            if (dto.UserId.HasValue &&
                (dto.UserId.Value <= 0 || !await _repo.UserBelongsToSiteAsync(dto.UserId.Value, siteId.Value)))
                return 0;

            CrmContactDto existing = null;
            if (dto.UserId.HasValue)
                existing = await _repo.GetByUserIdAsync(siteId.Value, dto.UserId.Value);
            if (existing == null && !string.IsNullOrWhiteSpace(dto.Phone))
                existing = await _repo.GetByPhoneAsync(siteId.Value, dto.Phone);

            if (existing != null)
            {
                var merged = MergeEmpty(existing, dto, siteId.Value);
                return await _repo.UpdateAsync(merged) ? existing.Id : 0;
            }

            var entity = new CrmContact
            {
                SiteId = siteId.Value, UserId = dto.UserId, StoreId = dto.StoreId, CompanyId = dto.CompanyId, FullName = dto.FullName ?? "", Phone = dto.Phone, Phone2 = PhoneNormalizer.Normalize(dto.Phone2),
                Email = dto.Email, Company = dto.Company, JobTitle = dto.JobTitle, Address = dto.Address,
                City = dto.City, Province = dto.Province, PostalCode = dto.PostalCode, Source = dto.Source,
                Tags = dto.Tags, Notes = dto.Notes, AssignedTo = dto.AssignedTo, Status = dto.Status,
                Rating = dto.Rating, IsActive = dto.IsActive, CreatedBy = createdBy, CreatedAt = DateTime.UtcNow
            };
            return await _repo.InsertAsync(entity);
        }

        public async Task<bool> LinkUserAsync(int contactId, int userId)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (!siteId.HasValue || userId <= 0 || !await _repo.UserBelongsToSiteAsync(userId, siteId.Value))
                return false;

            var contact = await _repo.GetByIdAsync(contactId, siteId.Value);
            if (contact == null) return false;

            var alreadyLinked = await _repo.GetByUserIdAsync(siteId.Value, userId);
            if (alreadyLinked != null && alreadyLinked.Id != contactId) return false;

            var entity = ToEntity(contact, siteId.Value);
            entity.UserId = userId;
            entity.UpdatedAt = DateTime.UtcNow;
            return await _repo.UpdateAsync(entity);
        }

        public async Task<bool> UpdateAsync(UpdateCrmContactDto dto)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (!siteId.HasValue) return false;
            var existing = await _repo.GetByIdAsync(dto.Id, siteId.Value);
            if (existing == null) return false;
            var userId = dto.UserId ?? existing.UserId;
            if (userId.HasValue)
            {
                if (!await _repo.UserBelongsToSiteAsync(userId.Value, siteId.Value)) return false;
                var linked = await _repo.GetByUserIdAsync(siteId.Value, userId.Value);
                if (linked != null && linked.Id != dto.Id) return false;
            }
            var entity = new CrmContact
            {
                Id = dto.Id, SiteId = siteId.Value, UserId = userId, StoreId = dto.StoreId, CompanyId = dto.CompanyId, FullName = dto.FullName ?? "", Phone = PhoneNormalizer.Normalize(dto.Phone), Phone2 = PhoneNormalizer.Normalize(dto.Phone2),
                Email = dto.Email, Company = dto.Company, JobTitle = dto.JobTitle, Address = dto.Address,
                City = dto.City, Province = dto.Province, PostalCode = dto.PostalCode, Source = dto.Source,
                Tags = dto.Tags, Notes = dto.Notes, AssignedTo = dto.AssignedTo, Status = dto.Status,
                Rating = dto.Rating, IsActive = dto.IsActive, UpdatedAt = DateTime.UtcNow
            };
            return await _repo.UpdateAsync(entity);
        }

        private static CrmContact MergeEmpty(CrmContactDto current, CreateCrmContactDto incoming, int siteId)
        {
            var entity = ToEntity(current, siteId);
            entity.UserId ??= incoming.UserId;
            entity.StoreId ??= incoming.StoreId;
            entity.CompanyId ??= incoming.CompanyId;
            entity.FullName = EmptyThen(entity.FullName, incoming.FullName);
            entity.Phone = EmptyThen(entity.Phone, incoming.Phone);
            entity.Phone2 = EmptyThen(entity.Phone2, PhoneNormalizer.Normalize(incoming.Phone2));
            entity.Email = EmptyThen(entity.Email, incoming.Email);
            entity.Company = EmptyThen(entity.Company, incoming.Company);
            entity.JobTitle = EmptyThen(entity.JobTitle, incoming.JobTitle);
            entity.Address = EmptyThen(entity.Address, incoming.Address);
            entity.City = EmptyThen(entity.City, incoming.City);
            entity.Province = EmptyThen(entity.Province, incoming.Province);
            entity.PostalCode = EmptyThen(entity.PostalCode, incoming.PostalCode);
            entity.Source = EmptyThen(entity.Source, incoming.Source);
            entity.Notes = EmptyThen(entity.Notes, incoming.Notes);
            entity.AssignedTo ??= incoming.AssignedTo;
            entity.Status = EmptyThen(entity.Status, incoming.Status);
            entity.Rating ??= incoming.Rating;
            entity.Tags = UnionTags(entity.Tags, incoming.Tags);
            entity.UpdatedAt = DateTime.UtcNow;
            return entity;
        }

        private static CrmContact ToEntity(CrmContactDto dto, int siteId) => new CrmContact
        {
            Id = dto.Id, SiteId = siteId, UserId = dto.UserId, CompanyId = dto.CompanyId, StoreId = dto.StoreId,
            FullName = dto.FullName, Phone = dto.Phone, Phone2 = dto.Phone2, Email = dto.Email,
            Company = dto.Company, JobTitle = dto.JobTitle, Address = dto.Address, City = dto.City,
            Province = dto.Province, PostalCode = dto.PostalCode, Source = dto.Source, Tags = dto.Tags,
            Notes = dto.Notes, AssignedTo = dto.AssignedTo, Status = dto.Status, Rating = dto.Rating,
            IsActive = dto.IsActive, CreatedBy = dto.CreatedBy, CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt, LastActivityAt = dto.LastActivityAt
        };

        private static string EmptyThen(string current, string incoming) =>
            string.IsNullOrWhiteSpace(current) ? incoming : current;

        private static string UnionTags(string current, string incoming)
        {
            var tags = (current ?? "").Split(new[] { ',', '،', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Concat((incoming ?? "").Split(new[] { ',', '،', ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", tags);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue ? _repo.DeleteAsync(id, siteId.Value) : Task.FromResult(false);
        }
    }
}
