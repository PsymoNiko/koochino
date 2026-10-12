using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Infrastructure.Data
{
    public class ChanceBoxRepository : IChanceBoxRepository
    {
        private readonly IDbConnectionFactory _factory;

        public ChanceBoxRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> CreateCodeAsync(ChanceBoxCode code)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("StoreId", code.StoreId);
            parameters.Add("StoreOwnerUserId", code.StoreOwnerUserId);
            parameters.Add("Code", code.Code);
            parameters.Add("MinPurchaseAmount", code.MinPurchaseAmount);
            parameters.Add("Description", code.Description);
            parameters.Add("PrizeDescription", code.PrizeDescription);
            parameters.Add("ChanceBoxCodeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spChanceBoxCode_Create",
                parameters,
                commandType: CommandType.StoredProcedure);

            var id = parameters.Get<int>("ChanceBoxCodeId");
            code.ChanceBoxCodeId = id;
            return id;
        }

        public async Task<ChanceBoxCode> GetByCodeAsync(string code)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ChanceBoxCode>(
                "koch.spChanceBoxCode_GetByCode",
                new { Code = code },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<ChanceBoxCode> GetLastForStoreAsync(int storeId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ChanceBoxCode>(
                "koch.spChanceBoxCode_GetLastForStore",
                new { StoreId = storeId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<ChanceBoxCode>> GetByStoreOwnerUserIdAsync(int storeOwnerUserId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<ChanceBoxCode>(
                "koch.spChanceBoxCode_GetByStoreOwnerUserId",
                new { StoreOwnerUserId = storeOwnerUserId },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<(bool Success, int ChanceBoxCodeUsageId)> MarkUsedAsync(int chanceBoxCodeId, int userId, long purchaseAmount)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("ChanceBoxCodeId", chanceBoxCodeId);
            parameters.Add("UserId", userId);
            parameters.Add("PurchaseAmount", purchaseAmount);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("ChanceBoxCodeUsageId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spChanceBoxCode_MarkUsed",
                parameters,
                commandType: CommandType.StoredProcedure);

            var result = parameters.Get<int>("Result") == 1;
            var usageId = parameters.Get<int>("ChanceBoxCodeUsageId");
            return (result, usageId);
        }

        public async Task SetChanceBoxCodeUsageOrderIdAsync(int chanceBoxCodeUsageId, int orderId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spChanceBoxCodeUsage_SetOrderId",
                new { Id = chanceBoxCodeUsageId, OrderId = orderId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<ChanceBoxCodeUsage>> GetUsagesByStoreOwnerUserIdAsync(int storeOwnerUserId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<ChanceBoxCodeUsage>(
                "koch.spChanceBoxCodeUsage_GetByStoreOwnerUserId",
                new { StoreOwnerUserId = storeOwnerUserId },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<AdminChanceBoxStoreDto>> GetAdminStoresWithChanceBoxAsync(string? search)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<AdminChanceBoxStoreDto>(
                "koch.spChanceBox_AdminStoresWithChanceBox",
                new { Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim() },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<ChanceBoxUsageDetailDto>> GetUsagesByStoreIdAsync(int storeId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<ChanceBoxUsageDetailDto>(
                "koch.spChanceBoxCodeUsage_GetByStoreId",
                new { StoreId = storeId },
                commandType: CommandType.StoredProcedure);
            return result.ToList();
        }
    }
}

