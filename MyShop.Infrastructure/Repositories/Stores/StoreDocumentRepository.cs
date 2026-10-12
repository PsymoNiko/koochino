using Dapper;
using MyShop.Application.Interfaces;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class StoreDocumentRepository : IStoreDocumentRepository
    {
        private readonly IDbConnectionFactory _factory;

        public StoreDocumentRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<StoreDocument>> GetByStoreIdAsync(int storeId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<StoreDocument>(
                "koch.spStoreDocuments_GetByStoreId",
                new { StoreId = storeId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreDocument>();
        }

        public async Task<StoreDocument> GetByIdAsync(int documentId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<StoreDocument>(
                "koch.spStoreDocuments_GetById",
                new { DocumentId = documentId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<StoreDocumentWithStoreDto>> GetLatestAsync(int count)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<StoreDocumentWithStoreDto>(
                "koch.spStoreDocuments_GetLatest",
                new { Count = count },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<StoreDocumentWithStoreDto>();
        }

        public async Task<int> CreateAsync(StoreDocument document)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("StoreId", document.StoreId);
            parameters.Add("Title", document.Title);
            parameters.Add("DocumentType", document.DocumentType);
            parameters.Add("ImageUrl", document.ImageUrl);
            parameters.Add("Description", document.Description);
            parameters.Add("ExpiryDate", document.ExpiryDate);
            parameters.Add("FileSize", document.FileSize);
            parameters.Add("MimeType", document.MimeType);
            parameters.Add("DisplayOrder", document.DisplayOrder);
            parameters.Add("DocumentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStoreDocuments_Create",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("DocumentId");
        }

        public async Task<bool> UpdateAsync(StoreDocument document)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("DocumentId", document.DocumentId);
            parameters.Add("Title", document.Title);
            parameters.Add("DocumentType", document.DocumentType);
            parameters.Add("ImageUrl", document.ImageUrl);
            parameters.Add("Description", document.Description);
            parameters.Add("ExpiryDate", document.ExpiryDate);
            parameters.Add("FileSize", document.FileSize);
            parameters.Add("MimeType", document.MimeType);
            parameters.Add("DisplayOrder", document.DisplayOrder);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStoreDocuments_Update",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int documentId)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("DocumentId", documentId);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStoreDocuments_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> ApproveAsync(int documentId, bool isApproved, int approvedBy)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("DocumentId", documentId);
            parameters.Add("IsApproved", isApproved);
            parameters.Add("ApprovedBy", approvedBy);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spStoreDocuments_Approve",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> SetAsProfileCoverAsync(int storeId, int documentId)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StoreId", storeId);
            p.Add("DocumentId", documentId);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spStoreDocuments_SetAsProfileCover", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }
}
