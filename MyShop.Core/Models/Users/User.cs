using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace  MyShop.Core.Models  
{
   
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        //public string Role { get; set; }
        public string PasswordSalt { get; set; } = default!;
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        /// <summary>آیا شماره موبایل از طریق OTP/بله تأیید شده است</summary>
        public bool IsPhoneVerified { get; set; }
        /// <summary>شناسه یکتای Google (sub) برای ورود OAuth</summary>
        public string GoogleSubject { get; set; }
        public DateTime CreatedAt { get; set; }
        public string NationalCode { get; set; }
        public int UserStatID { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string UserStatTitle { get; set; }
        public DateTime? BirthDate { get; set; }
        /// <summary>سایت (برند) کاربر</summary>
        public int? SiteId { get; set; }
    }

}
