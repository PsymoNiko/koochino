using System;                 // ← این مهم است
using System.Collections.Generic;

namespace MyShop.Core.Models.Dto
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string NationalCode { get; set; }
        public string RoleName { get; set; }
        public List<string> Roles { get; set; } = new List<string>(); // لیست نقش‌ها
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int? SiteId { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
    }
}
