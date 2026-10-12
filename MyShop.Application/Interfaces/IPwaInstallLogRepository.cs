using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IPwaInstallLogRepository
    {
        Task<int> InsertAsync(PwaInstallLog log);
        Task<List<PwaInstallLog>> GetListAsync(int skip = 0, int take = 50);
        Task<int> GetTotalCountAsync();
    }
}
