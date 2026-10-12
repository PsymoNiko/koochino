using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyLessonDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CourseTitle { get; set; }
    }

    public class AcademyQuizDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public int PassScore { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CourseTitle { get; set; }
        public int? CourseInstructorId { get; set; }
        public int QuestionCount { get; set; }
    }

    public class AcademyQuizQuestionDto
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string Prompt { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string CorrectOption { get; set; }
        public int SortOrder { get; set; }
    }

    public class AcademyQuizAttemptDto
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int StudentId { get; set; }
        public int Score { get; set; }
        public bool Passed { get; set; }
        public string AnswersJson { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class AcademyPointEntryDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; }
        public string RelatedType { get; set; }
        public int? RelatedId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StudentName { get; set; }
        public int Balance { get; set; }
    }
}
