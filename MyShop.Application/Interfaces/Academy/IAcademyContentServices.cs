using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyLessonRepository
    {
        Task<List<AcademyLessonDto>> GetByCourseAsync(int courseId, bool publishedOnly = false);
        Task<AcademyLessonDto> GetByIdAsync(int id);
        Task<int> InsertAsync(int courseId, string title, string body, int sortOrder, bool isPublished);
        Task<bool> UpdateAsync(int id, string title, string body, int sortOrder, bool isPublished);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyLessonService
    {
        Task<List<AcademyLessonDto>> GetByCourseAsync(int courseId, bool publishedOnly = false);
        Task<List<AcademyLessonDto>> GetPublishedForStudentAsync(int courseId, int studentId);
        Task<AcademyActionResult> CreateAsync(int courseId, string title, string body, int sortOrder, bool isPublished);
        Task<AcademyActionResult> UpdateAsync(int id, string title, string body, int sortOrder, bool isPublished);
        Task<AcademyActionResult> DeleteAsync(int id);
    }

    public interface IAcademyQuizRepository
    {
        Task<AcademyQuizDto> GetByCourseAsync(int courseId);
        Task<AcademyQuizDto> GetByIdAsync(int id);
        Task<int> UpsertQuizAsync(int courseId, string title, int passScore, bool isPublished);
        Task<List<AcademyQuizQuestionDto>> GetQuestionsAsync(int quizId);
        Task<int> InsertQuestionAsync(AcademyQuizQuestionDto q);
        Task<bool> DeleteQuestionAsync(int id);
        Task<AcademyQuizAttemptDto> GetAttemptAsync(int quizId, int studentId);
        Task<int> UpsertAttemptAsync(int quizId, int studentId, int score, bool passed, string answersJson);
    }

    public interface IAcademyQuizService
    {
        Task<AcademyQuizDto> GetByCourseAsync(int courseId);
        Task<AcademyQuizDto> GetPublishedForStudentAsync(int courseId, int studentId);
        Task<AcademyActionResult> UpsertQuizAsync(int courseId, string title, int passScore, bool isPublished);
        Task<List<AcademyQuizQuestionDto>> GetQuestionsAsync(int quizId, bool includeAnswers);
        Task<List<AcademyQuizQuestionDto>> GetQuestionsForStudentAsync(int quizId, int studentId);
        Task<AcademyActionResult> AddQuestionAsync(AcademyQuizQuestionDto q);
        Task<AcademyActionResult> DeleteQuestionAsync(int id);
        Task<AcademyQuizAttemptDto> GetAttemptAsync(int quizId, int studentId);
        Task<AcademyActionResult> SubmitAttemptAsync(int quizId, int studentId, Dictionary<int, string> answers);
    }

    public interface IAcademyPointsRepository
    {
        Task<int> AwardOnceAsync(int studentId, int points, string reason, string relatedType, int relatedId);
        Task<int> GetBalanceAsync(int studentId);
        Task<List<AcademyPointEntryDto>> GetLedgerAsync(int studentId, int top = 100);
        Task<List<AcademyPointEntryDto>> GetLeaderboardAsync(int top = 20);
    }

    public interface IAcademyPointsService
    {
        Task AwardOnceAsync(int studentId, string reason, string relatedType, int relatedId, int points);
        Task<int> GetBalanceAsync(int studentId);
        Task<List<AcademyPointEntryDto>> GetLedgerAsync(int studentId, int top = 100);
        Task<List<AcademyPointEntryDto>> GetLeaderboardAsync(int top = 20);
    }
}
