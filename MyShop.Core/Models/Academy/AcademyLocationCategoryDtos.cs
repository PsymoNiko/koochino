using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Academy
{
    public class AcademyLocationDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int? Capacity { get; set; }
        public string Description { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAcademyLocationDto
    {
        public int? SiteId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int? Capacity { get; set; }
        public string Description { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class UpdateAcademyLocationDto : CreateAcademyLocationDto
    {
        public int Id { get; set; }
    }

    public class AcademyActivityCategoryDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int? ParentId { get; set; }
        public string ParentName { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AcademyActivityCategoryDto> Children { get; set; } = new();
    }

    public class CreateAcademyActivityCategoryDto
    {
        public int? SiteId { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateAcademyActivityCategoryDto : CreateAcademyActivityCategoryDto
    {
        public int Id { get; set; }
    }
}
