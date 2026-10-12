using System;

namespace MyShop.Core.Models.Tickets
{
    public class TicketDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string TicketNo { get; set; }
        public int? UserId { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int? ClosedBy { get; set; }
        public string Resolution { get; set; }
        public int? OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string StoreName { get; set; }
        public long? FinalAmount { get; set; }
        public byte? OrderStatus { get; set; }
    }

    public class CreateTicketDto
    {
        public int? DepartmentId { get; set; }
        public string TicketNo { get; set; }
        public int? UserId { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int? AssignedTo { get; set; }
        public string Resolution { get; set; }
        public int? OrderId { get; set; }
    }

    /// <summary>نتیجه ثبت درخواست بررسی سفارش</summary>
    public class OrderDisputeResult
    {
        public bool Success { get; set; }
        public int TicketId { get; set; }
        public string Message { get; set; }
    }

    public class UpdateTicketDto
    {
        public int Id { get; set; }
        public int? DepartmentId { get; set; }
        public string TicketNo { get; set; }
        public int? UserId { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int? AssignedTo { get; set; }
        public string Resolution { get; set; }
    }

    /// <summary>برای دراپ‌داون دپارتمان در فرم تیکت</summary>
    public class TicketDepartmentItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TicketDepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTicketDepartmentDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class UpdateTicketDepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}
