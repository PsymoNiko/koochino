using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShop.Core.Models
{
    public class UserProfileVm
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalCode { get; set; }
        public DateTime? BirthDate { get; set; }
        /// <summary>آیا شماره موبایل تأیید شده است</summary>
        public bool IsPhoneVerified { get; set; }
    /// <summary>آدرس عکس پروفایل</summary>
    public string ProfilePictureUrl { get; set; }
    /// <summary>آدرس عکس روی کارت ملی</summary>
    public string NationalIdFrontImageUrl { get; set; }
    /// <summary>آدرس عکس پشت کارت ملی</summary>
    public string NationalIdBackImageUrl { get; set; }
    }
}
