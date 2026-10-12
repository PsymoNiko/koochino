using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Services.Commerce;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Dto;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class CommercePhaseITests
    {
        [Fact]
        public void WishlistRoute_MapsToCommerce()
        {
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/customer/wishlist"));
        }

        [Fact]
        public async Task FlagOff_ListEmpty_ToggleThrows()
        {
            var h = Harness.Create(false);
            Assert.Empty(await h.Wish.ListAsync(1));
            await Assert.ThrowsAsync<InvalidOperationException>(() => h.Wish.ToggleAsync(1, 10));
        }

        [Fact]
        public async Task Toggle_IsPerUser()
        {
            var h = Harness.Create(true);
            Assert.True(await h.Wish.ToggleAsync(1, 10));
            Assert.True(await h.Wish.ContainsAsync(1, 10));
            Assert.False(await h.Wish.ContainsAsync(2, 10));
            Assert.False(await h.Wish.ToggleAsync(1, 10));
            Assert.False(await h.Wish.ContainsAsync(1, 10));
        }

        [Fact]
        public async Task Review_UpdateOwn_Ok_OtherUser_Denied()
        {
            var comments = new MemComments();
            comments.Items.Add(new CommentDto { CommentId = 5, UserId = 1, Text = "a" });
            var svc = new CommerceProductReviewService(comments);
            await svc.UpdateOwnAsync(1, 5, "b");
            Assert.Equal("b", comments.Items[0].Text);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateOwnAsync(2, 5, "hack"));
        }

        [Fact]
        public async Task Review_Add_UsesExistingCommentService()
        {
            var comments = new MemComments();
            var svc = new CommerceProductReviewService(comments);
            var id = await svc.AddAsync(3, 44, 7, "خوب", 5);
            Assert.True(id > 0);
            Assert.Single(comments.Created);
            Assert.Equal(44, comments.Created[0].ProductId);
            Assert.Equal(7, comments.Created[0].StoreId);
        }

        sealed class Harness
        {
            public CommerceWishlistService Wish = null!;
            public MemWish Repo = new();
            public Flag Flags = new();

            public static Harness Create(bool on)
            {
                var h = new Harness();
                h.Flags.On = on;
                h.Wish = new CommerceWishlistService(h.Repo, h.Flags);
                return h;
            }
        }

        sealed class Flag : ICommerceFeatureFlags
        {
            public bool On;
            public bool IsEnabled(string featureKey) =>
                On && string.Equals(featureKey, CommerceConstants.Features.Wishlist, StringComparison.OrdinalIgnoreCase);
        }

        sealed class MemWish : ICommerceWishlistRepository
        {
            public List<CommerceWishlistItem> All = new();
            int _n = 1;
            public Task<List<CommerceWishlistItem>> ListByUserAsync(int userId) =>
                Task.FromResult(All.Where(x => x.UserId == userId).ToList());
            public Task<CommerceWishlistItem> GetAsync(int userId, int productId) =>
                Task.FromResult(All.FirstOrDefault(x => x.UserId == userId && x.ProductId == productId)!);
            public Task InsertAsync(int userId, int productId)
            {
                All.Add(new CommerceWishlistItem { Id = _n++, UserId = userId, ProductId = productId });
                return Task.CompletedTask;
            }
            public Task DeleteAsync(int userId, int productId)
            {
                All.RemoveAll(x => x.UserId == userId && x.ProductId == productId);
                return Task.CompletedTask;
            }
        }

        sealed class MemComments : ICommentService
        {
            public List<CommentDto> Items = new();
            public List<CreateCommentRequest> Created = new();
            long _n = 1;
            public Task<CommentTreeNodeDto> GetTreeByStoreAsync(long storeId) => Task.FromResult(new CommentTreeNodeDto());
            public Task<CommentTreeNodeDto> GetTreeByOrderAsync(long orderId) => Task.FromResult(new CommentTreeNodeDto());
            public Task<CommentTreeNodeDto> GetTreeByProductAsync(long productId) => Task.FromResult(new CommentTreeNodeDto());
            public Task<CommentDto> GetByIdAsync(long commentId) =>
                Task.FromResult(Items.FirstOrDefault(x => x.CommentId == commentId)!);
            public Task<long> CreateAsync(long userId, CreateCommentRequest request)
            {
                Created.Add(request);
                return Task.FromResult(_n++);
            }
            public Task<bool> UpdateStatusAsync(long commentId, byte statusView, int? moderatorUserId = null, string? moderationNotes = null) => Task.FromResult(true);
            public Task<bool> UpdateContentAsync(long commentId, string text)
            {
                var c = Items.First(x => x.CommentId == commentId);
                c.Text = text;
                return Task.FromResult(true);
            }
            public Task<bool> SoftDeleteAsync(long commentId) => Task.FromResult(true);
            public Task<bool> HardDeleteAsync(long commentId) => Task.FromResult(true);
            public Task<List<CommentListItemDto>> GetListForAdminAsync(byte filterType, string searchText) => Task.FromResult(new List<CommentListItemDto>());
            public Task<CommentTreeNodeDto> GetThreadByCommentIdAsync(long commentId) => Task.FromResult(new CommentTreeNodeDto());
            public Task<List<CommentDto>> GetFlatByStoreAsync(int storeId) => Task.FromResult(new List<CommentDto>());
            public Task<CommentCountResultDto?> GetCountByStoreAsync(int storeId) => Task.FromResult<CommentCountResultDto?>(null);
            public Task<CommentCountResultDto?> GetCountByProductAsync(long productId) => Task.FromResult<CommentCountResultDto?>(null);
            public Task<long?> GetCommentIdByOrderAndUserAsync(int orderId, int userId) => Task.FromResult<long?>(null);
        }
    }
}
