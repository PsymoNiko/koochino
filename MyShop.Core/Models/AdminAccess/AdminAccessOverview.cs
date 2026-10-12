using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.AdminAccess
{
    /// <summary>نمای کلی ادمین: پروفایل دسترسی + سایت عضویت + سایت‌های مجاز پنل.</summary>
    public class AdminAccessOverview
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public int? MembershipSiteId { get; set; }
        public string MembershipSiteCode { get; set; }
        public string MembershipSiteName { get; set; }
        public int? ProfileId { get; set; }
        public string ProfileName { get; set; }
        public bool IsFullAccess { get; set; }
        public string AdminSiteNames { get; set; }
        public string AdminSiteIdsCsv { get; set; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
                ? (Username ?? PhoneNumber ?? UserId.ToString())
                : $"{FirstName} {LastName}".Trim();

        public List<int> ParseAdminSiteIds()
        {
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(AdminSiteIdsCsv)) return list;
            foreach (var part in AdminSiteIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part.Trim(), out var id)) list.Add(id);
            return list;
        }
    }
}
