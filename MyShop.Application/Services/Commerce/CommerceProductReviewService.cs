using System;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Services.Commerce
{
    public class CommerceProductReviewService : ICommerceProductReviewService
    {
        private readonly ICommentService _comments;

        public CommerceProductReviewService(ICommentService comments) => _comments = comments;

        public async Task<CommentTreeNodeDto> GetTreeAsync(int productId)
        {
            if (productId <= 0 || _comments == null)
                return new CommentTreeNodeDto();
            try { return await _comments.GetTreeByProductAsync(productId) ?? new CommentTreeNodeDto(); }
            catch { return new CommentTreeNodeDto(); }
        }

        public async Task<long> AddAsync(int userId, int productId, int? storeId, string text, byte? rate)
        {
            if (userId <= 0) throw new InvalidOperationException("برای ثبت نظر وارد شوید.");
            if (productId <= 0) throw new InvalidOperationException("محصول نامعتبر است.");
            text = (text ?? "").Trim();
            if (text.Length == 0 && !rate.HasValue)
                throw new InvalidOperationException("متن یا امتیاز الزامی است.");
            if (rate.HasValue && (rate.Value < 1 || rate.Value > 5))
                throw new InvalidOperationException("امتیاز باید بین ۱ و ۵ باشد.");
            return await _comments.CreateAsync(userId, new CreateCommentRequest
            {
                ProductId = productId,
                StoreId = storeId,
                Text = text.Length == 0 ? " " : text,
                Rate = rate,
                ReplierType = 1
            });
        }

        public async Task UpdateOwnAsync(int userId, long commentId, string text)
        {
            if (userId <= 0) throw new InvalidOperationException("برای ویرایش نظر وارد شوید.");
            var c = await _comments.GetByIdAsync(commentId);
            if (c == null) throw new InvalidOperationException("نظر یافت نشد.");
            if (c.UserId != userId)
                throw new InvalidOperationException("فقط نظر خودتان را می‌توانید ویرایش کنید.");
            text = (text ?? "").Trim();
            if (text.Length == 0) throw new InvalidOperationException("متن نظر خالی است.");
            await _comments.UpdateContentAsync(commentId, text);
        }
    }
}
