using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Academy
{
    public class AcademyStudentDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string NationalCode { get; set; }
        public string Email { get; set; }
        public int? GuardianContactId { get; set; }
        public string GuardianName { get; set; }
    }

    public class CreateAcademyStudentDto
    {
        public int? SiteId { get; set; }
        /// <summary>اگر ست شود مستقیماً استفاده می‌شود؛ در غیر این صورت از PhoneNumber پیدا/ایجاد می‌شود.</summary>
        public int? UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalCode { get; set; }
        public string Status { get; set; } = "Active";
        public string Notes { get; set; }
        public int? GuardianContactId { get; set; }
    }

    public class UpdateAcademyStudentDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Status { get; set; } = "Active";
        public string Notes { get; set; }
        public bool MustChangePassword { get; set; }
        public int? GuardianContactId { get; set; }
    }

    public class AcademyInstructorDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int PersonnelId { get; set; }
        public int UserId { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string PersonnelFullName { get; set; }
        public string PersonnelPhone { get; set; }
        public string PersonnelNationalCode { get; set; }
        public string UserPhone { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public List<int> CategoryIds { get; set; } = new();
    }

    public class CreateAcademyInstructorDto
    {
        public int? SiteId { get; set; }
        public int PersonnelId { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> CategoryIds { get; set; } = new();
    }

    public class UpdateAcademyInstructorDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; }
        public List<int> CategoryIds { get; set; } = new();
    }

    public class AcademyActionResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public int Id { get; set; }

        public string RedirectUrl { get; set; }

        public string Message { get; set; }

        public static AcademyActionResult Success(int id, string redirectUrl = null, string message = null) =>
            new() { Ok = true, Id = id, RedirectUrl = redirectUrl, Message = message };
        public static AcademyActionResult Fail(string error) => new() { Ok = false, Error = error };
    }
}
