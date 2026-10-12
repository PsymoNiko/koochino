using MyShop.Core.Models;
using MyShop.Core.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces
{
    public interface IUserService
    {
        Task<int> CountAsync();
        Task<List<UserDto>> GetUsersAsync(string search);
        Task<List<UserDto>> SearchUsersAsync(string query, int limit = 20);
        Task<UserDto> GetUserBriefAsync(int userId);
        Task<bool> DeleteAsync(int userId);
        Task<List<User>> LatestAsync(int count);

        // اگر نیاز به ListedAsync دارید
        Task<List<User>> ListedAsync(int count);

        //بروزرسانی پروفایل
       // Task<UpdateUserProfileDto> GetProfileAsync(int userId);
        Task<int> UpdateProfileAsync(UpdateUserProfileDto dto);

        Task<UserProfileVm> GetProfileAsync(int userId);
        Task<bool> UpdateUserDocumentUrlAsync(int userId, string documentType, string url);
        Task<bool> ToggleStatusAsync(int userId, bool isActive);
        Task<bool> SetPhoneVerifiedAsync(int userId, bool isVerified = true);
        Task<int?> FindUserIdByPhoneAsync(string phoneNumber);
        Task<bool> IsPhoneVerifiedAsync(int userId);
        Task<bool> IsPhoneVerifiedByAnotherUserAsync(string phoneNumber, int? excludeUserId = null);

        /// <summary>وضعیت آخرین احراز هویت مغازه‌دار (Pending / Approved / Rejected / null)</summary>
        Task<string> GetIdentityVerificationStatusAsync(int userId);
        Task<bool> SetIdentityVerificationStatusAsync(int userId, string status);
    }
}
