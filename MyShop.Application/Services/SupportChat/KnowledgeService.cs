using MyShop.Application.Interfaces.Ai;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.SupportChat
{
    public class KnowledgeService : IKnowledgeService
    {
        private readonly IKnowledgeRepository _repo;
        private readonly IAiEmbeddingIndexer _indexer;

        public KnowledgeService(IKnowledgeRepository repo, IAiEmbeddingIndexer indexer = null)
        {
            _repo = repo;
            _indexer = indexer;
        }

        public Task<List<KnowledgeCategory>> ListCategoriesAsync(int? siteId, bool activeOnly = false) =>
            _repo.ListCategoriesAsync(siteId, activeOnly);

        public Task<int> SaveCategoryAsync(KnowledgeCategory cat)
        {
            if (cat == null || string.IsNullOrWhiteSpace(cat.Name))
                throw new InvalidOperationException("نام دسته الزامی است.");
            cat.Name = cat.Name.Trim();
            return _repo.UpsertCategoryAsync(cat);
        }

        public Task DeleteCategoryAsync(int id) => _repo.DeleteCategoryAsync(id);

        public Task<List<KnowledgeArticle>> ListArticlesAsync(int? siteId, string status = null, int? categoryId = null, string type = null, string search = null, bool publicOnly = false) =>
            _repo.ListArticlesAsync(siteId, status, categoryId, type, search, publicOnly);

        public Task<List<KnowledgeArticle>> SearchForRagAsync(int? siteId, IEnumerable<string> tokens, string audience, int take = 24) =>
            _repo.SearchForRagAsync(siteId, tokens, audience, take);

        public Task<KnowledgeArticle> GetAsync(int id) => _repo.GetArticleByIdAsync(id);
        public Task<KnowledgeArticle> GetBySlugAsync(int? siteId, string slug) => _repo.GetArticleBySlugAsync(siteId, slug);
        public Task<KnowledgeArticle> GetBySourceKeyAsync(int? siteId, string sourceKey) => _repo.GetArticleBySourceKeyAsync(siteId, sourceKey);

        public Task<List<KnowledgeArticle>> ListBySourcePrefixAsync(int? siteId, string prefix, string status = null) =>
            _repo.ListBySourcePrefixAsync(siteId, prefix, status);

        public async Task<int> SaveArticleAsync(KnowledgeArticle article, IEnumerable<string> tags, int? authorUserId = null)
        {
            if (article == null || string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Content))
                throw new InvalidOperationException("عنوان و محتوا الزامی است.");
            article.Title = article.Title.Trim();
            if (article.Id <= 0 && authorUserId.HasValue)
                article.AuthorUserId = authorUserId;
            if (string.Equals(article.Status, KnowledgeArticleStatuses.Published, StringComparison.OrdinalIgnoreCase) && article.PublishedAt == null)
                article.PublishedAt = DateTime.Now;
            var id = await _repo.UpsertArticleAsync(article, tags);
            await TryIndexAsync(id);
            return id;
        }

        public async Task PublishAsync(int id)
        {
            await _repo.SetArticleStatusAsync(id, KnowledgeArticleStatuses.Published);
            await TryIndexAsync(id);
        }

        public async Task ArchiveAsync(int id)
        {
            await _repo.SetArticleStatusAsync(id, KnowledgeArticleStatuses.Archived);
            await TryIndexAsync(id);
        }

        public async Task DeleteArticleAsync(int id)
        {
            await _repo.DeleteArticleAsync(id);
            if (_indexer == null) return;
            try { await _indexer.IndexArticleAsync(id); }
            catch { }
        }
        public Task<List<KnowledgeTag>> ListTagsAsync(int? siteId) => _repo.ListTagsAsync(siteId);

        public Task<List<KnowledgeArticle>> SearchPublicAsync(int? siteId, string query, int take = 20) =>
            _repo.ListArticlesAsync(siteId, KnowledgeArticleStatuses.Published, null, null, query, publicOnly: true, take: take);

        async Task TryIndexAsync(int id)
        {
            if (_indexer == null || id <= 0) return;
            try { await _indexer.IndexArticleAsync(id); }
            catch { }
        }
    }
}
