using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IPostService
    {
        Task<Post> GetByIdAsync(int id);
        Task<IEnumerable<Post>> GetAllAsync(bool publishedOnly = true);
        Task<IEnumerable<Post>> GetLatestAsync(int count, bool publishedOnly = true);
        Task<int> CreateAsync(CreatePostRequest request, int authorId);
        Task<bool> UpdateAsync(UpdatePostRequest request);
        Task<bool> DeleteAsync(int id);
        Task<bool> IncrementViewCountAsync(int id);
        Task<IEnumerable<Post>> SearchAsync(string searchTerm);
    }
}
