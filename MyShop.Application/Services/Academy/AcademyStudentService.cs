using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Interfaces.Crm;
using MyShop.Core.Helpers;
using MyShop.Core.Models.Crm;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyStudentService : IAcademyStudentService
    {
        private readonly IAcademyStudentRepository _repo;
        private readonly IAcademyInstructorRepository _roleRepo;
        private readonly IAuthRepository _auth;
        private readonly ICurrentTenant _tenant;
        private readonly ICrmContactService _crm;

        public AcademyStudentService(
            IAcademyStudentRepository repo,
            IAcademyInstructorRepository roleRepo,
            IAuthRepository auth,
            ICurrentTenant tenant = null,
            ICrmContactService crm = null)
        {
            _repo = repo;
            _roleRepo = roleRepo;
            _auth = auth;
            _tenant = tenant;
            _crm = crm;
        }

        public Task<List<AcademyStudentDto>> GetAllAsync(int? siteId = null, string status = null)
            => _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant) ?? siteId, status);

        public async Task<AcademyStudentDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return Owns(item?.SiteId) ? item : null;
        }

        public async Task<AcademyStudentDto> GetByUserIdAsync(int userId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue) return await _repo.GetByUserIdAsync(userId);
            return (await _repo.GetAllAsync(current)).FirstOrDefault(x => x.UserId == userId);
        }

        public async Task<AcademyActionResult> CreateAsync(CreateAcademyStudentDto dto)
        {
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            var roleId = await _roleRepo.GetRoleIdByNameAsync(AcademyConstants.RoleStudent);
            if (roleId <= 0)
                return AcademyActionResult.Fail("نقش Student در دیتابیس تعریف نشده است. اسکریپت Phase0 را اجرا کنید.");

            int userId;
            if (dto.UserId.HasValue && dto.UserId.Value > 0)
            {
                userId = dto.UserId.Value;
                var existing = await _auth.GetUserByIdAsync(userId);
                if (existing == null)
                    return AcademyActionResult.Fail("کاربر با این شناسه یافت نشد.");
                await _auth.EnsureUserHasRoleAsync(userId, roleId);
            }
            else
            {
                var phone = (dto.PhoneNumber ?? "").Trim();
                if (string.IsNullOrWhiteSpace(phone))
                    return AcademyActionResult.Fail("شماره موبایل یا شناسه کاربر الزامی است.");

                var user = await _auth.FindByPhoneAsync(phone);
                if (user != null)
                {
                    userId = user.Id;
                    await _auth.EnsureUserHasRoleAsync(userId, roleId);
                }
                else
                {
                    var first = (dto.FirstName ?? "").Trim();
                    var last = (dto.LastName ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
                        return AcademyActionResult.Fail("برای کاربر جدید نام و نام خانوادگی الزامی است.");

                    var national = (dto.NationalCode ?? "").Trim();
                    var password = national.Length >= 8 ? national : (national + "00000000").Substring(0, 8);
                    var (hash, salt) = PasswordHasher.HashPassword(password);
                    var reg = new RegisterRequest
                    {
                        Username = "st_" + phone,
                        PhoneNumber = phone,
                        FirstName = first,
                        LastName = last,
                        NationalCode = national,
                        Password = password,
                        RoleId = roleId,
                        SiteId = dto.SiteId,
                        Email = ""
                    };
                    var result = await _auth.RegisterUserAsync(reg, hash, salt);
                    if (result != 1)
                        return AcademyActionResult.Fail(MapRegisterError(result));

                    user = await _auth.FindByPhoneAsync(phone);
                    if (user == null)
                        return AcademyActionResult.Fail("کاربر ایجاد شد ولی بازیابی نشد.");
                    userId = user.Id;
                    await _auth.EnsureUserHasRoleAsync(userId, roleId);
                }
            }

            var guardian = await ResolveGuardianAsync(dto.GuardianContactId);
            if (dto.GuardianContactId.HasValue && dto.GuardianContactId.Value > 0 && guardian == null)
                return AcademyActionResult.Fail("والد انتخاب‌شده در CRM این سایت یافت نشد.");

            var id = await _repo.InsertAsync(dto.SiteId, userId, dto.Status ?? "Active", dto.Notes, mustChangePassword: true);
            if (id == -1)
                return AcademyActionResult.Fail("این کاربر قبلاً به‌عنوان دانش‌آموز ثبت شده است.");
            if (id <= 0)
                return AcademyActionResult.Fail("خطا در ثبت دانش‌آموز.");
            if (guardian != null)
                await _repo.UpdateAsync(new UpdateAcademyStudentDto
                {
                    Id = id,
                    SiteId = dto.SiteId,
                    Status = dto.Status ?? "Active",
                    Notes = dto.Notes,
                    MustChangePassword = true,
                    GuardianContactId = guardian.Id
                });
            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> RegisterSelfAsync(string phoneNumber, string password, int? siteId)
        {
            siteId = SiteScope.CurrentSiteId(_tenant) ?? siteId;
            var phone = (phoneNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(phone))
                return AcademyActionResult.Fail("شماره موبایل الزامی است.");
            if (string.IsNullOrWhiteSpace(password))
                return AcademyActionResult.Fail("رمز عبور الزامی است.");

            var existingUser = await _auth.FindByPhoneAsync(phone);
            if (existingUser != null)
                return AcademyActionResult.Fail("این شماره قبلاً ثبت شده است.");

            var roleId = await _roleRepo.GetRoleIdByNameAsync(AcademyConstants.RoleStudent);
            if (roleId <= 0)
                return AcademyActionResult.Fail("نقش Student در دیتابیس تعریف نشده است.");

            var (hash, salt) = PasswordHasher.HashPassword(password);
            var result = await _auth.RegisterUserAsync(new RegisterRequest
            {
                Username = "st_" + phone,
                PhoneNumber = phone,
                FirstName = "-",
                LastName = "-",
                NationalCode = NationalFromPhone(phone),
                Password = password,
                RoleId = roleId,
                SiteId = siteId,
                Email = ""
            }, hash, salt);
            if (result != 1)
                return AcademyActionResult.Fail(MapRegisterError(result));

            var user = await _auth.FindByPhoneAsync(phone);
            if (user == null)
                return AcademyActionResult.Fail("کاربر ایجاد شد ولی بازیابی نشد.");
            await _auth.EnsureUserHasRoleAsync(user.Id, roleId);

            var id = await _repo.InsertAsync(siteId, user.Id, AcademyConstants.StudentStatus.Active, null, mustChangePassword: false);
            if (id == -1)
                return AcademyActionResult.Fail("این کاربر قبلاً به‌عنوان دانش‌آموز ثبت شده است.");
            if (id <= 0)
                return AcademyActionResult.Fail("خطا در ثبت دانش‌آموز.");
            return AcademyActionResult.Success(id);
        }

        public async Task<bool> UpdateAsync(UpdateAcademyStudentDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null || !Owns(existing.SiteId)) return false;
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            if (dto.GuardianContactId.HasValue && dto.GuardianContactId.Value > 0)
            {
                var guardian = await ResolveGuardianAsync(dto.GuardianContactId);
                if (guardian == null) return false;
                dto.GuardianContactId = guardian.Id;
            }
            else
                dto.GuardianContactId = null;
            return await _repo.UpdateAsync(dto);
        }

        public async Task<bool> ClearMustChangePasswordAsync(int studentId)
        {
            var item = await _repo.GetByIdAsync(studentId);
            if (item == null || !Owns(item.SiteId)) return false;
            if (!item.MustChangePassword) return true;
            return await _repo.UpdateAsync(new UpdateAcademyStudentDto
            {
                Id = item.Id,
                SiteId = item.SiteId,
                Status = item.Status,
                Notes = item.Notes,
                MustChangePassword = false,
                GuardianContactId = item.GuardianContactId
            });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item != null && Owns(item.SiteId) && await _repo.DeleteAsync(id);
        }

        private bool Owns(int? siteId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            return !current.HasValue || siteId == current;
        }

        private async Task<CrmContactDto> ResolveGuardianAsync(int? contactId)
        {
            if (!contactId.HasValue || contactId.Value <= 0 || _crm == null)
                return null;
            return await _crm.GetByIdAsync(contactId.Value);
        }

        private static string MapRegisterError(int code) => code switch
        {
            2 => "نام کاربری تکراری است.",
            3 => "شماره موبایل تکراری است.",
            4 => "کد ملی تکراری است.",
            5 => "این نقش قبلاً برای این شماره ثبت شده است.",
            _ => "خطا در ثبت‌نام کاربر."
        };

        private static string NationalFromPhone(string phone)
        {
            var digits = (phone ?? "").Trim();
            if (digits.Length >= 10) return digits[^10..];
            return (digits + "0000000000")[..10];
        }
    }

    public class AcademyInstructorService : IAcademyInstructorService
    {
        private readonly IAcademyInstructorRepository _repo;
        private readonly IAuthRepository _auth;
        private readonly MyShop.Application.Interfaces.Personnel.IPersonnelRepository _personnel;
        private readonly ICurrentTenant _tenant;

        public AcademyInstructorService(
            IAcademyInstructorRepository repo,
            IAuthRepository auth,
            MyShop.Application.Interfaces.Personnel.IPersonnelRepository personnel,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _auth = auth;
            _personnel = personnel;
            _tenant = tenant;
        }

        public async Task<List<AcademyInstructorDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
        {
            var list = await _repo.GetAllAsync(SiteScope.CurrentSiteId(_tenant) ?? siteId, isActive);
            foreach (var item in list)
                item.CategoryIds = await _repo.GetCategoryIdsAsync(item.Id);
            return list;
        }

        public async Task<AcademyInstructorDto> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return Owns(item?.SiteId) ? item : null;
        }

        public async Task<AcademyInstructorDto> GetByUserIdAsync(int userId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            if (!current.HasValue) return await _repo.GetByUserIdAsync(userId);
            var item = (await _repo.GetAllAsync(current)).FirstOrDefault(x => x.UserId == userId);
            if (item != null)
                item.CategoryIds = await _repo.GetCategoryIdsAsync(item.Id);
            return item;
        }

        public async Task<AcademyActionResult> CreateFromPersonnelAsync(CreateAcademyInstructorDto dto)
        {
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            if (dto.PersonnelId <= 0)
                return AcademyActionResult.Fail("انتخاب پرسنل الزامی است.");

            var personnel = await _personnel.GetByIdAsync(dto.PersonnelId, dto.SiteId ?? 0);
            if (personnel == null)
                return AcademyActionResult.Fail("پرسنل یافت نشد.");

            var phone = (personnel.Phone ?? "").Trim();
            if (string.IsNullOrWhiteSpace(phone))
                return AcademyActionResult.Fail("پرسنل شماره موبایل ندارد؛ ابتدا موبایل را در پرسونل تکمیل کنید.");

            var national = (personnel.NationalCode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(national) || national.Length < 8)
                return AcademyActionResult.Fail("کد ملی پرسنل برای رمز اولیه معتبر نیست (حداقل ۸ کاراکتر).");

            var roleId = await _repo.GetRoleIdByNameAsync(AcademyConstants.RoleInstructor);
            if (roleId <= 0)
                return AcademyActionResult.Fail("نقش Instructor در دیتابیس تعریف نشده است. اسکریپت Phase0 را اجرا کنید.");

            var user = await _auth.FindByPhoneAsync(phone);
            int userId;
            var mustChangePassword = false;

            if (user != null)
            {
                userId = user.Id;
                await _auth.EnsureUserHasRoleAsync(userId, roleId);
            }
            else
            {
                var nameParts = (personnel.FullName ?? "").Trim()
                    .Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var first = nameParts.Length > 0 ? nameParts[0] : "مربی";
                var last = nameParts.Length > 1 ? nameParts[1] : "-";
                var (hash, salt) = PasswordHasher.HashPassword(national);
                var reg = new RegisterRequest
                {
                    Username = "ins_" + phone,
                    PhoneNumber = phone,
                    FirstName = first,
                    LastName = last,
                    NationalCode = national,
                    Password = national,
                    RoleId = roleId,
                    SiteId = dto.SiteId,
                    Email = personnel.Email ?? ""
                };
                var result = await _auth.RegisterUserAsync(reg, hash, salt);
                if (result != 1)
                    return AcademyActionResult.Fail(MapRegisterError(result));

                user = await _auth.FindByPhoneAsync(phone);
                if (user == null)
                    return AcademyActionResult.Fail("کاربر مربی ایجاد شد ولی بازیابی نشد.");
                userId = user.Id;
                await _auth.EnsureUserHasRoleAsync(userId, roleId);
                mustChangePassword = true;
            }

            var id = await _repo.InsertAsync(dto.SiteId, dto.PersonnelId, userId, dto.Bio, dto.IsActive, mustChangePassword);
            if (id == -1)
                return AcademyActionResult.Fail("این پرسنل یا کاربر قبلاً به‌عنوان مربی ثبت شده است.");
            if (id <= 0)
                return AcademyActionResult.Fail("خطا در ثبت مربی.");

            if (dto.CategoryIds != null && dto.CategoryIds.Any())
                await _repo.SetCategoryIdsAsync(id, dto.CategoryIds);

            return AcademyActionResult.Success(id);
        }

        public async Task<AcademyActionResult> RegisterSelfAsync(string phoneNumber, string password, int? siteId)
        {
            siteId = SiteScope.CurrentSiteId(_tenant) ?? siteId;
            var phone = (phoneNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(phone))
                return AcademyActionResult.Fail("شماره موبایل الزامی است.");
            if (string.IsNullOrWhiteSpace(password))
                return AcademyActionResult.Fail("رمز عبور الزامی است.");

            var existingUser = await _auth.FindByPhoneAsync(phone);
            if (existingUser != null)
                return AcademyActionResult.Fail("این شماره قبلاً ثبت شده است.");

            var roleId = await _repo.GetRoleIdByNameAsync(AcademyConstants.RoleInstructor);
            if (roleId <= 0)
                return AcademyActionResult.Fail("نقش Instructor در دیتابیس تعریف نشده است.");

            var (hash, salt) = PasswordHasher.HashPassword(password);
            var result = await _auth.RegisterUserAsync(new RegisterRequest
            {
                Username = "ins_" + phone,
                PhoneNumber = phone,
                FirstName = "-",
                LastName = "-",
                NationalCode = NationalFromPhone(phone),
                Password = password,
                RoleId = roleId,
                SiteId = siteId,
                Email = ""
            }, hash, salt);
            if (result != 1)
                return AcademyActionResult.Fail(MapRegisterError(result));

            var user = await _auth.FindByPhoneAsync(phone);
            if (user == null)
                return AcademyActionResult.Fail("کاربر مربی ایجاد شد ولی بازیابی نشد.");
            await _auth.EnsureUserHasRoleAsync(user.Id, roleId);

            var personnelId = await _personnel.InsertAsync(new MyShop.Core.Models.Personnel.Personnel
            {
                FullName = "-",
                NationalCode = NationalFromPhone(phone),
                Phone = phone,
                Position = "مربی آموزشگاه",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            if (personnelId <= 0)
                return AcademyActionResult.Fail("خطا در ثبت پرونده پرسنل مربی.");

            var id = await _repo.InsertAsync(siteId, personnelId, user.Id, null, isActive: true, mustChangePassword: false);
            if (id == -1)
                return AcademyActionResult.Fail("این کاربر قبلاً به‌عنوان مربی ثبت شده است.");
            if (id <= 0)
                return AcademyActionResult.Fail("خطا در ثبت مربی.");
            return AcademyActionResult.Success(id);
        }

        public async Task<bool> UpdateAsync(UpdateAcademyInstructorDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null || !Owns(existing.SiteId)) return false;
            dto.SiteId = SiteScope.CurrentSiteId(_tenant) ?? dto.SiteId;
            var ok = await _repo.UpdateAsync(dto);
            if (!ok) return false;
            await _repo.SetCategoryIdsAsync(dto.Id, dto.CategoryIds ?? Enumerable.Empty<int>());
            return true;
        }

        public async Task<bool> ClearMustChangePasswordAsync(int instructorId)
        {
            var item = await _repo.GetByIdAsync(instructorId);
            if (item == null || !Owns(item.SiteId)) return false;
            if (!item.MustChangePassword) return true;
            return await UpdateAsync(new UpdateAcademyInstructorDto
            {
                Id = item.Id,
                SiteId = item.SiteId,
                Bio = item.Bio,
                IsActive = item.IsActive,
                MustChangePassword = false,
                CategoryIds = item.CategoryIds
            });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item != null && Owns(item.SiteId) && await _repo.DeleteAsync(id);
        }

        private bool Owns(int? siteId)
        {
            var current = SiteScope.CurrentSiteId(_tenant);
            return !current.HasValue || siteId == current;
        }

        private static string MapRegisterError(int code) => code switch
        {
            2 => "نام کاربری تکراری است.",
            3 => "شماره موبایل تکراری است.",
            4 => "کد ملی تکراری است.",
            5 => "این نقش قبلاً برای این شماره ثبت شده است.",
            _ => "خطا در ثبت‌نام کاربر مربی."
        };

        private static string NationalFromPhone(string phone)
        {
            var digits = (phone ?? "").Trim();
            if (digits.Length >= 10) return digits[^10..];
            return (digits + "0000000000")[..10];
        }
    }
}
