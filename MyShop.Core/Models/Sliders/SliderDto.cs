using System;
using System.ComponentModel.DataAnnotations;

namespace MyShop.Core.Models.Dto
{
    public class SliderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PlaceKey { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SliderSlideDto
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
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateSliderDto
    {
        [Required(ErrorMessage = "نام اسلایدر الزامی است")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "محل استفاده (PlaceKey) الزامی است")]
        public string PlaceKey { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class UpdateSliderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PlaceKey { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreateSliderSlideDto
    {
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
    }

    public class UpdateSliderSlideDto
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
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
