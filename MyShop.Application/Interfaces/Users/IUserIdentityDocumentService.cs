using MyShop.Core.Models.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Users
{
    public interface IUserIdentityDocumentService
    {
        /// <summary>آخرین وضعیت دو نوع مدرک (کارت ملی، سلفی) + وضعیت تجمیعی برای گیت‌ها</summary>
        Task<UserIdentityDocumentsSummary> GetSummaryAsync(int userId);

        Task<UserIdentityDocument> UploadAsync(int userId, int? verificationId, string documentType, string imageUrl, string verificationCode = null);
        Task<bool> ApproveAsync(int documentId, int reviewedBy);
        Task<bool> RejectAsync(int documentId, int reviewedBy, string rejectionReason);
        Task<List<UserIdentityDocument>> GetLatestForAdminAsync(int count, string statusFilter = null);
    }
}
