using System;

namespace MyShop.Core.Models.Crm
{
    public class CrmContactDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? UserId { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string JobTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string Source { get; set; }
        public string Tags { get; set; }
        public string Notes { get; set; }
        public int? AssignedTo { get; set; }
        public string Status { get; set; }
        public byte? Rating { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }

    public class CreateCrmContactDto
    {
        public int? UserId { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string JobTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string Source { get; set; }
        public string Tags { get; set; }
        public string Notes { get; set; }
        public int? AssignedTo { get; set; }
        public string Status { get; set; }
        public byte? Rating { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateCrmContactDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string JobTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string Source { get; set; }
        public string Tags { get; set; }
        public string Notes { get; set; }
        public int? AssignedTo { get; set; }
        public string Status { get; set; }
        public byte? Rating { get; set; }
        public bool IsActive { get; set; }
    }
}
