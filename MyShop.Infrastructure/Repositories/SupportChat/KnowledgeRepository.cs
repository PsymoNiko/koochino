using Dapper;
using MyShop.Application.Interfaces.SupportChat;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.SupportChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.SupportChat
{
    public class KnowledgeRepository : IKnowledgeRepository
    {
        private readonly IDbConnectionFactory _factory;
        public KnowledgeRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<KnowledgeCategory>> ListCategoriesAsync(int? siteId, bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<KnowledgeCategory>(@"
SELECT c.*, (SELECT COUNT(1) FROM koch.SupportKnowledgeArticles a WHERE a.CategoryId=c.Id) AS ArticleCount
FROM koch.SupportKnowledgeCategories c
WHERE (@SiteId IS NULL OR @SiteId=0 OR c.SiteId=@SiteId OR c.SiteId IS NULL)
  AND (@ActiveOnly=0 OR c.IsActive=1)
ORDER BY c.SortOrder, c.Name", new { SiteId = siteId, ActiveOnly = activeOnly ? 1 : 0 });
            return rows?.ToList() ?? new List<KnowledgeCategory>();
        }

        public async Task<int> UpsertCategoryAsync(KnowledgeCategory cat)
        {
            using var conn = _factory.CreateConnection();
            cat.Slug = EnsureSlug(cat.Slug, cat.Name);
            if (cat.Id <= 0)
            {
                return await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.SupportKnowledgeCategories (SiteId, Name, Slug, Description, SortOrder, IsActive)
OUTPUT INSERTED.Id VALUES (@SiteId, @Name, @Slug, @Description, @SortOrder, @IsActive)", cat);
            }
            await conn.ExecuteAsync(@"
UPDATE koch.SupportKnowledgeCategories
SET Name=@Name, Slug=@Slug, Description=@Description, SortOrder=@SortOrder, IsActive=@IsActive
WHERE Id=@Id", cat);
            return cat.Id;
        }

        public async Task DeleteCategoryAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SupportKnowledgeCategories WHERE Id=@Id", new { Id = id });
        }

        public async Task<List<KnowledgeArticle>> ListArticlesAsync(int? siteId, string status = null, int? categoryId = null, string type = null, string search = null, bool publicOnly = false, int take = 100)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<KnowledgeArticle>(@"
SELECT a.*, c.Name AS CategoryName
FROM koch.SupportKnowledgeArticles a
LEFT JOIN koch.SupportKnowledgeCategories c ON c.Id=a.CategoryId
WHERE (@SiteId IS NULL OR @SiteId=0 OR a.SiteId=@SiteId OR a.SiteId IS NULL)
  AND (@Status IS NULL OR @Status=N'' OR a.Status=@Status)
  AND (@CategoryId IS NULL OR a.CategoryId=@CategoryId)
  AND (@Type IS NULL OR @Type=N'' OR a.ArticleType=@Type)
  AND (@PublicOnly=0 OR (a.IsPublic=1 AND a.Status=N'Published'))
  AND (
        @Search IS NULL OR @Search=N'' OR
        a.Title LIKE N'%' + @Search + N'%' OR
        a.Summary LIKE N'%' + @Search + N'%' OR
        a.Content LIKE N'%' + @Search + N'%'
      )
ORDER BY a.UpdatedAt DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY",
                new { SiteId = siteId, Status = status, CategoryId = categoryId, Type = type, Search = search, PublicOnly = publicOnly ? 1 : 0, Take = take <= 0 ? 100 : take });
            var list = rows?.ToList() ?? new List<KnowledgeArticle>();
            foreach (var a in list)
                a.Tags = await LoadTagsAsync(conn, a.Id);
            return list;
        }

        /// <summary>
        /// Candidate set for retrieval-augmented answering: published, public, audience-matched articles that
        /// contain at least one query token. Final relevance scoring happens in memory against normalized text.
        /// If the token filter finds nothing (Persian character variants can defeat a raw LIKE) it falls back to
        /// a bounded recent set so scoring still gets a chance to match.
        /// </summary>
        public async Task<List<KnowledgeArticle>> SearchForRagAsync(int? siteId, IEnumerable<string> tokens, string audience, int take = 24)
        {
            var terms = (tokens ?? Enumerable.Empty<string>())
                .Select(t => (t ?? "").Trim())
                .Where(t => t.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();

            using var conn = _factory.CreateConnection();
            var rows = await QueryCandidatesAsync(conn, siteId, audience, terms, take <= 0 ? 24 : take);
            if (rows.Count == 0 && terms.Count > 0)
                rows = await QueryCandidatesAsync(conn, siteId, audience, new List<string>(), 50);

            foreach (var a in rows)
                a.Tags = await LoadTagsAsync(conn, a.Id);
            return rows;
        }

        static async Task<List<KnowledgeArticle>> QueryCandidatesAsync(
            System.Data.IDbConnection conn, int? siteId, string audience, List<string> terms, int take)
        {
            var p = new DynamicParameters();
            p.Add("SiteId", siteId);
            p.Add("Audience", string.IsNullOrWhiteSpace(audience) ? null : audience);
            p.Add("Take", take);

            var sql = new StringBuilder(@"
SELECT TOP (@Take) a.*, c.Name AS CategoryName
FROM koch.SupportKnowledgeArticles a
LEFT JOIN koch.SupportKnowledgeCategories c ON c.Id=a.CategoryId
WHERE a.Status=N'Published' AND a.IsPublic=1
  AND (@SiteId IS NULL OR @SiteId=0 OR a.SiteId=@SiteId OR a.SiteId IS NULL)
  AND (@Audience IS NULL OR a.Audience=N'Both' OR a.Audience=@Audience)");

            if (terms.Count > 0)
            {
                sql.Append(" AND (");
                for (var i = 0; i < terms.Count; i++)
                {
                    if (i > 0) sql.Append(" OR ");
                    sql.Append($@"a.Title LIKE @t{i} OR a.Summary LIKE @t{i} OR a.Content LIKE @t{i}
        OR EXISTS (SELECT 1 FROM koch.SupportKnowledgeArticleTags kat
                   INNER JOIN koch.SupportKnowledgeTags kt ON kt.Id=kat.TagId
                   WHERE kat.ArticleId=a.Id AND kt.Name LIKE @t{i})");
                    p.Add($"t{i}", "%" + terms[i] + "%");
                }
                sql.Append(")");
            }

            sql.Append(" ORDER BY a.UpdatedAt DESC");
            var rows = await conn.QueryAsync<KnowledgeArticle>(sql.ToString(), p);
            return rows?.ToList() ?? new List<KnowledgeArticle>();
        }

        public async Task<KnowledgeArticle> GetArticleBySourceKeyAsync(int? siteId, string sourceKey)
        {
            if (string.IsNullOrWhiteSpace(sourceKey)) return null;
            using var conn = _factory.CreateConnection();
            var a = await conn.QueryFirstOrDefaultAsync<KnowledgeArticle>(@"
SELECT TOP 1 a.*, c.Name AS CategoryName
FROM koch.SupportKnowledgeArticles a
LEFT JOIN koch.SupportKnowledgeCategories c ON c.Id=a.CategoryId
WHERE a.SourceKey=@SourceKey
  AND (@SiteId IS NULL OR a.SiteId=@SiteId OR a.SiteId IS NULL)
ORDER BY a.Id", new { SiteId = siteId, SourceKey = sourceKey });
            if (a != null) a.Tags = await LoadTagsAsync(conn, a.Id);
            return a;
        }

        public async Task<List<KnowledgeArticle>> ListBySourcePrefixAsync(int? siteId, string prefix, string status = null, int take = 100)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return new List<KnowledgeArticle>();
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<KnowledgeArticle>(@"
SELECT a.*, c.Name AS CategoryName
FROM koch.SupportKnowledgeArticles a
LEFT JOIN koch.SupportKnowledgeCategories c ON c.Id=a.CategoryId
WHERE a.SourceKey LIKE @Prefix + N'%'
  AND (@SiteId IS NULL OR @SiteId=0 OR a.SiteId=@SiteId OR a.SiteId IS NULL)
  AND (@Status IS NULL OR @Status=N'' OR a.Status=@Status)
ORDER BY a.UpdatedAt DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY",
                new { SiteId = siteId, Prefix = prefix, Status = status, Take = take <= 0 ? 100 : take });
            var list = rows?.ToList() ?? new List<KnowledgeArticle>();
            foreach (var a in list)
                a.Tags = await LoadTagsAsync(conn, a.Id);
            return list;
        }

        public async Task<KnowledgeArticle> GetArticleByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var a = await conn.QueryFirstOrDefaultAsync<KnowledgeArticle>(@"
SELECT a.*, c.Name AS CategoryName
FROM koch.SupportKnowledgeArticles a
LEFT JOIN koch.SupportKnowledgeCategories c ON c.Id=a.CategoryId
WHERE a.Id=@Id", new { Id = id });
            if (a != null) a.Tags = await LoadTagsAsync(conn, a.Id);
            return a;
        }

        public async Task<KnowledgeArticle> GetArticleBySlugAsync(int? siteId, string slug)
        {
            using var conn = _factory.CreateConnection();
            var a = await conn.QueryFirstOrDefaultAsync<KnowledgeArticle>(@"
SELECT a.*, c.Name AS CategoryName
FROM koch.SupportKnowledgeArticles a
LEFT JOIN koch.SupportKnowledgeCategories c ON c.Id=a.CategoryId
WHERE a.Slug=@Slug AND (@SiteId IS NULL OR a.SiteId=@SiteId OR a.SiteId IS NULL)",
                new { Slug = slug, SiteId = siteId });
            if (a != null) a.Tags = await LoadTagsAsync(conn, a.Id);
            return a;
        }

        public async Task<int> UpsertArticleAsync(KnowledgeArticle article, IEnumerable<string> tags)
        {
            using var conn = _factory.CreateConnection();
            article.Slug = EnsureSlug(article.Slug, article.Title);
            int id;
            if (article.Id <= 0)
            {
                id = await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.SupportKnowledgeArticles
(SiteId, CategoryId, Title, Slug, Summary, Content, ArticleType, Audience, SourceKey, SourceChatId, CoverImageUrl, Status, IsPublic, AuthorUserId, PublishedAt, SeoTitle, SeoDescription, UpdatedAt)
OUTPUT INSERTED.Id
VALUES (@SiteId, @CategoryId, @Title, @Slug, @Summary, @Content, @ArticleType, ISNULL(@Audience, N'Both'), @SourceKey, @SourceChatId, @CoverImageUrl, @Status, @IsPublic, @AuthorUserId, @PublishedAt, @SeoTitle, @SeoDescription, SYSDATETIME())", article);
            }
            else
            {
                await conn.ExecuteAsync(@"
UPDATE koch.SupportKnowledgeArticles SET
 CategoryId=@CategoryId, Title=@Title, Slug=@Slug, Summary=@Summary, Content=@Content, ArticleType=@ArticleType,
 Audience=ISNULL(@Audience, N'Both'), SourceKey=@SourceKey, SourceChatId=@SourceChatId,
 CoverImageUrl=@CoverImageUrl, Status=@Status, IsPublic=@IsPublic, PublishedAt=@PublishedAt,
 SeoTitle=@SeoTitle, SeoDescription=@SeoDescription, UpdatedAt=SYSDATETIME()
WHERE Id=@Id", article);
                id = article.Id;
            }
            await conn.ExecuteAsync("DELETE FROM koch.SupportKnowledgeArticleTags WHERE ArticleId=@Id", new { Id = id });
            foreach (var tagName in (tags ?? Enumerable.Empty<string>()).Select(t => (t ?? "").Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var tagId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT Id FROM koch.SupportKnowledgeTags WHERE SiteId=@SiteId AND Name=@Name",
                    new { article.SiteId, Name = tagName });
                if (tagId == null)
                {
                    tagId = await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.SupportKnowledgeTags (SiteId, Name) OUTPUT INSERTED.Id VALUES (@SiteId, @Name)",
                        new { article.SiteId, Name = tagName });
                }
                await conn.ExecuteAsync(
                    "INSERT INTO koch.SupportKnowledgeArticleTags (ArticleId, TagId) VALUES (@ArticleId, @TagId)",
                    new { ArticleId = id, TagId = tagId.Value });
            }
            return id;
        }

        public async Task SetArticleStatusAsync(int id, string status)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(@"
UPDATE koch.SupportKnowledgeArticles
SET Status=@Status,
    PublishedAt = CASE WHEN @Status=N'Published' THEN ISNULL(PublishedAt, SYSDATETIME()) ELSE PublishedAt END,
    UpdatedAt=SYSDATETIME()
WHERE Id=@Id", new { Id = id, Status = status });
        }

        public async Task DeleteArticleAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM koch.SupportKnowledgeArticles WHERE Id=@Id", new { Id = id });
        }

        public async Task<List<KnowledgeTag>> ListTagsAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<KnowledgeTag>(
                "SELECT * FROM koch.SupportKnowledgeTags WHERE (@SiteId IS NULL OR SiteId=@SiteId OR SiteId IS NULL) ORDER BY Name",
                new { SiteId = siteId });
            return rows?.ToList() ?? new List<KnowledgeTag>();
        }

        static async Task<List<string>> LoadTagsAsync(System.Data.IDbConnection conn, int articleId)
        {
            var tags = await conn.QueryAsync<string>(@"
SELECT t.Name FROM koch.SupportKnowledgeArticleTags at
INNER JOIN koch.SupportKnowledgeTags t ON t.Id=at.TagId
WHERE at.ArticleId=@Id ORDER BY t.Name", new { Id = articleId });
            return tags?.ToList() ?? new List<string>();
        }

        static string EnsureSlug(string slug, string title)
        {
            var s = string.IsNullOrWhiteSpace(slug) ? title : slug;
            s = (s ?? "item").Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", "-");
            s = Regex.Replace(s, @"[^a-z0-9\u0600-\u06FF\-]+", "");
            if (string.IsNullOrWhiteSpace(s)) s = "item-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return s.Length > 200 ? s.Substring(0, 200) : s;
        }
    }
}
