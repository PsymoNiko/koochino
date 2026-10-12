using MyShop.Core.Models.AdminAccess;
using MyShop.Core.Models.Commerce;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.AdminAccess
{
    public interface IAdminAccessAuditRepository
    {
        Task InsertAsync(AdminAccessAuditEntry entry);
        Task<List<AdminAccessAuditEntry>> ListAsync(int top = 100);
    }
}

namespace MyShop.Application.Interfaces.Commerce
{
    public interface IUserAddressRepository
    {
        Task<List<UserAddress>> ListByUserAsync(int userId);
        Task<UserAddress> GetAsync(int id, int userId);
        Task<int> UpsertAsync(UserAddress address);
        Task DeleteAsync(int id, int userId);
        Task SetDefaultAsync(int id, int userId);
    }

    public interface IUserAddressService
    {
        Task<List<UserAddress>> ListAsync(int userId);
        Task<int> SaveAsync(UserAddress address);
        Task DeleteAsync(int id, int userId);
        Task SetDefaultAsync(int id, int userId);
    }
}
