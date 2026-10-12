using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Interfaces   // اگر در Core است، همین namespace را مطابق پروژه‌ات تنظیم کن
{
    public interface IUserRepository
    {
        Task<int> CountAsync();
        Task<List<User>> LatestAsync(int count);
        Task<List<UserDto>> GetUsersAsync(string search);
        Task<List<UserDto>> SearchUsersAsync(string query, int limit = 20);
        Task<UserDto> GetUserBriefAsync(int userId);
        Task<bool> DeleteAsync(int userId);
       // Task<UpdateUserProfileDto> GetProfileAsync(int userId);
       //Task<UpdateUserProfileDto> GetProfileAsync(int userId);
        Task<UserProfileVm> GetProfileAsync(int userId);
        Task<int> UpdateProfileAsync(UpdateUserProfileDto dto);
        Task<bool> UpdateUserDocumentUrlAsync(int userId, string documentType, string url);
        Task<bool> ToggleStatusAsync(int userId, bool isActive);
        Task<bool> SetPhoneVerifiedAsync(int userId, bool isVerified = true);
        Task<int?> FindUserIdByPhoneAsync(string phoneNumber);
        Task<bool> IsPhoneVerifiedAsync(int userId);
        Task<bool> IsPhoneVerifiedByAnotherUserAsync(string phoneNumber, int? excludeUserId = null);
        Task<string> GetIdentityVerificationStatusAsync(int userId);
        Task<bool> SetIdentityVerificationStatusAsync(int userId, string status);
    }

}
