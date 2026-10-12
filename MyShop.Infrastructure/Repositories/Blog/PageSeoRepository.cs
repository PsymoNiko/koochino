using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace MyShop.Infrastructure.Repository
{
    public class PageSeoRepository : IPageSeoRepository
    {
        private readonly IDbConnectionFactory _factory;

        public PageSeoRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<PageSeo?> GetByPageKeyAsync(string pageKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey)) return null;
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<PageSeo>(
                "SELECT Id, PageKey, PageName, Title, MetaDescription, MetaKeywords, OgTitle, OgDescription, OgImage, CanonicalUrl, UpdatedAt FROM koch.PageSeo WHERE PageKey = @PageKey",
                new { PageKey = pageKey.Trim() },
                commandType: CommandType.Text);
        }

        public async Task<List<PageSeo>> GetAllAsync()
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<PageSeo>(
                "SELECT Id, PageKey, PageName, Title, MetaDescription, MetaKeywords, OgTitle, OgDescription, OgImage, CanonicalUrl, UpdatedAt FROM koch.PageSeo ORDER BY PageKey",
                commandType: CommandType.Text);
            return list?.ToList() ?? new List<PageSeo>();
        }

        public async Task<int> UpsertAsync(PageSeo entity)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.PageKey))
                return 0;
            using var conn = _factory.CreateConnection();
            entity.UpdatedAt = DateTime.UtcNow;
            var sql = @"
                MERGE koch.PageSeo AS t
                USING (SELECT @PageKey AS PageKey) AS s ON t.PageKey = s.PageKey
                WHEN MATCHED THEN
                    UPDATE SET PageName = @PageName, Title = @Title, MetaDescription = @MetaDescription, MetaKeywords = @MetaKeywords,
                        OgTitle = @OgTitle, OgDescription = @OgDescription, OgImage = @OgImage, CanonicalUrl = @CanonicalUrl, UpdatedAt = @UpdatedAt
                WHEN NOT MATCHED THEN
                    INSERT (PageKey, PageName, Title, MetaDescription, MetaKeywords, OgTitle, OgDescription, OgImage, CanonicalUrl, UpdatedAt)
                    VALUES (@PageKey, @PageName, @Title, @MetaDescription, @MetaKeywords, @OgTitle, @OgDescription, @OgImage, @CanonicalUrl, @UpdatedAt);
                SELECT Id FROM koch.PageSeo WHERE PageKey = @PageKey;";
            var id = await conn.ExecuteScalarAsync<int?>(sql, new
            {
                PageKey = entity.PageKey,
                PageName = entity.PageName,
                Title = entity.Title,
                MetaDescription = entity.MetaDescription ?? string.Empty,
                MetaKeywords = entity.MetaKeywords ?? "",
                OgTitle = entity.OgTitle ?? "",
                OgDescription = entity.OgDescription ?? "",
                OgImage = entity.OgImage ?? "",
                CanonicalUrl = entity.CanonicalUrl ?? "",
                UpdatedAt = entity.UpdatedAt
            }, commandType: CommandType.Text);
            return id ?? entity.Id;
        }
    }
}
