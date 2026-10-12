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
    public class UserActivityRepository : IUserActivityRepository
    {
        private readonly IDbConnectionFactory _factory;

        public UserActivityRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> LogActivityAsync(int userId, string activityType, string sessionId = null, 
            string connectionId = null, string ipAddress = null, string userAgent = null)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            parameters.Add("@ActivityType", activityType);
            parameters.Add("@SessionId", sessionId);
            parameters.Add("@ConnectionId", connectionId);
            parameters.Add("@IpAddress", ipAddress);
            parameters.Add("@UserAgent", userAgent);
            parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserActivityLog_Insert",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Id");
        }

        public async Task UpdateHeartbeatAsync(int userId, string sessionId = null, string connectionId = null)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spUserActivityLog_UpdateHeartbeat",
                new { UserId = userId, SessionId = sessionId, ConnectionId = connectionId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task LogoutAsync(int userId, string sessionId = null, string connectionId = null)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spUserActivityLog_Logout",
                new { UserId = userId, SessionId = sessionId, ConnectionId = connectionId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<OnlineUser>> GetOnlineUsersAsync(int inactiveMinutes = 5)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<OnlineUser>(
                "koch.spUserActivityLog_GetOnlineUsers",
                new { InactiveMinutes = inactiveMinutes },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<int> GetOnlineUserCountAsync(int inactiveMinutes = 5)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<int>(
                "koch.spUserActivityLog_GetOnlineCount",
                new { InactiveMinutes = inactiveMinutes },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<DailyLoginStat>> GetDailyLoginStatsAsync(int days = 30)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<DailyLoginStat>(
                "koch.spUserActivityLog_GetDailyLoginStats",
                new { Days = days },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<int> CleanupInactiveSessionsAsync(int inactiveMinutes = 30)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryFirstOrDefaultAsync<int>(
                "koch.spUserActivityLog_CleanupInactive",
                new { InactiveMinutes = inactiveMinutes },
                commandType: CommandType.StoredProcedure
            );
            return result;
        }
    }
}
