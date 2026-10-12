using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Interfaces
{
    public interface ICommentRepository
    {
        Task<long> InsertAsync(Comment comment);
        Task<CommentDto> GetByIdAsync(long commentId);
        Task<List<CommentDto>> GetTreeFlatByStoreAsync(long storeId);
        Task<List<CommentDto>> GetTreeFlatByOrderAsync(long orderId);
        Task<List<CommentDto>> GetTreeFlatByProductAsync(long productId);
        Task<bool> UpdateStatusAsync(long commentId, byte statusView, int? moderatorUserId = null, string? moderationNotes = null);
        Task<bool> UpdateContentAsync(long commentId, string text);
        Task<bool> HardDeleteAsync(long commentId);
        Task<bool> SoftDeleteAsync(long commentId);
        Task<CommentCountResultDto?> GetCountByStoreAsync(int storeId);
        Task<CommentCountResultDto?> GetCountByProductAsync(long productId);
        Task<long?> GetCommentIdByOrderAndUserAsync(int orderId, int userId);
        /// <summary>FilterType: 1=پیغام، 2=کاربر، 3=فروشگاه، 4=شماره سفارش</summary>
        Task<List<CommentListItemDto>> GetListForAdminAsync(byte filterType, string searchText);
        Task<List<CommentDto>> GetThreadFlatByCommentIdAsync(long commentId);
    }
}
