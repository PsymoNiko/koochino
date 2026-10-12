using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyActivityDto
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryParentName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CategoryDisplay =>
            string.IsNullOrWhiteSpace(CategoryParentName) ? CategoryName : $"{CategoryParentName} / {CategoryName}";
    }

    public class CreateAcademyActivityDto
    {
        public int? SiteId { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class UpdateAcademyActivityDto : CreateAcademyActivityDto
    {
        public int Id { get; set; }
    }

    public class AcademyActivitySessionDto
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public int? InstructorId { get; set; }
        public string InstructorName { get; set; }
        public string Title { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int? Capacity { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAcademyActivitySessionDto
    {
        public int ActivityId { get; set; }
        public int? LocationId { get; set; }
        public int? InstructorId { get; set; }
        public string Title { get; set; }
        public DateTime StartsAt { get; set; } = DateTime.Now.AddDays(1);
        public DateTime? EndsAt { get; set; }
        public int? Capacity { get; set; }
        public string Status { get; set; } = AcademyConstants.SessionStatus.Scheduled;
        public string Notes { get; set; }
    }

    public class UpdateAcademyActivitySessionDto : CreateAcademyActivitySessionDto
    {
        public int Id { get; set; }
    }

    public class AcademyActivityRequestDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int StudentId { get; set; }
        public string Status { get; set; }
        public string StudentNote { get; set; }
        public string ReviewerNote { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string SessionTitle { get; set; }
        public int? Capacity { get; set; }
        public int? SessionInstructorId { get; set; }
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string StudentName { get; set; }
        public string StudentPhone { get; set; }
        public string ReviewerName { get; set; }
    }

    public class CreateAcademyActivityRequestDto
    {
        public int SessionId { get; set; }
        public int StudentId { get; set; }
        public string StudentNote { get; set; }
    }

    public class ReviewAcademyActivityRequestDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public string ReviewerNote { get; set; }
    }
}
