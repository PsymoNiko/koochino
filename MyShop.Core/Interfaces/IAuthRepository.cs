using MyShop.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace MyShop.Core.Interfaces
{
    public interface IAuthRepository
    {
        // ---------------------------
        // User Fetch
        // ---------------------------
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUserNameAsync(string userName);
        Task<User> GetUserByPhoneAsync(string phone, int RoleID);
        Task<User> FindByPhoneAsync(string phone);
        Task<User> GetUserByGoogleSubjectAsync(string googleSubject);
        Task<User> GetCustomerByEmailAsync(string email);
        Task<User> GetStoreOwnerByEmailAsync(string email);
        Task<int> RegisterGoogleCustomerAsync(string googleSubject, string email, string firstName, string lastName, string passwordHash, string passwordSalt, int? siteId);
        Task<int> RegisterGoogleStoreOwnerAsync(string googleSubject, string email, string firstName, string lastName, string passwordHash, string passwordSalt, int? siteId);
        Task LinkGoogleSubjectAsync(int userId, string googleSubject);
        Task EnsureUserHasRoleAsync(int userId, int roleId);
        Task RemoveUserRoleAsync(int userId, int roleId);
        // ---------------------------
        // Roles
        // ---------------------------
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        // ---------------------------
        // Password (Hash + Salt)
        // ---------------------------
        Task<(string Hash, string Salt)> GetPasswordInfoAsync(int userId);
        Task<bool> SetUserPasswordAsync(int userId, string passwordHash, string passwordSalt);

        // ---------------------------
        // Login
        // ---------------------------
        Task<User> LoginByIdAsync(int userId);

        // ---------------------------
        // Registration
        // ---------------------------
        Task<int> RegisterUserAsync(RegisterRequest request, string passwordHash, string passwordSalt);


        //بروزرسانی پروفایل

        Task<int> UpdateProfileAsync(UpdateUserProfileDto dto);
        // ---------------------------
        // گرفتن اطلاعات کاربر
        // ---------------------------

        Task<UserProfileVm> GetProfileAsync(int userId);


        // ---------------------------
        // Last Login
        // ---------------------------
        Task UpdateLastLoginAsync(int userId);

        // ---------------------------
        // Stores of a User
        // ---------------------------
        Task<IEnumerable<Store>> GetStoresForUserAsync(int userId);


        //Task<int> RegisterUserAsync(RegisterRequest request, string passwordHash, string passwordSalt);



    }
}
