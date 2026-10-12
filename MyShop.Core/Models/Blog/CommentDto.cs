using System.Collections.Generic;

namespace MyShop.Core.Models.Dto
{
    public class CommentDto
    {
        public long CommentId { get; set; }
        public long? ProductId { get; set; }
        public long? OrderId { get; set; }
        public long? StoreId { get; set; }
        public long UserId { get; set; }
        public long? ParentId { get; set; }
        public string Text { get; set; } = string.Empty;
        public byte? Rate { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Depth { get; set; }
        public byte? ReplierType { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public byte StatusView { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
    }

    public class CommentListItemDto
    {
        public long CommentId { get; set; }
        public string Text { get; set; } = string.Empty;
        public byte? Rate { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public long? StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public long? OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public long? ProductId { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public byte StatusView { get; set; }
        public bool IsDeleted { get; set; }
        public int Depth { get; set; }
        public long? ParentId { get; set; }
        public int? ModeratorUserId { get; set; }
        public System.DateTime? ModeratedAt { get; set; }
        public string ModerationNotes { get; set; } = string.Empty;
        public int ReportCount { get; set; }
        public System.DateTime? LastReportedAt { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
    }

    /// <summary>تعداد و میانگین امتیاز نظرات تاییدشده برای فروشگاه یا محصول.</summary>
    public class CommentCountResultDto
    {
        public int TotalCount { get; set; }
        public double? AvgRate { get; set; }
    }

    public class CommentTreeNodeDto
    {
        public long CommentId { get; set; }
        public string Text { get; set; } = string.Empty;
        public byte? Rate { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public byte? ReplierType { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime? UpdatedAt { get; set; }
        public int Depth { get; set; }
        public List<CommentTreeNodeDto> Children { get; set; } = new();
    }

    public class CreateCommentRequest
    {
        public long? ProductId { get; set; }
        public long? OrderId { get; set; }
        public long? StoreId { get; set; }
        public long? ParentId { get; set; }
        public string Text { get; set; } = string.Empty;
        public byte? Rate { get; set; }
        public byte? ReplierType { get; set; }
    }
}
