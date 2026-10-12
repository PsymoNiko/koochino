using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Interfaces
{
    public interface ICommentService
    {
        Task<CommentTreeNodeDto> GetTreeByStoreAsync(long storeId);
        Task<CommentTreeNodeDto> GetTreeByOrderAsync(long orderId);
        Task<CommentTreeNodeDto> GetTreeByProductAsync(long productId);
        Task<CommentDto> GetByIdAsync(long commentId);
        Task<long> CreateAsync(long userId, CreateCommentRequest request);
        Task<bool> UpdateStatusAsync(long commentId, byte statusView, int? moderatorUserId = null, string? moderationNotes = null);
        Task<bool> UpdateContentAsync(long commentId, string text);
        Task<bool> SoftDeleteAsync(long commentId);
        Task<bool> HardDeleteAsync(long commentId);
        Task<List<CommentListItemDto>> GetListForAdminAsync(byte filterType, string searchText);
        Task<CommentTreeNodeDto> GetThreadByCommentIdAsync(long commentId);
        Task<List<CommentDto>> GetFlatByStoreAsync(int storeId);
        Task<CommentCountResultDto?> GetCountByStoreAsync(int storeId);
        Task<CommentCountResultDto?> GetCountByProductAsync(long productId);
        Task<long?> GetCommentIdByOrderAndUserAsync(int orderId, int userId);
    }
}
