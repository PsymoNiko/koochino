using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class UserActivityService : IUserActivityService
    {
        private readonly IUserActivityRepository _repository;

        public UserActivityService(IUserActivityRepository repository)
        {
            _repository = repository;
        }

        public async Task LogLoginAsync(int userId, string sessionId = null, string connectionId = null, 
            string ipAddress = null, string userAgent = null)
        {
            await _repository.LogActivityAsync(userId, "Login", sessionId, connectionId, ipAddress, userAgent);
        }

        public async Task UpdateHeartbeatAsync(int userId, string sessionId = null, string connectionId = null)
        {
            await _repository.UpdateHeartbeatAsync(userId, sessionId, connectionId);
        }

        public async Task LogLogoutAsync(int userId, string sessionId = null, string connectionId = null)
        {
            await _repository.LogoutAsync(userId, sessionId, connectionId);
        }

        public async Task<List<OnlineUser>> GetOnlineUsersAsync(int inactiveMinutes = 5)
        {
            return await _repository.GetOnlineUsersAsync(inactiveMinutes);
        }

        public async Task<int> GetOnlineUserCountAsync(int inactiveMinutes = 5)
        {
            return await _repository.GetOnlineUserCountAsync(inactiveMinutes);
        }

        public async Task<List<DailyLoginStat>> GetDailyLoginStatsAsync(int days = 30)
        {
            return await _repository.GetDailyLoginStatsAsync(days);
        }
    }
}
