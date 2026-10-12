using MyShop.Core.Models.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Users
{
    public interface IUserIdentityDocumentRepository
    {
        Task<UserIdentityDocument> GetLatestByUserIdAndTypeAsync(int userId, string documentType);
        Task<List<UserIdentityDocument>> GetAllLatestByUserIdAsync(int userId);
        Task<UserIdentityDocument> GetByIdAsync(int documentId);
        Task<int> CreateAsync(UserIdentityDocument document);
        Task<bool> ApproveAsync(int documentId, int reviewedBy);
        Task<bool> RejectAsync(int documentId, int reviewedBy, string rejectionReason);
        Task<List<UserIdentityDocument>> GetLatestForAdminAsync(int count, string statusFilter = null);
    }
}
