using System;

namespace MyShop.Core.Models.Dto
{
    public class AnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AlertColor { get; set; } = "Info";
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int TargetAudience { get; set; } // 1=ShopOwner, 2=Customer, 3=Both
        public string TargetAudienceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class CreateAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string AlertColor { get; set; } = "Info";
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public TargetAudience TargetAudience { get; set; }
    }

    public class UpdateAnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AlertColor { get; set; } = "Info";
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public TargetAudience TargetAudience { get; set; }
    }
}
