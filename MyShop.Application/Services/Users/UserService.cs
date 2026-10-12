using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }


        public Task<int> CountAsync()
        {
            return _repo.CountAsync();
        }

        public Task<List<UserDto>> GetUsersAsync(string search)
        {
            return _repo.GetUsersAsync(search);
        }

        public Task<List<UserDto>> SearchUsersAsync(string query, int limit = 20)
            => _repo.SearchUsersAsync(query, limit);

        public Task<UserDto> GetUserBriefAsync(int userId)
            => _repo.GetUserBriefAsync(userId);

        public Task<bool> DeleteAsync(int userId)
        {
            return _repo.DeleteAsync(userId);
        }

        public async Task<List<User>> LatestAsync(int count)
        {
            var result = await _repo.LatestAsync(count);
            return result.ToList();
        }

        public async Task<List<User>> ListedAsync(int count)
        {
            return (List<User>)await _repo.LatestAsync(count);
        }

        public Task<UserProfileVm> GetProfileAsync(int userId)
            => _repo.GetProfileAsync(userId);


        public Task<int> UpdateProfileAsync(UpdateUserProfileDto dto)
            => _repo.UpdateProfileAsync(dto);

        public Task<bool> UpdateUserDocumentUrlAsync(int userId, string documentType, string url)
            => _repo.UpdateUserDocumentUrlAsync(userId, documentType, url);

        public Task<bool> ToggleStatusAsync(int userId, bool isActive)
            => _repo.ToggleStatusAsync(userId, isActive);

        public Task<bool> SetPhoneVerifiedAsync(int userId, bool isVerified = true)
            => _repo.SetPhoneVerifiedAsync(userId, isVerified);

        public Task<int?> FindUserIdByPhoneAsync(string phoneNumber)
            => _repo.FindUserIdByPhoneAsync(phoneNumber);

        public Task<bool> IsPhoneVerifiedAsync(int userId)
            => _repo.IsPhoneVerifiedAsync(userId);

        public Task<bool> IsPhoneVerifiedByAnotherUserAsync(string phoneNumber, int? excludeUserId = null)
            => _repo.IsPhoneVerifiedByAnotherUserAsync(phoneNumber, excludeUserId);

        public Task<string> GetIdentityVerificationStatusAsync(int userId)
            => _repo.GetIdentityVerificationStatusAsync(userId);

        public Task<bool> SetIdentityVerificationStatusAsync(int userId, string status)
            => _repo.SetIdentityVerificationStatusAsync(userId, status);
    }
}
