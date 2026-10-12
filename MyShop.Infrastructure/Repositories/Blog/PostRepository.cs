using Dapper;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class PostRepository : IPostRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PostRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Post> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Post>(
                "koch.spPosts_GetById",
                new { PostId = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Post>> GetAllAsync(bool publishedOnly = true)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Post>(
                "koch.spPosts_GetAll",
                new { PublishedOnly = publishedOnly },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Post>> GetLatestAsync(int count, bool publishedOnly = true)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Post>(
                "koch.spPosts_GetLatest",
                new { Count = count, PublishedOnly = publishedOnly },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CreateAsync(Post post, int authorId)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            var parameters = new DynamicParameters();
            parameters.Add("Title", post.Title);
            parameters.Add("Content", post.Content);
            parameters.Add("Summary", post.Summary ?? string.Empty);
            parameters.Add("AuthorId", authorId);
            parameters.Add("IsPublished", post.IsPublished);
            parameters.Add("ImageUrl", post.ImageUrl);
            parameters.Add("Tags", post.Tags);
            parameters.Add("PostId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spPosts_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("PostId");
        }

        public async Task<bool> UpdateAsync(Post post)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            var parameters = new DynamicParameters();
            parameters.Add("PostId", post.Id);
            parameters.Add("Title", post.Title);
            parameters.Add("Content", post.Content);
            parameters.Add("Summary", post.Summary ?? string.Empty);
            parameters.Add("IsPublished", post.IsPublished);
            parameters.Add("ImageUrl", post.ImageUrl);
            parameters.Add("Tags", post.Tags);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await conn.ExecuteAsync(
                "koch.spPosts_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            var parameters = new DynamicParameters();
            parameters.Add("PostId", id);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await conn.ExecuteAsync(
                "koch.spPosts_Delete",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> IncrementViewCountAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var affected = await conn.ExecuteAsync(
                "koch.spPosts_IncrementViewCount",
                new { PostId = id },
                commandType: CommandType.StoredProcedure
            );
            return affected > 0;
        }

        public async Task<IEnumerable<Post>> SearchAsync(string searchTerm)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Post>(
                "koch.spPosts_Search",
                new { SearchTerm = searchTerm ?? string.Empty },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}

