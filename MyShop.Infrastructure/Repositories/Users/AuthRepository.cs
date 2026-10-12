using Dapper;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbConnectionFactory _factory;

        public AuthRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }
        //login — یک شماره = یک کاربر؛ نقش‌ها در UserRoles هستند
        public async Task<User> FindByPhoneAsync(string PhoneNumber)
        {
            using var conn = _factory.CreateConnection();
            var normalized = NormalizeIranMobile(PhoneNumber);

            var user = await conn.QueryFirstOrDefaultAsync<User>(@"
SELECT TOP 1 u.Id, u.Username, u.Email, u.PhoneNumber, u.FirstName, u.LastName,
       u.IsActive, ISNULL(u.IsPhoneVerified, 0) AS IsPhoneVerified, u.GoogleSubject,
       u.CreatedAt, u.NationalCode,
       ISNULL((SELECT TOP 1 ur.RoleId FROM koch.UserRoles ur WHERE ur.UserId = u.Id ORDER BY ur.RoleId), 0) AS RoleId,
       u.SiteId
FROM koch.Users u
WHERE REPLACE(LTRIM(RTRIM(ISNULL(u.PhoneNumber, N''))), N' ', N'') IN (@Phone, @PhoneAlt, @PhoneRaw)
   OR RIGHT(REPLACE(LTRIM(RTRIM(ISNULL(u.PhoneNumber, N''))), N' ', N''), 10) = @Last10
ORDER BY u.Id",
                new
                {
                    Phone = normalized,
                    PhoneAlt = normalized.Length == 11 && normalized.StartsWith("0")
                        ? normalized.Substring(1)
                        : normalized,
                    PhoneRaw = (PhoneNumber ?? "").Trim(),
                    Last10 = normalized.Length >= 10 ? normalized.Substring(normalized.Length - 10) : normalized
                });

            if (user != null)
                return user;

            try
            {
                return await conn.QueryFirstOrDefaultAsync<User>(
                    "koch.spUsers_FindByPhone",
                    new { PhoneNumber = string.IsNullOrWhiteSpace(normalized) ? PhoneNumber : normalized },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                return null;
            }
        }
        // -------------------------
        // 1) دریافت کاربر با شماره موبایل + نقش (چند نقش روی یک شماره مجاز است)
        // -------------------------
        public async Task<User> GetUserByPhoneAsync(string phone , int RoleID)
        {
            using var conn = _factory.CreateConnection();
            var normalized = NormalizeIranMobile(phone);

            // نقش از koch.UserRoles خوانده می‌شود تا یک شماره بتواند چند نقش (مشتری + مغازه‌دار) داشته باشد.
            // SP قدیمی ممکن است فقط Users.RoleId (تک‌نقش) را چک کند.
            var user = await conn.QueryFirstOrDefaultAsync<User>(@"
SELECT TOP 1 u.Id, u.Username, u.Email, u.PhoneNumber, u.FirstName, u.LastName,
       u.IsActive, ISNULL(u.IsPhoneVerified, 0) AS IsPhoneVerified, u.GoogleSubject,
       u.CreatedAt, u.NationalCode, ur.RoleId, u.SiteId
FROM koch.Users u
INNER JOIN koch.UserRoles ur ON ur.UserId = u.Id AND ur.RoleId = @RoleID
WHERE REPLACE(LTRIM(RTRIM(ISNULL(u.PhoneNumber, N''))), N' ', N'') IN (@Phone, @PhoneAlt, @PhoneRaw)
   OR RIGHT(REPLACE(LTRIM(RTRIM(ISNULL(u.PhoneNumber, N''))), N' ', N''), 10) = @Last10
ORDER BY u.Id",
                new
                {
                    Phone = normalized,
                    PhoneAlt = normalized.Length == 11 && normalized.StartsWith("0")
                        ? normalized.Substring(1)
                        : normalized,
                    PhoneRaw = (phone ?? "").Trim(),
                    Last10 = normalized.Length >= 10 ? normalized.Substring(normalized.Length - 10) : normalized,
                    RoleID = RoleID
                });

            if (user != null)
                return user;

            // حساب قدیمی: نقش فقط در Users.RoleId است — همگام‌سازی به UserRoles و برگرداندن
            try
            {
                var legacyUser = await conn.QueryFirstOrDefaultAsync<User>(@"
SELECT TOP 1 u.Id, u.Username, u.Email, u.PhoneNumber, u.FirstName, u.LastName,
       u.IsActive, ISNULL(u.IsPhoneVerified, 0) AS IsPhoneVerified, u.GoogleSubject,
       u.CreatedAt, u.NationalCode, TRY_CAST(u.RoleId AS INT) AS RoleId, u.SiteId
FROM koch.Users u
WHERE TRY_CAST(u.RoleId AS INT) = @RoleID
  AND (
        REPLACE(LTRIM(RTRIM(ISNULL(u.PhoneNumber, N''))), N' ', N'') IN (@Phone, @PhoneAlt, @PhoneRaw)
     OR RIGHT(REPLACE(LTRIM(RTRIM(ISNULL(u.PhoneNumber, N''))), N' ', N''), 10) = @Last10
  )
ORDER BY u.Id",
                    new
                    {
                        Phone = normalized,
                        PhoneAlt = normalized.Length == 11 && normalized.StartsWith("0")
                            ? normalized.Substring(1)
                            : normalized,
                        PhoneRaw = (phone ?? "").Trim(),
                        Last10 = normalized.Length >= 10 ? normalized.Substring(normalized.Length - 10) : normalized,
                        RoleID = RoleID
                    });
                if (legacyUser != null && legacyUser.Id > 0)
                {
                    await EnsureUserHasRoleAsync(legacyUser.Id, RoleID);
                    return legacyUser;
                }
            }
            catch { /* ادامه به SP */ }

            // fallback به SP برای سازگاری با داده‌های قدیمی
            try
            {
                return await conn.QueryFirstOrDefaultAsync<User>(
                    "koch.spUsers_FindByPhoneRole",
                    new { PhoneNumber = string.IsNullOrWhiteSpace(normalized) ? phone : normalized, RoleID = RoleID },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeIranMobile(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var sb = new System.Text.StringBuilder(input.Length);
            foreach (var c in input.Trim())
            {
                if (c >= '0' && c <= '9') sb.Append(c);
                else if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
                else if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
            }
            var value = sb.ToString();
            if (value.StartsWith("98") && value.Length >= 12) value = "0" + value.Substring(2);
            if (value.StartsWith("9") && value.Length == 10) value = "0" + value;
            return value;
        }

        public async Task<User> GetUserByGoogleSubjectAsync(string googleSubject)
        {
            if (string.IsNullOrWhiteSpace(googleSubject)) return null;
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(@"
SELECT TOP 1 u.Id, u.Username, u.Email, u.PhoneNumber, u.FirstName, u.LastName,
       u.IsActive, ISNULL(u.IsPhoneVerified, 0) AS IsPhoneVerified, u.GoogleSubject,
       u.CreatedAt, u.NationalCode, ur.RoleId, u.SiteId
FROM koch.Users u
INNER JOIN koch.UserRoles ur ON ur.UserId = u.Id
WHERE u.GoogleSubject = @GoogleSubject
ORDER BY CASE WHEN ur.RoleId = 3 THEN 0 ELSE 1 END, u.Id",
                new { GoogleSubject = googleSubject.Trim() });
        }

        public async Task<User> GetCustomerByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) return null;
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(@"
SELECT TOP 1 u.Id, u.Username, u.Email, u.PhoneNumber, u.FirstName, u.LastName,
       u.IsActive, ISNULL(u.IsPhoneVerified, 0) AS IsPhoneVerified, u.GoogleSubject,
       u.CreatedAt, u.NationalCode, ur.RoleId, u.SiteId
FROM koch.Users u
INNER JOIN koch.UserRoles ur ON ur.UserId = u.Id
WHERE LTRIM(RTRIM(LOWER(ISNULL(u.Email, N'')))) = LTRIM(RTRIM(LOWER(@Email)))
  AND ur.RoleId = 3
  AND LTRIM(RTRIM(ISNULL(u.Email, N''))) NOT IN (N'', N'-')
ORDER BY CASE WHEN u.GoogleSubject IS NOT NULL THEN 0 ELSE 1 END, u.Id",
                new { Email = email.Trim() });
        }

        public async Task<User> GetStoreOwnerByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) return null;
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(@"
SELECT TOP 1 u.Id, u.Username, u.Email, u.PhoneNumber, u.FirstName, u.LastName,
       u.IsActive, ISNULL(u.IsPhoneVerified, 0) AS IsPhoneVerified, u.GoogleSubject,
       u.CreatedAt, u.NationalCode, ur.RoleId, u.SiteId
FROM koch.Users u
INNER JOIN koch.UserRoles ur ON ur.UserId = u.Id
WHERE LTRIM(RTRIM(LOWER(ISNULL(u.Email, N'')))) = LTRIM(RTRIM(LOWER(@Email)))
  AND ur.RoleId = 2
  AND LTRIM(RTRIM(ISNULL(u.Email, N''))) NOT IN (N'', N'-')
ORDER BY CASE WHEN u.GoogleSubject IS NOT NULL THEN 0 ELSE 1 END, u.Id",
                new { Email = email.Trim() });
        }

        public async Task<int> RegisterGoogleCustomerAsync(
            string googleSubject, string email, string firstName, string lastName,
            string passwordHash, string passwordSalt, int? siteId)
        {
            return await RegisterGoogleUserAsync(googleSubject, email, firstName, lastName, passwordHash, passwordSalt, siteId, roleId: 3);
        }

        public async Task<int> RegisterGoogleStoreOwnerAsync(
            string googleSubject, string email, string firstName, string lastName,
            string passwordHash, string passwordSalt, int? siteId)
        {
            return await RegisterGoogleUserAsync(googleSubject, email, firstName, lastName, passwordHash, passwordSalt, siteId, roleId: 2);
        }

        private async Task<int> RegisterGoogleUserAsync(
            string googleSubject, string email, string firstName, string lastName,
            string passwordHash, string passwordSalt, int? siteId, int roleId)
        {
            using var conn = _factory.CreateConnection();
            var username = "g_" + (googleSubject ?? Guid.NewGuid().ToString("N"));
            if (username.Length > 50) username = username.Substring(0, 50);

            var id = await conn.ExecuteScalarAsync<int>(@"
DECLARE @Phone NVARCHAR(50) = N'g' + LEFT(REPLACE(@GoogleSubject, N'-', N''), 20);
IF EXISTS (SELECT 1 FROM koch.Users WHERE PhoneNumber = @Phone)
    SET @Phone = N'g' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 20);

INSERT INTO koch.Users
    (Username, Email, PhoneNumber, FirstName, LastName, PasswordHash, PasswordSalt,
     NationalCode, IsActive, IsPhoneVerified, GoogleSubject, SiteId, CreatedAt, UserStatID)
VALUES
    (@Username, @Email, @Phone, @FirstName, @LastName, @PasswordHash, @PasswordSalt,
     N'0000000000', 1, 0, @GoogleSubject, @SiteId, SYSDATETIME(), 1);
DECLARE @NewId INT = CAST(SCOPE_IDENTITY() AS INT);
IF NOT EXISTS (SELECT 1 FROM koch.UserRoles WHERE UserId = @NewId AND RoleId = @RoleId)
    INSERT INTO koch.UserRoles (UserId, RoleId) VALUES (@NewId, @RoleId);
SELECT @NewId;",
                new
                {
                    Username = username,
                    Email = email?.Trim(),
                    FirstName = firstName ?? "",
                    LastName = lastName ?? "",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    GoogleSubject = googleSubject?.Trim(),
                    SiteId = siteId,
                    RoleId = roleId
                });
            return id;
        }

        public async Task LinkGoogleSubjectAsync(int userId, string googleSubject)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(googleSubject)) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE koch.Users SET GoogleSubject = @GoogleSubject WHERE Id = @UserId AND (GoogleSubject IS NULL OR GoogleSubject = N'')",
                new { UserId = userId, GoogleSubject = googleSubject.Trim() });
        }

        public async Task EnsureUserHasRoleAsync(int userId, int roleId)
        {
            if (userId <= 0 || roleId <= 0) return;
            using var conn = _factory.CreateConnection();
            // نقش جدید + همگام‌سازی نقش قدیمی ستون Users.RoleId (برای حساب‌های تک‌نقشه قدیمی)
            await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM koch.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    INSERT INTO koch.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);

