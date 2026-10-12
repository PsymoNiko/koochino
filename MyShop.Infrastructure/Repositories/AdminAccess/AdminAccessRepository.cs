using Dapper;
using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.AdminAccess;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class AdminAccessRepository : IAdminAccessRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminAccessRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<AdminAccessProfile>> ListProfilesAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<AdminAccessProfile>(
                "koch.spAdminAccessProfiles_List", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AdminAccessProfile>();
        }

        public async Task<AdminAccessProfile> GetProfileAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var profile = await conn.QueryFirstOrDefaultAsync<AdminAccessProfile>(
                "koch.spAdminAccessProfiles_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
            if (profile == null) return null;
            profile.Permissions = await ListPermissionsAsync(id);
            return profile;
        }

        public async Task<int> UpsertProfileAsync(AdminAccessProfile profile)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", profile.Id > 0 ? profile.Id : (int?)null);
            p.Add("Name", profile.Name);
            p.Add("Description", profile.Description);
            p.Add("IsFullAccess", profile.IsFullAccess);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAdminAccessProfiles_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task DeleteProfileAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spAdminAccessProfiles_Delete", new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<AdminModulePermission>> ListPermissionsAsync(int profileId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<AdminModulePermission>(
                "koch.spAdminProfilePermissions_ListByProfile",
                new { ProfileId = profileId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AdminModulePermission>();
        }

        public async Task ReplacePermissionsAsync(int profileId, IEnumerable<AdminModulePermission> permissions)
        {
            var payload = (permissions ?? Enumerable.Empty<AdminModulePermission>())
                .Select(x => new
                {
                    x.ModuleKey,
                    x.CanRead,
                    x.CanCreate,
                    x.CanUpdate,
                    x.CanDelete
                });
            var json = JsonSerializer.Serialize(payload);
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spAdminProfilePermissions_ReplaceAll",
                new { ProfileId = profileId, Json = json },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<UserAdminProfileAssignment> GetUserAssignmentAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserAdminProfileAssignment>(
                "koch.spUserAdminProfiles_GetByUser",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task AssignUserProfileAsync(int userId, int profileId)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spUserAdminProfiles_Assign",
                new { UserId = userId, ProfileId = profileId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<AdminEffectivePermissions> GetEffectiveAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "koch.spAdminPermissions_GetEffective",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);

            var meta = await multi.ReadFirstOrDefaultAsync<dynamic>();
            var perms = (await multi.ReadAsync<AdminModulePermission>())?.ToList() ?? new List<AdminModulePermission>();

            int profileId = 0;
            bool isFull = false;
            if (meta != null)
            {
                try { profileId = (int)meta.ProfileId; } catch { profileId = 0; }
                try { isFull = (bool)meta.IsFullAccess; } catch
                {
                    try { isFull = ((int)meta.IsFullAccess) == 1; } catch { isFull = false; }
                }
            }

            return new AdminEffectivePermissions
            {
                UserId = userId,
                ProfileId = profileId,
                IsFullAccess = isFull,
                Permissions = perms
            };
        }

        public async Task<List<AdminAccessOverview>> ListAdminOverviewAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<AdminAccessOverview>(
                "koch.spAdminAccess_ListAdminOverview",
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AdminAccessOverview>();
        }
    }
}
