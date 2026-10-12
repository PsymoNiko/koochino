using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class StoreDocumentService : IStoreDocumentService
    {
        private readonly IStoreDocumentRepository _repo;

        public StoreDocumentService(IStoreDocumentRepository repo)
        {
            _repo = repo;
        }

        public Task<List<StoreDocument>> GetByStoreIdAsync(int storeId) => _repo.GetByStoreIdAsync(storeId);
        public Task<StoreDocument> GetByIdAsync(int documentId) => _repo.GetByIdAsync(documentId);
        public Task<List<StoreDocumentWithStoreDto>> GetLatestAsync(int count) => _repo.GetLatestAsync(count);
        public Task<int> CreateAsync(StoreDocument document) => _repo.CreateAsync(document);
        public Task<bool> UpdateAsync(StoreDocument document) => _repo.UpdateAsync(document);
        public Task<bool> DeleteAsync(int documentId) => _repo.DeleteAsync(documentId);
        public Task<bool> ApproveAsync(int documentId, bool isApproved, int approvedBy) => _repo.ApproveAsync(documentId, isApproved, approvedBy);
        public Task<bool> SetAsProfileCoverAsync(int storeId, int documentId) => _repo.SetAsProfileCoverAsync(storeId, documentId);
    }
}
