using System;

namespace MyShop.Core.Models.Sliders
{
    public class SliderSlide
    {
        public int Id { get; set; }
        public int SliderId { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageAltText { get; set; }
        public string? LinkUrl { get; set; }
        public string LinkTarget { get; set; } = "_self";
        public string? ButtonText { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
