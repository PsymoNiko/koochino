using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Dto;
using MyShop.Core.Models.Products;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.Commerce
{
    public class CommerceWishlistItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }

    public interface ICommerceWishlistRepository
    {
        Task<List<CommerceWishlistItem>> ListByUserAsync(int userId);
        Task<CommerceWishlistItem> GetAsync(int userId, int productId);
        Task InsertAsync(int userId, int productId);
        Task DeleteAsync(int userId, int productId);
    }

    public interface ICommerceWishlistService
    {
        Task<List<CommerceWishlistItem>> ListAsync(int userId);
        Task<bool> ContainsAsync(int userId, int productId);
        Task<bool> ToggleAsync(int userId, int productId);
        Task RemoveAsync(int userId, int productId);
    }

    public interface ICommerceProductReviewService
    {
        Task<CommentTreeNodeDto> GetTreeAsync(int productId);
        Task<long> AddAsync(int userId, int productId, int? storeId, string text, byte? rate);
        Task UpdateOwnAsync(int userId, long commentId, string text);
    }
}
