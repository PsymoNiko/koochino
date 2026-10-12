using System;

namespace MyShop.Core.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsPublished { get; set; }
        public int ViewCount { get; set; }
        public string? ImageUrl { get; set; }
        public string? Tags { get; set; }
    }

    public class CreatePostRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public string? ImageUrl { get; set; }
        public string? Tags { get; set; }
        public bool SendToBale { get; set; } = false;
        public bool SendToEita { get; set; } = false;
    }

    public class UpdatePostRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public string? ImageUrl { get; set; }
        public string? Tags { get; set; }
    }
}
