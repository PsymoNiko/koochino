using System;

namespace MyShop.Core.Models.Sliders
{
    public class Slider
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PlaceKey { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
