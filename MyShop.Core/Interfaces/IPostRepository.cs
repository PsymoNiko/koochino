using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Core.Interfaces
{
    public interface IPostRepository
    {
        Task<Post> GetByIdAsync(int id);
        Task<IEnumerable<Post>> GetAllAsync(bool publishedOnly = true);
        Task<IEnumerable<Post>> GetLatestAsync(int count, bool publishedOnly = true);
        Task<int> CreateAsync(Post post, int authorId);
        Task<bool> UpdateAsync(Post post);
        Task<bool> DeleteAsync(int id);
        Task<bool> IncrementViewCountAsync(int id);
        Task<IEnumerable<Post>> SearchAsync(string searchTerm);
    }
}

