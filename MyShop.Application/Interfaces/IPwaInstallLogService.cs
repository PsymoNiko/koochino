using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IPwaInstallLogService
    {
        Task<int> LogInstallAsync(int? userId, string ipAddress, string userAgent, string deviceType = null,
            string platform = null, string language = null, int? screenWidth = null, int? screenHeight = null,
            int? hardwareConcurrency = null, decimal? deviceMemory = null, string timeZone = null,
            string extraInfo = null, string macAddress = null);
        Task<List<PwaInstallLog>> GetListAsync(int skip = 0, int take = 50);
        Task<int> GetTotalCountAsync();
    }
}
