using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyScheduleExceptionDto
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public DateTime ExceptionDate { get; set; }
        public bool IsCancelled { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public int? DurationMinutes { get; set; }
        public int? Capacity { get; set; }
        public int? InstructorId { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int? CourseInstructorId { get; set; }
    }

    public class UpsertAcademyScheduleExceptionDto
    {
        public int ScheduleId { get; set; }
        public DateTime ExceptionDate { get; set; }
        public bool IsCancelled { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public int? DurationMinutes { get; set; }
        public int? Capacity { get; set; }
        public int? InstructorId { get; set; }
        public string Note { get; set; }
    }
}
