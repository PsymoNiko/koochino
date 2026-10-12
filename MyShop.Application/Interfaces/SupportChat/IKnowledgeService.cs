using MyShop.Core.Models.SupportChat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.SupportChat
{
    public interface IKnowledgeRepository
    {
        Task<List<KnowledgeCategory>> ListCategoriesAsync(int? siteId, bool activeOnly = false);
        Task<int> UpsertCategoryAsync(KnowledgeCategory cat);
        Task DeleteCategoryAsync(int id);
        Task<List<KnowledgeArticle>> ListArticlesAsync(int? siteId, string status = null, int? categoryId = null, string type = null, string search = null, bool publicOnly = false, int take = 100);
        Task<List<KnowledgeArticle>> SearchForRagAsync(int? siteId, IEnumerable<string> tokens, string audience, int take = 24);
        Task<KnowledgeArticle> GetArticleByIdAsync(int id);
        Task<KnowledgeArticle> GetArticleBySlugAsync(int? siteId, string slug);
        Task<KnowledgeArticle> GetArticleBySourceKeyAsync(int? siteId, string sourceKey);
        Task<List<KnowledgeArticle>> ListBySourcePrefixAsync(int? siteId, string prefix, string status = null, int take = 100);
        Task<int> UpsertArticleAsync(KnowledgeArticle article, IEnumerable<string> tags);
        Task SetArticleStatusAsync(int id, string status);
        Task DeleteArticleAsync(int id);
        Task<List<KnowledgeTag>> ListTagsAsync(int? siteId);
    }

    public interface IKnowledgeService
    {
        Task<List<KnowledgeCategory>> ListCategoriesAsync(int? siteId, bool activeOnly = false);
        Task<int> SaveCategoryAsync(KnowledgeCategory cat);
        Task DeleteCategoryAsync(int id);
        Task<List<KnowledgeArticle>> ListArticlesAsync(int? siteId, string status = null, int? categoryId = null, string type = null, string search = null, bool publicOnly = false);
        Task<List<KnowledgeArticle>> SearchForRagAsync(int? siteId, IEnumerable<string> tokens, string audience, int take = 24);
        Task<KnowledgeArticle> GetAsync(int id);
        Task<KnowledgeArticle> GetBySlugAsync(int? siteId, string slug);
        Task<KnowledgeArticle> GetBySourceKeyAsync(int? siteId, string sourceKey);
        Task<List<KnowledgeArticle>> ListBySourcePrefixAsync(int? siteId, string prefix, string status = null);
        Task<int> SaveArticleAsync(KnowledgeArticle article, IEnumerable<string> tags, int? authorUserId = null);
        Task PublishAsync(int id);
        Task ArchiveAsync(int id);
        Task DeleteArticleAsync(int id);
        Task<List<KnowledgeTag>> ListTagsAsync(int? siteId);
        Task<List<KnowledgeArticle>> SearchPublicAsync(int? siteId, string query, int take = 20);
    }
}
