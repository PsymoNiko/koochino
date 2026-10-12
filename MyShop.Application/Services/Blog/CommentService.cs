using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _repo;

        public CommentService(ICommentRepository repo) => _repo = repo;

        public async Task<CommentTreeNodeDto> GetTreeByStoreAsync(long storeId)
        {
            var flat = await _repo.GetTreeFlatByStoreAsync(storeId);
            return BuildTree(flat);
        }

        public async Task<CommentTreeNodeDto> GetTreeByOrderAsync(long orderId)
        {
            var flat = await _repo.GetTreeFlatByOrderAsync(orderId);
            return BuildTree(flat);
        }

        public async Task<CommentTreeNodeDto> GetTreeByProductAsync(long productId)
        {
            var flat = await _repo.GetTreeFlatByProductAsync(productId);
            return BuildTree(flat);
        }

        public Task<CommentDto> GetByIdAsync(long commentId) => _repo.GetByIdAsync(commentId);

        public async Task<long> CreateAsync(long userId, CreateCommentRequest request)
        {
            var isAdminReply = request.ReplierType == 2;
            var comment = new Comment
            {
                ProductId = request.ProductId,
                OrderId = request.OrderId,
                StoreId = request.StoreId,
                UserId = userId,
                ParentId = request.ParentId,
                Text = request.Text,
                Rate = request.Rate,
                ReplierType = request.ReplierType,
                StatusView = isAdminReply ? (byte)1 : (byte)0
            };
            return await _repo.InsertAsync(comment);
        }

        public Task<bool> UpdateStatusAsync(long commentId, byte statusView, int? moderatorUserId = null, string? moderationNotes = null) => _repo.UpdateStatusAsync(commentId, statusView, moderatorUserId, moderationNotes);
        public Task<bool> UpdateContentAsync(long commentId, string text) => _repo.UpdateContentAsync(commentId, text);
        public Task<bool> SoftDeleteAsync(long commentId) => _repo.SoftDeleteAsync(commentId);
        public Task<bool> HardDeleteAsync(long commentId) => _repo.HardDeleteAsync(commentId);
        public Task<List<CommentListItemDto>> GetListForAdminAsync(byte filterType, string searchText) => _repo.GetListForAdminAsync(filterType, searchText ?? "");

        public async Task<CommentTreeNodeDto> GetThreadByCommentIdAsync(long commentId)
        {
            var flat = await _repo.GetThreadFlatByCommentIdAsync(commentId);
            return BuildTree(flat);
        }

        public Task<List<CommentDto>> GetFlatByStoreAsync(int storeId) => _repo.GetTreeFlatByStoreAsync(storeId);
        public Task<CommentCountResultDto?> GetCountByStoreAsync(int storeId) => _repo.GetCountByStoreAsync(storeId);
        public Task<CommentCountResultDto?> GetCountByProductAsync(long productId) => _repo.GetCountByProductAsync(productId);
        public Task<long?> GetCommentIdByOrderAndUserAsync(int orderId, int userId) => _repo.GetCommentIdByOrderAndUserAsync(orderId, userId);

        private static CommentTreeNodeDto BuildTree(List<CommentDto> flat)
        {
            var root = new CommentTreeNodeDto { CommentId = 0, Depth = -1 };
            var byParent = flat.GroupBy(c => c.ParentId ?? 0).ToDictionary(g => g.Key, g => g.ToList());
            void Add(CommentTreeNodeDto parent, long parentId)
            {
                if (!byParent.TryGetValue(parentId, out var list)) return;
                foreach (var d in list.OrderBy(x => x.CreatedAt))
                {
                    var node = new CommentTreeNodeDto
                    {
                        CommentId = d.CommentId,
                        Text = d.Text,
                        Rate = d.Rate,
                        UserId = d.UserId,
                        UserDisplayName = d.UserDisplayName,
                        ReplierType = d.ReplierType,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt,
                        Depth = d.Depth
                    };
                    Add(node, d.CommentId);
                    parent.Children.Add(node);
                }
            }
            Add(root, 0);
            return root;
        }
    }
}
