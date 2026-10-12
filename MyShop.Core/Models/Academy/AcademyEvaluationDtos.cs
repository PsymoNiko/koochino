using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyEvaluationDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int? InstructorId { get; set; }
        public byte CourseRating { get; set; }
        public byte InstructorRating { get; set; }
        public string Comment { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CourseTitle { get; set; }
        public string InstructorName { get; set; }
        public string StudentName { get; set; }
    }

    public class UpsertAcademyEvaluationDto
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public int CourseRating { get; set; } = 5;
        public int InstructorRating { get; set; } = 5;
        public string Comment { get; set; }
    }

    public class AcademyEvaluationCourseSummaryDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string InstructorName { get; set; }
        public int? InstructorId { get; set; }
        public int RatingCount { get; set; }
        public decimal AvgCourseRating { get; set; }
        public decimal AvgInstructorRating { get; set; }
    }

    public class AcademyEvaluationInstructorSummaryDto
    {
        public int InstructorId { get; set; }
        public string InstructorName { get; set; }
        public int RatingCount { get; set; }
        public decimal AvgInstructorRating { get; set; }
    }
}
