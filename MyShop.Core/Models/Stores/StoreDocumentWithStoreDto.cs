using System;

namespace MyShop.Core.Models.Dto
{
    public class StoreDocumentWithStoreDto
    {
        public int DocumentId { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public bool IsApproved { get; set; }
    }
}
