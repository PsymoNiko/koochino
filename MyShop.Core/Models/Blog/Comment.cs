using System;

namespace MyShop.Core.Models
{
    /// <summary>نظر چندلایه. StatusView: 0=در انتظار، 1=تایید، 2=رد. ReplierType: null=اصلی، 0=کاربر، 1=فروشنده، 2=ادمین.</summary>
    public class Comment
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
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public byte StatusView { get; set; }
    }
}
