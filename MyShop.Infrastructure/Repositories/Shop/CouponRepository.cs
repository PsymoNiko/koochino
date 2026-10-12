using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Data
{
    public class CouponRepository : ICouponRepository
    {
        private readonly IDbConnectionFactory _factory;

        public CouponRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private static (int DiscountType, long? DiscountValue) ResolveDiscountTypeValue(Coupon coupon)
        {
            if (coupon.DiscountValue.HasValue)
                return (coupon.DiscountType, coupon.DiscountValue);
            if (coupon.DiscountAmount.HasValue)
                return (0, (long)coupon.DiscountAmount.Value);
            if (coupon.DiscountPercent.HasValue)
                return (1, (long)coupon.DiscountPercent.Value);
            return (0, null);
        }

        public async Task<List<Coupon>> GetByUserIdAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<Coupon>(
                "koch.spCopon_GetByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<List<Coupon>> GetByStoreIdAsync(int storeId)
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<Coupon>(
                "koch.spCopon_GetByStoreId",
                new { StoreId = storeId },
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<List<Coupon>> GetAllPendingAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<Coupon>(
                "koch.spCopon_GetAllPending",
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<List<Coupon>> GetAllActiveAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<Coupon>(
                "koch.spCopon_GetAllActive",
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<List<Coupon>> GetAllAsync()
        {
            using var conn = _factory.CreateConnection();
            return (await conn.QueryAsync<Coupon>(
                "koch.spCopon_GetAll",
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<Coupon> GetByIdAsync(int couponId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Coupon>(
                "koch.spCopon_GetById",
                new { CoponId = couponId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CreateAsync(Coupon coupon)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("CoponTitle", coupon.CoponTitle);
            parameters.Add("CoponCode", coupon.CoponCode ?? "");
            parameters.Add("CoponExpireDate", coupon.CoponExpireDate);
            parameters.Add("CoponStartDate", coupon.CoponStartDate);
            parameters.Add("CoponStock", coupon.CoponStock);
            parameters.Add("CoponMinBuy", coupon.CoponMinBuy);
            parameters.Add("CoponMaxBuy", coupon.CoponMaxBuy);
            parameters.Add("StoreID", coupon.StoreID);
            parameters.Add("UserID", coupon.UserID);
            var (discType, discVal) = ResolveDiscountTypeValue(coupon);
            parameters.Add("DiscountType", (byte)discType);
            parameters.Add("DiscountValue", discVal);
            parameters.Add("CoponId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spCopon_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("CoponId");
        }

        public async Task<bool> UpdateAsync(Coupon coupon)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("CoponId", coupon.CoponID);
            parameters.Add("CoponTitle", coupon.CoponTitle);
            parameters.Add("CoponCode", coupon.CoponCode ?? "");
            parameters.Add("CoponExpireDate", coupon.CoponExpireDate);
            parameters.Add("CoponStartDate", coupon.CoponStartDate);
            parameters.Add("CoponStock", coupon.CoponStock);
            parameters.Add("CoponMinBuy", coupon.CoponMinBuy);
            parameters.Add("CoponMaxBuy", coupon.CoponMaxBuy);
            var (discType, discVal) = ResolveDiscountTypeValue(coupon);
            parameters.Add("DiscountType", (byte)discType);
            parameters.Add("DiscountValue", discVal);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spCopon_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var result = parameters.Get<int>("Result");
            if (result == -2)
                throw new InvalidOperationException("DUPLICATE_COUPON_CODE");
            return result == 1;
        }

        public async Task<bool> DeleteAsync(int couponId)
        {
            using var conn = _factory.CreateConnection();
            await ReferenceCount.EnsureUnusedAsync(conn, new { Id = couponId },
                ("SELECT COUNT(1) FROM koch.CouponUsage WHERE CoponID=@Id", "استفاده در سفارش"));
            var parameters = new DynamicParameters();
            parameters.Add("CoponId", couponId);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spCopon_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> ApproveByAdminAsync(int couponId, bool approved)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("CoponId", couponId);
            parameters.Add("CoponStatAdmin", approved);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spCopon_ApproveByAdmin",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> ToggleStoreStatusAsync(int couponId, bool status)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("CoponId", couponId);
            parameters.Add("CoponStatStor", status);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spCopon_ToggleStoreStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<CouponValidateResultDto> ValidateCouponAsync(int userId, string couponCode, long purchaseAmount)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);
            parameters.Add("CouponCode", couponCode?.Trim());
            parameters.Add("PurchaseAmount", purchaseAmount);
            parameters.Add("Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            parameters.Add("Message", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);
            parameters.Add("DiscountAmount", dbType: DbType.Int64, direction: ParameterDirection.Output);
            parameters.Add("CoponID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spCopon_Validate",
                parameters,
                commandType: CommandType.StoredProcedure);

            return new CouponValidateResultDto
            {
                Success = parameters.Get<bool>("Success"),
                Message = parameters.Get<string>("Message") ?? "",
                DiscountAmount = parameters.Get<long>("DiscountAmount"),
                CoponID = parameters.Get<int>("CoponID")
            };
        }

        public async Task<(int Result, int NewId)> RecordCouponUsageAsync(int coponId, int userId, long purchaseAmount, long discountAmount)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("CoponID", coponId);
            parameters.Add("UserId", userId);
            parameters.Add("PurchaseAmount", purchaseAmount);
            parameters.Add("DiscountAmount", discountAmount);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spCouponUsage_Insert",
                parameters,
                commandType: CommandType.StoredProcedure);

            return (parameters.Get<int>("Result"), parameters.Get<int>("NewId"));
        }

        public async Task SetCouponUsageRedeemStoreAsync(int couponUsageId, int redeemStoreId, long? reservedAmount)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spCouponUsage_SetRedeemStore",
                new { Id = couponUsageId, RedeemStoreId = redeemStoreId, ReservedAmount = reservedAmount },
                commandType: CommandType.StoredProcedure);
        }

        public async Task SetCouponUsageOrderIdAsync(int couponUsageId, int orderId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spCouponUsage_SetOrderId",
                new { Id = couponUsageId, OrderId = orderId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<CouponUsage>> GetUsagesByUserIdAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CouponUsage>(
                "koch.spCouponUsage_GetByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
            return list.ToList();
        }

        public async Task<List<CouponUsageReportDto>> GetUsageReportForAdminAsync(int? userIdFilter, int? coponIdFilter)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CouponUsageReportDto>(
                "koch.spCouponUsage_GetForAdmin",
                new { UserIdFilter = userIdFilter, CoponIDFilter = coponIdFilter },
                commandType: CommandType.StoredProcedure);
            return list.ToList();
        }

        public async Task<List<CouponUsageStoreOwnerDto>> GetUsageReportByStoreOwnerAsync(int storeOwnerUserId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<CouponUsageStoreOwnerDto>(
                "koch.spCouponUsage_GetReportByStoreOwnerUserId",
                new { StoreOwnerUserId = storeOwnerUserId },
                commandType: CommandType.StoredProcedure);
            return list.ToList();
        }

        public async Task<bool> ApproveCouponUsageByStoreAsync(int usageId, int storeOwnerUserId)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UsageId", usageId);
            parameters.Add("StoreOwnerUserId", storeOwnerUserId);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync(
                "koch.spCouponUsage_ApproveByStore",
                parameters,
                commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("Result") == 1;
        }

        public async Task<List<int>> GetStoreIdsHavingCouponAsync()
        {
            using var conn = _factory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT [StoreID] FROM [koch].[Copon]
                WHERE [CoponStatAdmin] = 1 AND [CoponStatStor] = 1
                  AND ([CoponExpireDate] IS NULL OR [CoponExpireDate] >= GETDATE())
                  AND ([CoponStock] IS NULL OR [CoponStock] > 0)";
            var list = await conn.QueryAsync<int>(sql);
            return list.ToList();
        }
    }
}
