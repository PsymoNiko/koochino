using Dapper;
using MyShop.Application.Interfaces.Users;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Users
{
    public class UserIdentityVerificationRepository : IUserIdentityVerificationRepository
    {
        private readonly IDbConnectionFactory _factory;

        public UserIdentityVerificationRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<UserIdentityVerification> GetLatestByUserIdAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserIdentityVerification>(
                "koch.spUserIdentityVerification_GetLatestByUserId",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<UserIdentityVerification> GetByIdAsync(int verificationId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<UserIdentityVerification>(
                "koch.spUserIdentityVerification_GetById",
                new { VerificationId = verificationId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<UserIdentityVerification>> GetLatestAsync(int count, string statusFilter = null)
        {
            using var conn = _factory.CreateConnection();
            var list = await conn.QueryAsync<UserIdentityVerification>(
                "koch.spUserIdentityVerification_GetLatest",
                new { Count = count, StatusFilter = statusFilter },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<UserIdentityVerification>();
        }

        public async Task<int> CreateAsync(UserIdentityVerification verification)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", verification.UserId);
            parameters.Add("NationalCode", verification.NationalCode);
            parameters.Add("FatherName", verification.FatherName);
            parameters.Add("BirthDate", verification.BirthDate);
            parameters.Add("PhoneNumber", verification.PhoneNumber);
            parameters.Add("NationalCardImageUrl", verification.NationalCardImageUrl);
            parameters.Add("SelfieImageUrl", verification.SelfieImageUrl);
            parameters.Add("VerificationCode", verification.VerificationCode);
            parameters.Add("TrackingCode", verification.TrackingCode);
            parameters.Add("VerificationId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserIdentityVerification_Create",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("VerificationId");
        }

        public async Task<bool> ApproveAsync(int verificationId, int reviewedBy)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("VerificationId", verificationId);
            parameters.Add("ReviewedBy", reviewedBy);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserIdentityVerification_Approve",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<bool> RejectAsync(int verificationId, int reviewedBy, string rejectionReason)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("VerificationId", verificationId);
            parameters.Add("ReviewedBy", reviewedBy);
            parameters.Add("RejectionReason", rejectionReason);
            parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserIdentityVerification_Reject",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("Result") == 1;
        }

        public async Task<int> UpsertPersonalInfoAsync(int userId, string nationalCode, string fatherName, DateTime? birthDate, string phoneNumber)
        {
            using var conn = _factory.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);
            parameters.Add("NationalCode", nationalCode);
            parameters.Add("FatherName", fatherName);
            parameters.Add("BirthDate", birthDate);
            parameters.Add("PhoneNumber", phoneNumber);
            parameters.Add("VerificationId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "koch.spUserIdentityVerification_UpsertProgress",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("VerificationId");
        }

        public async Task AttachStoreAsync(int verificationId, int storeId)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spUserIdentityVerification_AttachStore",
                new { VerificationId = verificationId, StoreId = storeId },
                commandType: CommandType.StoredProcedure);
        }
    }
}
