using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Users;
using MyShop.Core.Models.Users;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Users
{
    public class UserIdentityDocumentService : IUserIdentityDocumentService
    {
        private readonly IUserIdentityDocumentRepository _repo;
        private readonly IUserService _userService;

        public UserIdentityDocumentService(IUserIdentityDocumentRepository repo, IUserService userService)
        {
            _repo = repo;
            _userService = userService;
        }

        public async Task<UserIdentityDocumentsSummary> GetSummaryAsync(int userId)
        {
            var all = await _repo.GetAllLatestByUserIdAsync(userId);
            var summary = new UserIdentityDocumentsSummary
            {
                NationalCard = all.FirstOrDefault(d => d.DocumentType == IdentityDocumentTypes.NationalCard),
                Selfie = all.FirstOrDefault(d => d.DocumentType == IdentityDocumentTypes.Selfie)
            };
            summary.AggregatedStatus = ComputeAggregatedStatus(summary, out var reason);
            summary.RejectionReason = reason;
            return summary;
        }

        private static string ComputeAggregatedStatus(UserIdentityDocumentsSummary s, out string rejectionReason)
        {
            rejectionReason = null;
            if (s.NationalCard == null && s.Selfie == null) return null;

            if (s.NationalCard?.Status == "Rejected")
            {
                rejectionReason = s.NationalCard.RejectionReason;
                return "Rejected";
            }
            if (s.Selfie?.Status == "Rejected")
            {
                rejectionReason = s.Selfie.RejectionReason;
                return "Rejected";
            }
            if (s.NationalCard?.Status == "Approved" && s.Selfie?.Status == "Approved")
                return "Approved";

            return "Pending";
        }

        public async Task<UserIdentityDocument> UploadAsync(int userId, int? verificationId, string documentType, string imageUrl, string verificationCode = null)
        {
            var doc = new UserIdentityDocument
            {
                UserId = userId,
                VerificationId = verificationId,
                DocumentType = documentType,
                ImageUrl = imageUrl,
                VerificationCode = verificationCode,
                Status = "Pending"
            };
            doc.DocumentId = await _repo.CreateAsync(doc);
            await SyncAggregateAsync(userId);
            return doc;
        }

        public async Task<bool> ApproveAsync(int documentId, int reviewedBy)
        {
            var result = await _repo.ApproveAsync(documentId, reviewedBy);
            if (result)
            {
                var doc = await _repo.GetByIdAsync(documentId);
                if (doc != null) await SyncAggregateAsync(doc.UserId);
            }
            return result;
        }

        public async Task<bool> RejectAsync(int documentId, int reviewedBy, string rejectionReason)
        {
            var result = await _repo.RejectAsync(documentId, reviewedBy, rejectionReason);
            if (result)
            {
                var doc = await _repo.GetByIdAsync(documentId);
                if (doc != null) await SyncAggregateAsync(doc.UserId);
            }
            return result;
        }

        public Task<List<UserIdentityDocument>> GetLatestForAdminAsync(int count, string statusFilter = null)
            => _repo.GetLatestForAdminAsync(count, statusFilter);

        private async Task SyncAggregateAsync(int userId)
        {
            var summary = await GetSummaryAsync(userId);
            await _userService.SetIdentityVerificationStatusAsync(userId, summary.AggregatedStatus);
        }
    }
}
