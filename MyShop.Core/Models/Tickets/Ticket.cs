using System;

namespace MyShop.Core.Models.Tickets
{
    public class Ticket
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int? ClosedBy { get; set; }
        public string Resolution { get; set; }
        /// <summary>سفارش مرتبط (اعتراض به سفارش ردشده)</summary>
        public int? OrderId { get; set; }
    }
}
