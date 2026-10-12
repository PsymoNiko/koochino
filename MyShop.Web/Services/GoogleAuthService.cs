using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using System;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public interface IGoogleAuthService
    {
        Task<(bool ok, string error, User user)> FindOrCreateCustomerAsync(
            string googleSubject, string email, string firstName, string lastName);

        Task<(bool ok, string error, User user)> FindOrCreateStoreOwnerAsync(
            string googleSubject, string email, string firstName, string lastName);
    }

    public class GoogleAuthService : IGoogleAuthService
    {
        private const int RoleCustomer = 3;
        private const int RoleStoreOwner = 2;

        private readonly IAuthRepository _repo;
        private readonly ICurrentTenant _tenant;

        public GoogleAuthService(IAuthRepository repo, ICurrentTenant tenant)
        {
            _repo = repo;
            _tenant = tenant;
        }

        public Task<(bool ok, string error, User user)> FindOrCreateCustomerAsync(
            string googleSubject, string email, string firstName, string lastName)
            => FindOrCreateAsync(googleSubject, email, firstName, lastName, RoleCustomer);

        public Task<(bool ok, string error, User user)> FindOrCreateStoreOwnerAsync(
            string googleSubject, string email, string firstName, string lastName)
            => FindOrCreateAsync(googleSubject, email, firstName, lastName, RoleStoreOwner);

        private async Task<(bool ok, string error, User user)> FindOrCreateAsync(
            string googleSubject, string email, string firstName, string lastName, int roleId)
        {
            if (string.IsNullOrWhiteSpace(googleSubject))
                return (false, "شناسه Google نامعتبر است.", null);
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return (false, "ایمیل Google در دسترس نیست.", null);

            try
            {
                var byGoogle = await _repo.GetUserByGoogleSubjectAsync(googleSubject);
                if (byGoogle != null)
                {
                    if (!byGoogle.IsActive)
                        return (false, "حساب شما فعال نشده است.", null);
                    await _repo.EnsureUserHasRoleAsync(byGoogle.Id, roleId);
                    byGoogle.RoleId = roleId;
                    return (true, null, byGoogle);
                }

                User byEmail = roleId == RoleStoreOwner
                    ? await _repo.GetStoreOwnerByEmailAsync(email)
                    : await _repo.GetCustomerByEmailAsync(email);

                if (byEmail != null)
                {
                    if (!byEmail.IsActive)
                        return (false, "حساب شما فعال نشده است.", null);
                    await _repo.LinkGoogleSubjectAsync(byEmail.Id, googleSubject);
                    await _repo.EnsureUserHasRoleAsync(byEmail.Id, roleId);
                    byEmail.GoogleSubject = googleSubject;
                    byEmail.RoleId = roleId;
                    return (true, null, byEmail);
                }

                // اگر کاربر با این GoogleSubject نیست ولی با ایمیل نقش دیگر وجود دارد،
                // همان کاربر را لینک می‌کنیم و نقش فعلی را اضافه می‌کنیم (یکتایی GoogleSubject).
                User otherRole = roleId == RoleStoreOwner
                    ? await _repo.GetCustomerByEmailAsync(email)
                    : await _repo.GetStoreOwnerByEmailAsync(email);
                if (otherRole != null)
                {
                    if (!otherRole.IsActive)
                        return (false, "حساب شما فعال نشده است.", null);
                    await _repo.LinkGoogleSubjectAsync(otherRole.Id, googleSubject);
                    await _repo.EnsureUserHasRoleAsync(otherRole.Id, roleId);
                    otherRole.GoogleSubject = googleSubject;
                    otherRole.RoleId = roleId;
                    return (true, null, otherRole);
                }

                var (hash, salt) = PasswordHasher.HashPassword(Guid.NewGuid().ToString("N") + "Aa1!");
                int? siteId = _tenant?.HasSite == true ? _tenant.SiteId : null;
                var newId = roleId == RoleStoreOwner
                    ? await _repo.RegisterGoogleStoreOwnerAsync(googleSubject, email, firstName ?? "", lastName ?? "", hash, salt, siteId)
                    : await _repo.RegisterGoogleCustomerAsync(googleSubject, email, firstName ?? "", lastName ?? "", hash, salt, siteId);
                if (newId <= 0)
                    return (false, "خطا در ایجاد حساب Google.", null);

                var created = await _repo.LoginByIdAsync(newId)
                              ?? await _repo.GetUserByGoogleSubjectAsync(googleSubject);
                if (created == null)
                    return (false, "حساب ایجاد شد اما بازیابی کاربر ناموفق بود.", null);

                created.RoleId = roleId;
                return (true, null, created);
            }
            catch (Exception ex)
            {
                return (false, "خطا در ورود Google: " + ex.Message, null);
            }
        }
    }
}
