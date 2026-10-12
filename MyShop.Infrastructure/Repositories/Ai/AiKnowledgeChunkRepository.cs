using Dapper;
using MyShop.Application.Interfaces.Ai;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Ai;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Ai
{
    public class AiKnowledgeChunkRepository : IAiKnowledgeChunkRepository
    {
        private readonly IDbConnectionFactory _factory;
        public AiKnowledgeChunkRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task ReplaceArticleChunksAsync(int articleId, IReadOnlyList<AiKnowledgeChunk> chunks)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            await conn.ExecuteAsync(
                "DELETE FROM koch.AiKnowledgeChunks WHERE ArticleId=@ArticleId",
                new { ArticleId = articleId }, tx);
            foreach (var chunk in chunks ?? Array.Empty<AiKnowledgeChunk>())
            {
                await conn.ExecuteAsync(@"
INSERT INTO koch.AiKnowledgeChunks
    (ArticleId, SiteId, ChunkIndex, Text, ContentHash, SourceHash, Embedding, Dimensions, Model, UpdatedAt)
VALUES
    (@ArticleId, @SiteId, @ChunkIndex, @Text, @ContentHash, @SourceHash, @Embedding, @Dimensions, @Model, SYSDATETIME())",
                    chunk, tx);
            }
            tx.Commit();
        }

        public async Task DeleteByArticleAsync(int articleId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM koch.AiKnowledgeChunks WHERE ArticleId=@ArticleId",
                new { ArticleId = articleId });
        }

        public async Task<List<AiKnowledgeChunk>> GetByArticleAsync(int articleId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<AiKnowledgeChunk>(
                "SELECT * FROM koch.AiKnowledgeChunks WHERE ArticleId=@ArticleId ORDER BY ChunkIndex",
                new { ArticleId = articleId });
            return rows?.ToList() ?? new List<AiKnowledgeChunk>();
        }

        public async Task<List<AiKnowledgeChunk>> ListPublishedAsync(int? siteId, string audience, string model)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<AiKnowledgeChunk>(@"
SELECT c.Id, c.ArticleId, c.SiteId, c.ChunkIndex, c.Text, c.ContentHash, c.SourceHash,
       c.Embedding, c.Dimensions, c.Model, c.UpdatedAt,
       a.Title, a.Slug, a.Audience, a.ArticleType
FROM koch.AiKnowledgeChunks c
INNER JOIN koch.SupportKnowledgeArticles a ON a.Id=c.ArticleId
WHERE a.Status=N'Published' AND a.IsPublic=1
  AND c.Embedding IS NOT NULL
  AND (@Model IS NULL OR @Model=N'' OR c.Model=@Model)
  AND (@SiteId IS NULL OR @SiteId=0 OR a.SiteId=@SiteId OR a.SiteId IS NULL)
  AND (@Audience IS NULL OR @Audience=N'' OR a.Audience=N'Both' OR a.Audience=@Audience)",
                new { SiteId = siteId, Audience = audience, Model = model });
            return rows?.ToList() ?? new List<AiKnowledgeChunk>();
        }

        public async Task<List<int>> ListStaleArticleIdsAsync(int? siteId, int take)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<int>(@"
SELECT TOP (@Take) a.Id
FROM koch.SupportKnowledgeArticles a
WHERE a.Status=N'Published' AND a.IsPublic=1
  AND (@SiteId IS NULL OR @SiteId=0 OR a.SiteId=@SiteId OR a.SiteId IS NULL)
  AND EXISTS (
        SELECT 1 FROM koch.SupportSettings s
        WHERE s.AiEmbeddingsEnabled=1
          AND (
                (a.SiteId IS NOT NULL AND s.SiteId=a.SiteId)
             OR (a.SiteId IS NULL AND (@SiteId IS NULL OR @SiteId=0 OR s.SiteId=@SiteId))
          )
      )
  AND (
        NOT EXISTS (SELECT 1 FROM koch.AiKnowledgeChunks c WHERE c.ArticleId=a.Id)
     OR EXISTS (
            SELECT 1 FROM koch.AiKnowledgeChunks c
            WHERE c.ArticleId=a.Id
            GROUP BY c.ArticleId
            HAVING MIN(c.UpdatedAt) < a.UpdatedAt
        )
      )
ORDER BY a.UpdatedAt DESC",
                new { SiteId = siteId, Take = take <= 0 ? 8 : take });
            return rows?.ToList() ?? new List<int>();
        }

        public async Task<int> CountAsync(int? siteId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM koch.AiKnowledgeChunks c
INNER JOIN koch.SupportKnowledgeArticles a ON a.Id=c.ArticleId
WHERE (@SiteId IS NULL OR @SiteId=0 OR a.SiteId=@SiteId OR a.SiteId IS NULL)",
                new { SiteId = siteId });
        }
    }
}
