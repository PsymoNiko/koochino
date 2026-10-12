using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ICurrentTenant _tenant;

        public UserRepository(IDbConnectionFactory connectionFactory, ICurrentTenant tenant = null)
        {
            _connectionFactory = connectionFactory;
            _tenant = tenant;
        }

        // -------------------------
        // Dashboard / Admin
        // -------------------------
        public async Task<int> CountAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var siteId = SiteScope.CurrentSiteId(_tenant);
            if (siteId.HasValue)
            {
                try
                {
                    return await conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM koch.Users WHERE SiteId IS NULL OR SiteId = @SiteId",
                        new { SiteId = siteId.Value });
                }
                catch { /* fallback */ }
            }

            return await conn.QueryFirstOrDefaultAsync<int>(
                "spUsers_Count",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<User>> LatestAsync(int count)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = (await conn.QueryAsync<User>(
                "koch.spUsers_Latest",
                new { Count = count },
                commandType: CommandType.StoredProcedure
            )).ToList();

            if (!SiteScope.ShouldFilter(_tenant) || list.Count == 0)
                return list;

            try
            {
                var ids = list.Select(u => u.Id).ToList();
                var rows = (await conn.QueryAsync<(int Id, int? SiteId)>(
                    "SELECT Id, SiteId FROM koch.Users WHERE Id IN @Ids",
                    new { Ids = ids })).ToList();
                var map = rows.ToDictionary(x => x.Id, x => x.SiteId);
                var sid = _tenant.SiteId;
                return list.Where(u =>
                {
                    if (!map.TryGetValue(u.Id, out var us)) return true;
                    u.SiteId = us;
                    return us == null || us == sid;
                }).ToList();
            }
            catch
            {
                return list;
            }
        }

        public async Task<List<UserDto>> GetUsersAsync(string search)
        {
            using var conn = _connectionFactory.CreateConnection();

            List<UserDto> users;
            try
            {
                // ترجیح: لیست با SiteId از SQL مستقیم (سازگار حتی اگر SP قدیمی باشد)
                users = (await conn.QueryAsync<UserDto>(@"
                    SELECT u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.NationalCode,
                           u.CreatedAt, u.IsActive, u.SiteId,
                           s.Code AS SiteCode, s.Name AS SiteName
                    FROM koch.Users u
                    LEFT JOIN koch.Sites s ON s.Id = u.SiteId
                    ORDER BY u.Id DESC")).ToList();
            }
            catch
            {
                users = (await conn.QueryAsync<UserDto>(
                    "koch.spUsers_List",
                    commandType: CommandType.StoredProcedure
                )).ToList();
            }

            // فیلتر سایت جاری (اگر resolve شده)
            if (_tenant != null && _tenant.HasSite)
            {
                var sid = _tenant.SiteId;
                users = users.Where(u => u.SiteId == null || u.SiteId == sid).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                users = users.Where(u =>
                    (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
                ).ToList();
            }

            // نقش‌ها از UserRoles (چندنقشه: مشتری + مغازه‌دار)
            if (users.Count > 0)
            {
                try
                {
                    var ids = users.Select(u => u.Id).Distinct().ToArray();
                    var roleRows = (await conn.QueryAsync<(int UserId, string RoleName)>(@"
SELECT ur.UserId,
       ISNULL(r.Name, CASE ur.RoleId
            WHEN 1 THEN N'Admin'
            WHEN 2 THEN N'StoreOwner'
            WHEN 3 THEN N'Customer'
            ELSE CONCAT(N'Role', ur.RoleId)
       END) AS RoleName
FROM koch.UserRoles ur
LEFT JOIN koch.Roles r ON r.Id = ur.RoleId
WHERE ur.UserId IN @Ids
UNION
SELECT u.Id AS UserId,
       CASE TRY_CAST(u.RoleId AS INT)
            WHEN 1 THEN N'Admin'
            WHEN 2 THEN N'StoreOwner'
            WHEN 3 THEN N'Customer'
            ELSE NULL
       END AS RoleName
FROM koch.Users u
WHERE u.Id IN @Ids
  AND TRY_CAST(u.RoleId AS INT) IN (1, 2, 3)
  AND NOT EXISTS (SELECT 1 FROM koch.UserRoles ur2 WHERE ur2.UserId = u.Id)",
                        new { Ids = ids })).ToList();

                    var byUser = roleRows
                        .Where(x => !string.IsNullOrWhiteSpace(x.RoleName))
                        .GroupBy(x => x.UserId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

                    foreach (var u in users)
                    {
                        if (byUser.TryGetValue(u.Id, out var roles) && roles.Count > 0)
                        {
                            u.Roles = roles;
                            u.RoleName = string.Join(", ", roles);
                        }
                    }
                }
                catch { /* اگر جدول نقش نبود، لیست بدون نقش برمی‌گردد */ }
            }

            return users;
        }

        public async Task<List<UserDto>> SearchUsersAsync(string query, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                return new List<UserDto>();

            limit = limit <= 0 ? 20 : Math.Min(limit, 50);
            var term = query.Trim();
            using var conn = _connectionFactory.CreateConnection();
            var users = (await conn.QueryAsync<UserDto>(@"
                SELECT TOP (@Limit) u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.NationalCode,
                       u.CreatedAt, u.IsActive, u.SiteId,
                       s.Code AS SiteCode, s.Name AS SiteName
                FROM koch.Users u
                LEFT JOIN koch.Sites s ON s.Id = u.SiteId
                WHERE u.LastName LIKE @Like
                   OR u.PhoneNumber LIKE @Like
                   OR u.FirstName LIKE @Like
                ORDER BY u.Id DESC",
                new { Limit = limit, Like = "%" + term + "%" })).ToList();

            if (_tenant != null && _tenant.HasSite)
            {
                var sid = _tenant.SiteId;
                users = users.Where(u => u.SiteId == null || u.SiteId == sid).ToList();
            }

            return users;
        }

        public async Task<UserDto> GetUserBriefAsync(int userId)
        {
            if (userId <= 0) return null;
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserDto>(@"
                SELECT u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.NationalCode,
                       u.CreatedAt, u.IsActive, u.SiteId
                FROM koch.Users u
                WHERE u.Id = @UserId",
                new { UserId = userId });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();

            var affected = await conn.ExecuteAsync(
                "koch.spUsers_Delete",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            return affected > 0;
        }

        // -------------------------
        // ✅ PROFILE (مشکل اصلی)
        // -------------------------

        public async Task<UserProfileVm> GetProfileAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();

            try
            {
                const string sql = @"
                    SELECT Username, Email, PhoneNumber, FirstName, LastName, NationalCode, BirthDate,
                           ProfilePictureUrl, NationalIdFrontImageUrl, NationalIdBackImageUrl,
                           ISNULL(IsPhoneVerified, 0) AS IsPhoneVerified
                    FROM koch.Users WHERE Id = @UserId";
                return await conn.QueryFirstOrDefaultAsync<UserProfileVm>(sql, new { UserId = userId });
            }
            catch
            {
                // اگر ستون IsPhoneVerified هنوز اضافه نشده باشد
                const string fallback = @"
                    SELECT Username, Email, PhoneNumber, FirstName, LastName, NationalCode, BirthDate,
                           ProfilePictureUrl, NationalIdFrontImageUrl, NationalIdBackImageUrl
                    FROM koch.Users WHERE Id = @UserId";
                return await conn.QueryFirstOrDefaultAsync<UserProfileVm>(fallback, new { UserId = userId });
            }
        }

        public async Task<int> UpdateProfileAsync(UpdateUserProfileDto dto)
        {
            using var conn = _connectionFactory.CreateConnection();

            var username = (dto.Username ?? "").Trim();
            var email = (dto.Email ?? "").Trim();
            var firstName = (dto.FirstName ?? "").Trim();
            var lastName = (dto.LastName ?? "").Trim();
            var nationalCode = (dto.NationalCode ?? "").Trim();

            // 1) چک وجود کاربر
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT 1 FROM koch.Users WHERE Id = @UserId",
                new { UserId = dto.UserId });
            if (exists == 0)
                return 0;

            // 2) چک نام کاربری تکراری (فقط وقتی مقدار معتبر داده شده) — مقایسه با LTRIM/RTRIM
            if (!string.IsNullOrEmpty(username))
            {
                var dupUser = await conn.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM koch.Users WHERE LTRIM(RTRIM(ISNULL(Username, N''))) = LTRIM(RTRIM(@Username)) AND Id <> @UserId",
                    new { Username = username, UserId = dto.UserId });
                if (dupUser.HasValue && dupUser.Value == 1)
                    return 2;
            }

            // 3) چک کد ملی تکراری — مقایسه با LTRIM/RTRIM تا فاصله/نویسه اضافه باعث نشود تکراری تشخیص داده نشود
            if (!string.IsNullOrEmpty(nationalCode))
            {
                var dupNc = await conn.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM koch.Users WHERE LTRIM(RTRIM(ISNULL(NationalCode, N''))) = LTRIM(RTRIM(@NationalCode)) AND Id <> @UserId",
                    new { NationalCode = nationalCode, UserId = dto.UserId });
                if (dupNc.HasValue && dupNc.Value == 1)
                    return 3;
            }

            // 4) چک ایمیل تکراری — مقایسه با LTRIM/RTRIM
            if (!string.IsNullOrEmpty(email))
            {
                var dupEmail = await conn.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM koch.Users WHERE LTRIM(RTRIM(ISNULL(Email, N''))) = LTRIM(RTRIM(@Email)) AND Id <> @UserId",
                    new { Email = email, UserId = dto.UserId });
                if (dupEmail.HasValue && dupEmail.Value == 1)
                    return 4;
            }

            // 5) به‌روزرسانی مستقیم با SQL (بدون وابستگی به SP) — فقط فیلدهای پروفایل
            const string updateSql = @"
                UPDATE koch.Users
                SET
                    Username    = CASE WHEN @Username IS NOT NULL AND LTRIM(RTRIM(@Username)) <> N'' THEN LTRIM(RTRIM(@Username)) ELSE Username END,
                    Email       = NULLIF(LTRIM(RTRIM(ISNULL(@Email, N''))), N''),
                    FirstName   = NULLIF(LTRIM(RTRIM(ISNULL(@FirstName, N''))), N''),
                    LastName    = NULLIF(LTRIM(RTRIM(ISNULL(@LastName, N''))), N''),
                    NationalCode = NULLIF(LTRIM(RTRIM(ISNULL(@NationalCode, N''))), N'')
                WHERE Id = @UserId";

            var affected = await conn.ExecuteAsync(updateSql, new
            {
                UserId = dto.UserId,
                Username = username,
                Email = string.IsNullOrEmpty(email) ? (string)null : email,
                FirstName = string.IsNullOrEmpty(firstName) ? (string)null : firstName,
                LastName = string.IsNullOrEmpty(lastName) ? (string)null : lastName,
                NationalCode = string.IsNullOrEmpty(nationalCode) ? (string)null : nationalCode
            });

            return affected > 0 ? 1 : 0;
        }

        /// <summary>به‌روزرسانی آدرس یک سند/عکس کاربر (پروفایل، کارت ملی رو، کارت ملی پشت)</summary>
        public async Task<bool> UpdateUserDocumentUrlAsync(int userId, string documentType, string url)
        {
            var column = documentType switch
            {
                "ProfilePicture" => "ProfilePictureUrl",
                "NationalIdFront" => "NationalIdFrontImageUrl",
                "NationalIdBack" => "NationalIdBackImageUrl",
                _ => null
            };
            if (string.IsNullOrEmpty(column)) return false;

            using var conn = _connectionFactory.CreateConnection();
            var sql = $"UPDATE koch.Users SET [{column}] = @Url WHERE Id = @UserId";
            var affected = await conn.ExecuteAsync(sql, new { UserId = userId, Url = url ?? "" });
            return affected > 0;
        }

        public async Task<bool> ToggleStatusAsync(int userId, bool isActive)
        {
            using var conn = _connectionFactory.CreateConnection();
            var affected = await conn.ExecuteAsync(
                "UPDATE koch.Users SET IsActive = @IsActive WHERE Id = @UserId",
                new { UserId = userId, IsActive = isActive });
            return affected > 0;
        }

        public async Task<bool> SetPhoneVerifiedAsync(int userId, bool isVerified = true)
        {
            try
            {
                using var conn = _connectionFactory.CreateConnection();
                var affected = await conn.ExecuteAsync(
                    "UPDATE koch.Users SET IsPhoneVerified = @IsVerified WHERE Id = @UserId",
                    new { UserId = userId, IsVerified = isVerified });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int?> FindUserIdByPhoneAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                return await conn.QueryFirstOrDefaultAsync<int?>(@"
                    SELECT TOP 1 Id FROM koch.Users
                    WHERE REPLACE(LTRIM(RTRIM(ISNULL(PhoneNumber, N''))), N' ', N'') = @Phone
                    ORDER BY CASE WHEN ISNULL(IsPhoneVerified, 0) = 1 THEN 0 ELSE 1 END, Id",
                    new { Phone = phoneNumber.Trim() });
            }
            catch
            {
                return await conn.QueryFirstOrDefaultAsync<int?>(@"
                    SELECT TOP 1 Id FROM koch.Users
                    WHERE REPLACE(LTRIM(RTRIM(ISNULL(PhoneNumber, N''))), N' ', N'') = @Phone
                    ORDER BY Id",
                    new { Phone = phoneNumber.Trim() });
            }
        }

        public async Task<bool> IsPhoneVerifiedAsync(int userId)
        {
            try
            {
                using var conn = _connectionFactory.CreateConnection();
                return await conn.ExecuteScalarAsync<bool>(
                    "SELECT CAST(ISNULL(IsPhoneVerified, 0) AS BIT) FROM koch.Users WHERE Id = @UserId",
                    new { UserId = userId });
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsPhoneVerifiedByAnotherUserAsync(string phoneNumber, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            try
            {
                using var conn = _connectionFactory.CreateConnection();
                var count = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(1) FROM koch.Users
                    WHERE ISNULL(IsPhoneVerified, 0) = 1
                      AND REPLACE(LTRIM(RTRIM(ISNULL(PhoneNumber, N''))), N' ', N'') = @Phone
                      AND (@ExcludeUserId IS NULL OR Id <> @ExcludeUserId)",
                    new { Phone = phoneNumber.Trim(), ExcludeUserId = excludeUserId });
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetIdentityVerificationStatusAsync(int userId)
        {
            try
            {
                using var conn = _connectionFactory.CreateConnection();
                return await conn.ExecuteScalarAsync<string>(
                    "SELECT IdentityVerificationStatus FROM koch.Users WHERE Id = @UserId",
                    new { UserId = userId });
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SetIdentityVerificationStatusAsync(int userId, string status)
        {
            try
            {
                using var conn = _connectionFactory.CreateConnection();
                var affected = await conn.ExecuteAsync(
                    "UPDATE koch.Users SET IdentityVerificationStatus = @Status WHERE Id = @UserId",
                    new { UserId = userId, Status = status });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
