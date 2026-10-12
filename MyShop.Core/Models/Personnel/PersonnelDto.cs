using System;

namespace MyShop.Core.Models.Personnel
{
    public class PersonnelDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string FullName { get; set; }
        public string NationalCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string ResumeText { get; set; }
        public string ResumeFileUrl { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Address { get; set; }
        public string Specifications { get; set; }
        public TimeSpan? DefaultWorkStartTime { get; set; }
        public TimeSpan? DefaultWorkEndTime { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreatePersonnelDto
    {
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string FullName { get; set; }
        public string NationalCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string ResumeText { get; set; }
        public string ResumeFileUrl { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Address { get; set; }
        public string Specifications { get; set; }
        public TimeSpan? DefaultWorkStartTime { get; set; }
        public TimeSpan? DefaultWorkEndTime { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpdatePersonnelDto
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string FullName { get; set; }
        public string NationalCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string ResumeText { get; set; }
        public string ResumeFileUrl { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Address { get; set; }
        public string Specifications { get; set; }
        public TimeSpan? DefaultWorkStartTime { get; set; }
        public TimeSpan? DefaultWorkEndTime { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}
