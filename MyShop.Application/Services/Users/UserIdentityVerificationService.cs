using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Users;
using MyShop.Core.Models;
using MyShop.Core.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Users
{
    public class UserIdentityVerificationService : IUserIdentityVerificationService
    {
        private readonly IUserIdentityVerificationRepository _repo;
        private readonly IUserService _userService;
        private static readonly Random Rnd = new();

        public UserIdentityVerificationService(IUserIdentityVerificationRepository repo, IUserService userService)
        {
            _repo = repo;
            _userService = userService;
        }

        public Task<UserIdentityVerification> GetLatestByUserIdAsync(int userId)
            => _repo.GetLatestByUserIdAsync(userId);

        public Task<UserIdentityVerification> GetByIdAsync(int verificationId)
            => _repo.GetByIdAsync(verificationId);

        public Task<List<UserIdentityVerification>> GetLatestAsync(int count, string statusFilter = null)
            => _repo.GetLatestAsync(count, statusFilter);

        public Task<UserIdentityVerification> GetRegistrationProgressAsync(int userId)
            => _repo.GetLatestByUserIdAsync(userId);

        public string GenerateVerificationCode()
        {
            lock (Rnd)
            {
                return Rnd.Next(100000, 999999).ToString();
            }
        }

        private string GenerateTrackingCode()
        {
            lock (Rnd)
            {
                return "KV" + DateTime.Now.ToString("yyMMdd") + Rnd.Next(1000, 9999);
            }
        }

        public async Task<UserIdentityVerification> SaveStep1Async(
            int userId,
            string nationalCode,
            string firstName,
            string lastName,
            string fatherName,
            DateTime? birthDate)
        {
            nationalCode = (nationalCode ?? "").Trim();
            firstName = (firstName ?? "").Trim();
            lastName = (lastName ?? "").Trim();
            fatherName = (fatherName ?? "").Trim();

            var profile = await _userService.GetProfileAsync(userId);

            // نام/نام‌خانوادگی/کد ملی روی حساب کاربری همگام می‌شود (بدون دست‌کاری Username/Email موجود)
            await _userService.UpdateProfileAsync(new UpdateUserProfileDto
            {
                UserId = userId,
                Username = profile?.Username,
                Email = profile?.Email,
                FirstName = firstName,
                LastName = lastName,
                NationalCode = nationalCode
            });

            var phoneNumber = profile?.PhoneNumber ?? "";
            var verificationId = await _repo.UpsertPersonalInfoAsync(userId, nationalCode, fatherName, birthDate, phoneNumber);

            return await _repo.GetByIdAsync(verificationId);
        }

        public async Task AttachStoreAsync(int verificationId, int storeId)
        {
            await _repo.AttachStoreAsync(verificationId, storeId);
        }

        public async Task<UserIdentityVerification> SubmitAsync(
            int userId,
            string nationalCode,
            string fatherName,
            DateTime? birthDate,
            string phoneNumber,
            string nationalCardImageUrl,
            string selfieImageUrl,
            string verificationCode)
        {
            var verification = new UserIdentityVerification
            {
                UserId = userId,
                NationalCode = (nationalCode ?? "").Trim(),
                FatherName = (fatherName ?? "").Trim(),
                BirthDate = birthDate,
                PhoneNumber = (phoneNumber ?? "").Trim(),
                NationalCardImageUrl = nationalCardImageUrl,
                SelfieImageUrl = selfieImageUrl,
                VerificationCode = verificationCode,
                TrackingCode = GenerateTrackingCode(),
                Status = "Pending"
            };

            var id = await _repo.CreateAsync(verification);
            verification.VerificationId = id;

            await _userService.SetIdentityVerificationStatusAsync(userId, "Pending");

            return verification;
        }

        public async Task<bool> ApproveAsync(int verificationId, int reviewedBy)
        {
            var result = await _repo.ApproveAsync(verificationId, reviewedBy);
            if (result)
            {
                var verification = await _repo.GetByIdAsync(verificationId);
                if (verification != null)
                    await _userService.SetIdentityVerificationStatusAsync(verification.UserId, "Approved");
            }
            return result;
        }

        public async Task<bool> RejectAsync(int verificationId, int reviewedBy, string rejectionReason)
        {
            var result = await _repo.RejectAsync(verificationId, reviewedBy, rejectionReason);
            if (result)
            {
                var verification = await _repo.GetByIdAsync(verificationId);
                if (verification != null)
                    await _userService.SetIdentityVerificationStatusAsync(verification.UserId, "Rejected");
            }
            return result;
        }
    }
}
