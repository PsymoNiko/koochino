using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class PwaInstallLogService : IPwaInstallLogService
    {
        private readonly IPwaInstallLogRepository _repository;

        public PwaInstallLogService(IPwaInstallLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> LogInstallAsync(int? userId, string ipAddress, string userAgent, string deviceType = null,
            string platform = null, string language = null, int? screenWidth = null, int? screenHeight = null,
            int? hardwareConcurrency = null, decimal? deviceMemory = null, string timeZone = null,
            string extraInfo = null, string macAddress = null)
        {
            var log = new PwaInstallLog
            {
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = deviceType,
                Platform = platform,
                Language = language,
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                HardwareConcurrency = hardwareConcurrency,
                DeviceMemory = deviceMemory,
                TimeZone = timeZone,
                ExtraInfo = extraInfo,
                MacAddress = macAddress
            };
            return await _repository.InsertAsync(log);
        }

        public Task<List<PwaInstallLog>> GetListAsync(int skip = 0, int take = 50)
            => _repository.GetListAsync(skip, take);

        public Task<int> GetTotalCountAsync()
            => _repository.GetTotalCountAsync();
    }
}
