using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IStoreDocumentRepository
    {
        Task<List<StoreDocument>> GetByStoreIdAsync(int storeId);
        Task<StoreDocument> GetByIdAsync(int documentId);
        Task<List<StoreDocumentWithStoreDto>> GetLatestAsync(int count);
        Task<int> CreateAsync(StoreDocument document);
        Task<bool> UpdateAsync(StoreDocument document);
        Task<bool> DeleteAsync(int documentId);
        Task<bool> ApproveAsync(int documentId, bool isApproved, int approvedBy);
        /// <summary>انتخاب یک سند (عکس) به عنوان کاور پروفایل فروشگاه. DocumentType آن سند به «کاور» و Store.StoreProfileImageUrl به‌روز می‌شود.</summary>
        Task<bool> SetAsProfileCoverAsync(int storeId, int documentId);
    }
}
