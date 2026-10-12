using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class PwaInstallLogRepository : IPwaInstallLogRepository
    {
        private readonly IDbConnectionFactory _factory;

        public PwaInstallLogRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> InsertAsync(PwaInstallLog log)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", log.UserId);
            parameters.Add("IpAddress", log.IpAddress);
            parameters.Add("UserAgent", log.UserAgent);
            parameters.Add("DeviceType", log.DeviceType);
            parameters.Add("Platform", log.Platform);
            parameters.Add("Language", log.Language);
            parameters.Add("ScreenWidth", log.ScreenWidth);
            parameters.Add("ScreenHeight", log.ScreenHeight);
            parameters.Add("HardwareConcurrency", log.HardwareConcurrency);
            parameters.Add("DeviceMemory", log.DeviceMemory);
            parameters.Add("TimeZone", log.TimeZone);
            parameters.Add("ExtraInfo", log.ExtraInfo);
            parameters.Add("MacAddress", log.MacAddress);
            parameters.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPwaInstallLog_Insert",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return parameters.Get<int>("Id");
        }

        public async Task<List<PwaInstallLog>> GetListAsync(int skip = 0, int take = 50)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<PwaInstallLog>(
                "koch.spPwaInstallLog_GetList",
                new { Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure
            );
            return list.ToList();
        }

        public async Task<int> GetTotalCountAsync()
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                "koch.spPwaInstallLog_GetCount",
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
