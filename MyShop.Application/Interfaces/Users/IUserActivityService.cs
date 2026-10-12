using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IUserActivityService
    {
        Task LogLoginAsync(int userId, string sessionId = null, string connectionId = null, 
            string ipAddress = null, string userAgent = null);
        
        Task UpdateHeartbeatAsync(int userId, string sessionId = null, string connectionId = null);
        
        Task LogLogoutAsync(int userId, string sessionId = null, string connectionId = null);
        
        Task<List<OnlineUser>> GetOnlineUsersAsync(int inactiveMinutes = 5);
        
        Task<int> GetOnlineUserCountAsync(int inactiveMinutes = 5);
        
        Task<List<DailyLoginStat>> GetDailyLoginStatsAsync(int days = 30);
    }
}
