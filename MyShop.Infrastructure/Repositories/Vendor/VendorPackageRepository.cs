using Dapper;
using MyShop.Application.Interfaces.Vendor;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Vendor;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Vendor
{
    public class VendorPackageRepository : IVendorPackageRepository
    {
        private readonly IDbConnectionFactory _factory;
        public VendorPackageRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<VendorPackage>> ListAsync(bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var packages = (await conn.QueryAsync<VendorPackage>(@"
                SELECT Id, Name, Description, Price, DurationDays, FeaturesJson, IsActive, SortOrder, CreatedAt
                FROM koch.VendorPackages
                WHERE (@ActiveOnly = 0 OR IsActive = 1)
                ORDER BY SortOrder, Id", new { ActiveOnly = activeOnly })).ToList();
            if (packages.Count == 0) return packages;
            var modules = await conn.QueryAsync<VendorPackageModule>(@"
                SELECT PackageId, ModuleKey, LimitsJson FROM koch.VendorPackageModules
                WHERE PackageId IN @Ids", new { Ids = packages.Select(x => x.Id).ToArray() });
            var map = modules.GroupBy(x => x.PackageId).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var package in packages)
                package.Modules = map.TryGetValue(package.Id, out var rows) ? rows : new List<VendorPackageModule>();
            return packages;
        }

        public async Task<VendorPackage> GetAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var package = await conn.QueryFirstOrDefaultAsync<VendorPackage>(@"
                SELECT Id, Name, Description, Price, DurationDays, FeaturesJson, IsActive, SortOrder, CreatedAt
                FROM koch.VendorPackages WHERE Id = @Id", new { Id = id });
            if (package != null)
                package.Modules = (await conn.QueryAsync<VendorPackageModule>(
                    "SELECT PackageId, ModuleKey, LimitsJson FROM koch.VendorPackageModules WHERE PackageId = @Id",
                    new { Id = id })).ToList();
            return package;
        }

        public async Task<int> UpsertAsync(VendorPackage package)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            if (package.Id == 0)
                package.Id = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO koch.VendorPackages (Name, Description, Price, DurationDays, FeaturesJson, IsActive, SortOrder)
                    OUTPUT INSERTED.Id VALUES (@Name, @Description, @Price, @DurationDays, @FeaturesJson, @IsActive, @SortOrder)",
                    package, tx);
            else
                await conn.ExecuteAsync(@"
                    UPDATE koch.VendorPackages SET Name=@Name, Description=@Description, Price=@Price,
                    DurationDays=@DurationDays, FeaturesJson=@FeaturesJson, IsActive=@IsActive, SortOrder=@SortOrder
                    WHERE Id=@Id", package, tx);
            await conn.ExecuteAsync("DELETE FROM koch.VendorPackageModules WHERE PackageId=@Id", new { package.Id }, tx);
            foreach (var module in package.Modules ?? new List<VendorPackageModule>())
                await conn.ExecuteAsync(@"
                    INSERT INTO koch.VendorPackageModules (PackageId, ModuleKey, LimitsJson)
                    VALUES (@PackageId, @ModuleKey, @LimitsJson)",
                    new { PackageId = package.Id, module.ModuleKey, module.LimitsJson }, tx);
            tx.Commit();
            return package.Id;
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.VendorPackages WHERE Id=@Id", new { Id = id });
        }

        public async Task<long> CreateSubscriptionAsync(VendorSubscription subscription)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<long>(@"
                INSERT INTO koch.VendorSubscriptions (UserId, PackageId, Status, PaymentGateway)
                OUTPUT INSERTED.Id VALUES (@UserId, @PackageId, @Status, @PaymentGateway)", subscription);
        }

        public async Task UpdateSubscriptionPaymentAsync(long id, string status, string paymentRef, string gateway)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE s SET Status=@Status, PaymentRef=@PaymentRef, PaymentGateway=@Gateway,
                    PaidAt=CASE WHEN @Status='Active' THEN SYSDATETIME() ELSE PaidAt END,
                    ExpiresAt=CASE WHEN @Status='Active' THEN DATEADD(DAY,p.DurationDays,SYSDATETIME()) ELSE ExpiresAt END
                FROM koch.VendorSubscriptions s JOIN koch.VendorPackages p ON p.Id=s.PackageId WHERE s.Id=@Id",
                new { Id = id, Status = status, PaymentRef = paymentRef, Gateway = gateway });
        }
    }
}
