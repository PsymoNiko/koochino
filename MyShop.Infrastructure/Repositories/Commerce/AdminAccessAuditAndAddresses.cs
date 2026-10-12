using Dapper;
using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.AdminAccess;
using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.AdminAccess
{
    public class AdminAccessAuditRepository : IAdminAccessAuditRepository
    {
        private readonly IDbConnectionFactory _factory;
        public AdminAccessAuditRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task InsertAsync(AdminAccessAuditEntry entry)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                INSERT INTO koch.AdminAccessAudit (ActorUserId, TargetUserId, Action, DetailJson, IpAddress)
                VALUES (@ActorUserId, @TargetUserId, @Action, @DetailJson, @IpAddress)", entry);
        }

        public async Task<List<AdminAccessAuditEntry>> ListAsync(int top = 100)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<AdminAccessAuditEntry>(@"
                SELECT TOP (@Top) a.*,
                       LTRIM(RTRIM(ISNULL(ac.FirstName,'')+' '+ISNULL(ac.LastName,''))) ActorName,
                       LTRIM(RTRIM(ISNULL(tg.FirstName,'')+' '+ISNULL(tg.LastName,''))) TargetName
                FROM koch.AdminAccessAudit a
                LEFT JOIN koch.Users ac ON ac.Id = a.ActorUserId
                LEFT JOIN koch.Users tg ON tg.Id = a.TargetUserId
                ORDER BY a.Id DESC", new { Top = top });
            return rows.ToList();
        }
    }
}

namespace MyShop.Infrastructure.Repositories.Commerce
{
    public class UserAddressRepository : IUserAddressRepository
    {
        private readonly IDbConnectionFactory _factory;
        public UserAddressRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<UserAddress>> ListByUserAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<UserAddress>(@"
                SELECT * FROM koch.UserAddresses WHERE UserId=@UserId
                ORDER BY IsDefault DESC, Id DESC", new { UserId = userId });
            return rows.ToList();
        }

        public async Task<UserAddress> GetAsync(int id, int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserAddress>(
                "SELECT * FROM koch.UserAddresses WHERE Id=@Id AND UserId=@UserId",
                new { Id = id, UserId = userId });
        }

        public async Task<int> UpsertAsync(UserAddress address)
        {
            using var conn = _factory.CreateConnection();
            if (address.IsDefault)
                await conn.ExecuteAsync("UPDATE koch.UserAddresses SET IsDefault=0 WHERE UserId=@UserId", new { address.UserId });

            if (address.Id > 0)
            {
                await conn.ExecuteAsync(@"
                    UPDATE koch.UserAddresses
                    SET Title=@Title, FullAddress=@FullAddress, PostalCode=@PostalCode,
                        ReceiverName=@ReceiverName, ReceiverPhone=@ReceiverPhone,
                        IsDefault=@IsDefault, UpdatedAt=SYSDATETIME()
                    WHERE Id=@Id AND UserId=@UserId", address);
                return address.Id;
            }
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO koch.UserAddresses (UserId, Title, FullAddress, PostalCode, ReceiverName, ReceiverPhone, IsDefault)
                OUTPUT INSERTED.Id
                VALUES (@UserId, @Title, @FullAddress, @PostalCode, @ReceiverName, @ReceiverPhone, @IsDefault)", address);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.UserAddresses WHERE Id=@Id AND UserId=@UserId", new { Id = id, UserId = userId });
        }

        public async Task SetDefaultAsync(int id, int userId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("UPDATE koch.UserAddresses SET IsDefault=0 WHERE UserId=@UserId", new { UserId = userId });
            await conn.ExecuteAsync("UPDATE koch.UserAddresses SET IsDefault=1, UpdatedAt=SYSDATETIME() WHERE Id=@Id AND UserId=@UserId", new { Id = id, UserId = userId });
        }
    }
}
