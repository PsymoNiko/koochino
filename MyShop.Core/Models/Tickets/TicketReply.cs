using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Tickets
{
    public class TicketReply
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; }
        public bool IsStaffReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TicketAttachment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? ReplyId { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public int? FileSize { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class TicketReplyDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; }
        public bool IsStaffReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; }
        public List<TicketAttachmentDto> Attachments { get; set; } = new();
    }

    public class TicketAttachmentDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? ReplyId { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public int? FileSize { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class TicketDetailDto
    {
        public TicketDto Ticket { get; set; }
        public List<TicketReplyDto> Replies { get; set; } = new();
        public List<TicketAttachmentDto> Attachments { get; set; } = new();
    }

    public class TicketStatsDto
    {
        public int TotalCount { get; set; }
        public int WaitingCount { get; set; }
        public int AnsweredCount { get; set; }
        public int ClosedCount { get; set; }
    }

    public class CreateTicketReplyDto
    {
        public int TicketId { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; }
        public bool IsStaffReply { get; set; }
        public bool CloseTicket { get; set; }
    }
}
