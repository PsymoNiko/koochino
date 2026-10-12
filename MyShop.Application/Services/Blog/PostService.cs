using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _repository;
        private readonly IAuthRepository _authRepository;

        public PostService(IPostRepository repository, IAuthRepository authRepository)
        {
            _repository = repository;
            _authRepository = authRepository;
        }

        public Task<Post> GetByIdAsync(int id)
            => _repository.GetByIdAsync(id);

        public Task<IEnumerable<Post>> GetAllAsync(bool publishedOnly = true)
            => _repository.GetAllAsync(publishedOnly);

        public Task<IEnumerable<Post>> GetLatestAsync(int count, bool publishedOnly = true)
            => _repository.GetLatestAsync(count, publishedOnly);

        public async Task<int> CreateAsync(CreatePostRequest request, int authorId)
        {
            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                Summary = request.Summary ?? string.Empty,
                IsPublished = request.IsPublished,
                ImageUrl = request.ImageUrl,
                Tags = request.Tags,
                CreatedAt = System.DateTime.Now
            };

            return await _repository.CreateAsync(post, authorId);
        }

        public async Task<bool> UpdateAsync(UpdatePostRequest request)
        {
            var existingPost = await _repository.GetByIdAsync(request.Id);
            if (existingPost == null)
                return false;

            existingPost.Title = request.Title;
            existingPost.Content = request.Content;
            existingPost.Summary = request.Summary ?? string.Empty;
            existingPost.IsPublished = request.IsPublished;
            existingPost.ImageUrl = request.ImageUrl;
            existingPost.Tags = request.Tags;
            existingPost.UpdatedAt = System.DateTime.Now;

            return await _repository.UpdateAsync(existingPost);
        }

        public Task<bool> DeleteAsync(int id)
            => _repository.DeleteAsync(id);

        public Task<bool> IncrementViewCountAsync(int id)
            => _repository.IncrementViewCountAsync(id);

        public Task<IEnumerable<Post>> SearchAsync(string searchTerm)
            => _repository.SearchAsync(searchTerm);
    }
}