DECLARE @LegacyRole INT = (
    SELECT TOP 1 TRY_CAST(RoleId AS INT) FROM koch.Users WHERE Id = @UserId
);
IF @LegacyRole IS NOT NULL AND @LegacyRole > 0
   AND NOT EXISTS (SELECT 1 FROM koch.UserRoles WHERE UserId = @UserId AND RoleId = @LegacyRole)
    INSERT INTO koch.UserRoles (UserId, RoleId) VALUES (@UserId, @LegacyRole);",
                new { UserId = userId, RoleId = roleId });
        }

        public async Task RemoveUserRoleAsync(int userId, int roleId)
        {
            if (userId <= 0 || roleId <= 0) return;
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId",
                new { UserId = userId, RoleId = roleId });
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            using var conn = _factory.CreateConnection();

            // اول از جدول چندنقشی بخوان؛ SP ممکن است فقط نقش ستون Users.RoleId را برگرداند
            var roles = (await conn.QueryAsync<string>(@"
SELECT DISTINCT ISNULL(r.Name, CASE ur.RoleId
        WHEN 1 THEN N'Admin'
        WHEN 2 THEN N'StoreOwner'
        WHEN 3 THEN N'Customer'
        ELSE CONCAT(N'Role', ur.RoleId)
    END) AS RoleName
FROM koch.UserRoles ur
LEFT JOIN koch.Roles r ON r.Id = ur.RoleId
WHERE ur.UserId = @UserId",
                new { UserId = userId })).ToList();

            if (roles.Count == 0)
            {
                // حساب‌های قدیمی فقط Users.RoleId دارند
                var legacy = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 TRY_CAST(RoleId AS INT) FROM koch.Users WHERE Id = @UserId",
                    new { UserId = userId });
                if (legacy is int rid && rid > 0)
                {
                    await EnsureUserHasRoleAsync(userId, rid);
                    roles = (await conn.QueryAsync<string>(@"
SELECT DISTINCT ISNULL(r.Name, CASE ur.RoleId
        WHEN 1 THEN N'Admin'
        WHEN 2 THEN N'StoreOwner'
        WHEN 3 THEN N'Customer'
        ELSE CONCAT(N'Role', ur.RoleId)
    END) AS RoleName
FROM koch.UserRoles ur
LEFT JOIN koch.Roles r ON r.Id = ur.RoleId
WHERE ur.UserId = @UserId",
                        new { UserId = userId })).ToList();
                }
            }

            if (roles.Count > 0)
                return roles;

            try
            {
                return await conn.QueryAsync<string>(
                    "koch.spUserRoles_GetRolesForUser",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// update progile
        /// </summary>
        /// <param name="updateprofile"></param>
        /// <returns></returns>
        /// 
        public async Task<int> UpdateProfileAsync(UpdateUserProfileDto dto)
        {
            using var conn = _factory.CreateConnection();

            var result = await conn.ExecuteAsync(
                "koch.spUsers_UpdateProfile",
                new
                {
                    dto.UserId,
                    dto.Username,
                    dto.Email,
                    dto.FirstName,
                    dto.LastName,
                    dto.NationalCode
                },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }


        public async Task<(string Hash, string Salt)> GetPasswordInfoAsync(int userId)
        {
            using var conn = _factory.CreateConnection();

            var result = await conn.QueryFirstOrDefaultAsync<(string Hash, string Salt)>(
                "koch.spUsers_GetPasswordInfo",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            );
            return result;
        }

        public async Task<bool> SetUserPasswordAsync(int userId, string passwordHash, string passwordSalt)
        {
            using var conn = _factory.CreateConnection();
            const string sql = @"
                UPDATE koch.Users
                SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt
                WHERE Id = @UserId";
            var affected = await conn.ExecuteAsync(sql, new { UserId = userId, PasswordHash = passwordHash, PasswordSalt = passwordSalt });
            return affected > 0;
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("koch.spUsers_UpdateLastLogin",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();

            return await conn.QueryFirstOrDefaultAsync<User>(
                "koch.spUsers_LoginById",
                new { UserId = id },
                commandType: CommandType.StoredProcedure
            );
        }

        /// <summary>
        /// ////////////////////////////////////
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<User> GetUserByUserNameAsync(string userName)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(
                "koch.spUsers_GetByUserName",
                new { UserName = userName },
                commandType: CommandType.StoredProcedure);
        }

        // -------------------------
        // 3) بروزرسانی آخرین ورود
        // -------------------------
        //public async Task UpdateLastLoginAsync(int userId)
        //{
        //    using var conn = _factory.CreateConnection();

        //    await conn.ExecuteAsync(
        //        "koch.spUsers_UpdateLastLogin",
        //        new { UserId = userId },
        //        commandType: CommandType.StoredProcedure);
        //}
        //ثبت نام کامل
        public async Task<int> RegisterUserAsync(RegisterRequest r, string hash, string salt)
        {
            using var conn = _factory.CreateConnection();

            var parameters = new DynamicParameters();

            parameters.Add("Username", r.Username);
            parameters.Add("PhoneNumber", r.PhoneNumber);
            parameters.Add("Email", r.Email);
            parameters.Add("FirstName", r.FirstName);
            parameters.Add("LastName", r.LastName);
            parameters.Add("PasswordHash", hash);
            parameters.Add("PasswordSalt", salt);
            parameters.Add("NationalCode", r.NationalCode);
            parameters.Add("RoleId", r.RoleId);
            parameters.Add("InviteCode", string.IsNullOrWhiteSpace(r.InviteCode) ? null : r.InviteCode.Trim());
            parameters.Add("SiteId", r.SiteId);

            // مهمترین بخش:
            parameters.Add("ret", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await conn.ExecuteAsync(
                "koch.spUsers_Register",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int result = parameters.Get<int>("ret");
            return result;
        }


        public async Task<User> LoginByIdAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(
                "koch.spUsers_LoginById",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Store>> GetStoresForUserAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Store>(
                "koch.spStores_FindByUser",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

    
        // -------------------------
        // PROFILE
        // -------------------------
        public async Task<UserProfileVm> GetProfileAsync(int userId)
        {
            using var conn = _factory.CreateConnection();

            return await conn.QueryFirstOrDefaultAsync<UserProfileVm>(
                "koch.spUsers_LoginById",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<(bool ok, string error)> UpdateProfileAsync(UpdateProfileRequest r)
        {
            using var conn = _factory.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("UserId", r.UserId);
            parameters.Add("FirstName", r.FirstName);
            parameters.Add("LastName", r.LastName);
            parameters.Add("Email", r.Email);
            parameters.Add("NationalCode", r.NationalCode);
            parameters.Add("PhoneNumber", r.PhoneNumber);
            parameters.Add("ret", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await conn.ExecuteAsync(
                "koch.spUsers_UpdateProfile",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int result = parameters.Get<int>("ret");

            return result switch
            {
                1 => (true, "بروزرسانی موفق انجام شد"),
                2 => (false, "شماره موبایل تکراری است"),
                3 => (false, "کد ملی تکراری است"),
                -1 => (false, "خطای سیستمی"),
                _ => (false, "خطای نامشخص")
            };
        }



    }
}
