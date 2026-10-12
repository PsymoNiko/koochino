using Dapper;
using MyShop.Application.Interfaces.Users;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Users;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Users
{
    public class UserIdentityDocumentRepository : IUserIdentityDocumentRepository
    {
        private readonly IDbConnectionFactory _factory;

        public UserIdentityDocumentRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<UserIdentityDocument> GetLatestByUserIdAndTypeAsync(int userId, string documentType)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserIdentityDocument>(
                "koch.spUserIdentityDocuments_GetLatestByUserIdAndType",
                new { UserId = userId, DocumentType = documentType },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<UserIdentityDocument>> GetAllLatestByUserIdAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<UserIdentityDocument>(
                "koch.spUserIdentityDocuments_GetAllLatestByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<UserIdentityDocument>();
        }

        public async Task<UserIdentityDocument> GetByIdAsync(int documentId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserIdentityDocument>(
                "koch.spUserIdentityDocuments_GetById",
                new { DocumentId = documentId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateAsync(UserIdentityDocument document)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", document.UserId);
            parameters.Add("VerificationId", document.VerificationId);
            parameters.Add("DocumentType", document.DocumentType);
            parameters.Add("ImageUrl", document.ImageUrl);
            parameters.Add("VerificationCode", document.VerificationCode);
            parameters.Add("DocumentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserIdentityDocuments_Create",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("DocumentId");
        }

        public async Task<bool> ApproveAsync(int documentId, int reviewedBy)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("DocumentId", documentId);
            parameters.Add("ReviewedBy", reviewedBy);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserIdentityDocuments_Approve",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> RejectAsync(int documentId, int reviewedBy, string rejectionReason)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("DocumentId", documentId);
            parameters.Add("ReviewedBy", reviewedBy);
            parameters.Add("RejectionReason", rejectionReason);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserIdentityDocuments_Reject",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<List<UserIdentityDocument>> GetLatestForAdminAsync(int count, string statusFilter = null)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<UserIdentityDocument>(
                "koch.spUserIdentityDocuments_GetLatestForAdmin",
                new { Count = count, StatusFilter = statusFilter },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<UserIdentityDocument>();
        }
    }
}
