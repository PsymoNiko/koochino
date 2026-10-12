using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyLessonService : IAcademyLessonService
    {
        private readonly IAcademyLessonRepository _repo;
        private readonly IAcademyEnrollmentRepository _enrollments;
        private readonly IAcademyCourseRepository _courses;
        private readonly ICurrentTenant _tenant;

        public AcademyLessonService(
            IAcademyLessonRepository repo,
            IAcademyEnrollmentRepository enrollments,
            IAcademyCourseRepository courses = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _enrollments = enrollments;
            _courses = courses;
            _tenant = tenant;
        }

        public async Task<List<AcademyLessonDto>> GetByCourseAsync(int courseId, bool publishedOnly = false)
            => await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId)
                ? await _repo.GetByCourseAsync(courseId, publishedOnly)
                : new List<AcademyLessonDto>();

        public async Task<List<AcademyLessonDto>> GetPublishedForStudentAsync(int courseId, int studentId)
        {
            if (!await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId))
                return new List<AcademyLessonDto>();
            if (!await AcademyStudentCourseAccess.CanAccessAsync(_enrollments, courseId, studentId))
                return new List<AcademyLessonDto>();
            return await _repo.GetByCourseAsync(courseId, publishedOnly: true) ?? new List<AcademyLessonDto>();
        }

        public async Task<AcademyActionResult> CreateAsync(int courseId, string title, string body, int sortOrder, bool isPublished)
        {
            if (string.IsNullOrWhiteSpace(title)) return AcademyActionResult.Fail("عنوان الزامی است.");
            if (!await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId))
                return AcademyActionResult.Fail("دوره یافت نشد.");
            var id = await _repo.InsertAsync(courseId, title.Trim(), body, sortOrder, isPublished);
            return id > 0 ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("ثبت درس ناموفق بود.");
        }

        public async Task<AcademyActionResult> UpdateAsync(int id, string title, string body, int sortOrder, bool isPublished)
        {
            if (string.IsNullOrWhiteSpace(title)) return AcademyActionResult.Fail("عنوان الزامی است.");
            var lesson = await _repo.GetByIdAsync(id);
            if (lesson == null || !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, lesson.CourseId))
                return AcademyActionResult.Fail("درس یافت نشد.");
            var ok = await _repo.UpdateAsync(id, title.Trim(), body, sortOrder, isPublished);
            return ok ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("درس یافت نشد.");
        }

        public async Task<AcademyActionResult> DeleteAsync(int id)
        {
            var lesson = await _repo.GetByIdAsync(id);
            if (lesson == null || !await AcademyTenantOwnership.CourseAsync(_tenant, _courses, lesson.CourseId))
                return AcademyActionResult.Fail("حذف ناموفق بود.");
            return await _repo.DeleteAsync(id) ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("حذف ناموفق بود.");
        }
    }

    public class AcademyQuizService : IAcademyQuizService
    {
        private readonly IAcademyQuizRepository _repo;
        private readonly IAcademyEnrollmentRepository _enrollments;
        private readonly IAcademyPointsService _points;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;

        public AcademyQuizService(
            IAcademyQuizRepository repo,
            IAcademyEnrollmentRepository enrollments,
            IAcademyPointsService points,
            IAcademyCourseRepository courses = null,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _enrollments = enrollments;
            _points = points;
            _courses = courses;
            _students = students;
            _tenant = tenant;
        }

        public async Task<AcademyQuizDto> GetByCourseAsync(int courseId)
            => await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId)
                ? await _repo.GetByCourseAsync(courseId)
                : null;

        public async Task<AcademyQuizDto> GetPublishedForStudentAsync(int courseId, int studentId)
        {
            if (!await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId)
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId))
                return null;
            if (!await AcademyStudentCourseAccess.CanAccessAsync(_enrollments, courseId, studentId))
                return null;
            var quiz = await _repo.GetByCourseAsync(courseId);
            return quiz != null && quiz.IsPublished ? quiz : null;
        }

        public async Task<AcademyActionResult> UpsertQuizAsync(int courseId, string title, int passScore, bool isPublished)
        {
            if (string.IsNullOrWhiteSpace(title)) return AcademyActionResult.Fail("عنوان آزمون الزامی است.");
            if (!await AcademyTenantOwnership.CourseAsync(_tenant, _courses, courseId))
                return AcademyActionResult.Fail("دوره یافت نشد.");
            var id = await _repo.UpsertQuizAsync(courseId, title.Trim(), passScore, isPublished);
            return id > 0 ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("ذخیره آزمون ناموفق بود.");
        }

        public async Task<List<AcademyQuizQuestionDto>> GetQuestionsAsync(int quizId, bool includeAnswers)
        {
            if (!await OwnsQuizAsync(quizId))
                return new List<AcademyQuizQuestionDto>();
            var list = await _repo.GetQuestionsAsync(quizId) ?? new List<AcademyQuizQuestionDto>();
            if (!includeAnswers)
            {
                foreach (var q in list) q.CorrectOption = null;
            }
            return list;
        }

        public async Task<List<AcademyQuizQuestionDto>> GetQuestionsForStudentAsync(int quizId, int studentId)
        {
            var quiz = await _repo.GetByIdAsync(quizId);
            if (quiz == null || !await OwnsQuizAsync(quiz) || !quiz.IsPublished
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId))
                return new List<AcademyQuizQuestionDto>();
            if (!await AcademyStudentCourseAccess.CanAccessAsync(_enrollments, quiz.CourseId, studentId))
                return new List<AcademyQuizQuestionDto>();
            return await GetQuestionsAsync(quizId, includeAnswers: false);
        }

        public async Task<AcademyActionResult> AddQuestionAsync(AcademyQuizQuestionDto q)
        {
            if (q == null || q.QuizId <= 0) return AcademyActionResult.Fail("آزمون نامعتبر است.");
            if (!await OwnsQuizAsync(q.QuizId)) return AcademyActionResult.Fail("آزمون نامعتبر است.");
            if (string.IsNullOrWhiteSpace(q.Prompt) || string.IsNullOrWhiteSpace(q.OptionA) || string.IsNullOrWhiteSpace(q.OptionB))
                return AcademyActionResult.Fail("صورت سؤال و گزینه A و B الزامی است.");
            var opt = (q.CorrectOption ?? "A").Trim().ToUpperInvariant();
            if (opt is not ("A" or "B" or "C" or "D")) return AcademyActionResult.Fail("گزینه صحیح نامعتبر است.");
            q.CorrectOption = opt;
            var id = await _repo.InsertQuestionAsync(q);
            return id > 0 ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("ثبت سؤال ناموفق بود.");
        }

        public async Task<AcademyActionResult> DeleteQuestionAsync(int id)
        {
            if (!await OwnsQuestionAsync(id)) return AcademyActionResult.Fail("حذف ناموفق بود.");
            return await _repo.DeleteQuestionAsync(id) ? AcademyActionResult.Success(id) : AcademyActionResult.Fail("حذف ناموفق بود.");
        }

        public async Task<AcademyQuizAttemptDto> GetAttemptAsync(int quizId, int studentId)
            => await OwnsQuizAsync(quizId)
                && await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId)
                    ? await _repo.GetAttemptAsync(quizId, studentId)
                    : null;

        public async Task<AcademyActionResult> SubmitAttemptAsync(int quizId, int studentId, Dictionary<int, string> answers)
        {
            var quiz = await _repo.GetByIdAsync(quizId);
            if (quiz == null || !await OwnsQuizAsync(quiz) || !quiz.IsPublished
                || !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId))
                return AcademyActionResult.Fail("آزمون در دسترس نیست.");

            var enroll = await _enrollments.GetByCourseStudentAsync(quiz.CourseId, studentId);
            if (enroll == null || (enroll.Status != AcademyConstants.EnrollmentStatus.Approved
                && enroll.Status != AcademyConstants.EnrollmentStatus.Completed))
                return AcademyActionResult.Fail("فقط دانش‌آموز تأییدشده این دوره می‌تواند آزمون بدهد.");

            var questions = await _repo.GetQuestionsAsync(quizId) ?? new List<AcademyQuizQuestionDto>();
            if (questions.Count == 0) return AcademyActionResult.Fail("سؤالی تعریف نشده است.");

            var correct = 0;
            foreach (var q in questions)
            {
                if (answers != null && answers.TryGetValue(q.Id, out var a)
                    && string.Equals(a?.Trim(), q.CorrectOption, StringComparison.OrdinalIgnoreCase))
                    correct++;
            }
            var score = (int)Math.Round(100.0 * correct / questions.Count);
            var passed = score >= quiz.PassScore;
            var json = JsonSerializer.Serialize(answers ?? new Dictionary<int, string>());
            var id = await _repo.UpsertAttemptAsync(quizId, studentId, score, passed, json);
            if (id <= 0) return AcademyActionResult.Fail("ثبت نتیجه ناموفق بود.");

            if (passed)
            {
                try { await _points.AwardOnceAsync(studentId, AcademyConstants.PointReason.QuizPass, "Quiz", quizId, 30); }
                catch { }
            }
            return AcademyActionResult.Success(id);
        }

        private async Task<bool> OwnsQuizAsync(int quizId)
        {
            var quiz = await _repo.GetByIdAsync(quizId);
            return await OwnsQuizAsync(quiz);
        }

        private Task<bool> OwnsQuizAsync(AcademyQuizDto quiz)
            => quiz == null
                ? Task.FromResult(false)
                : AcademyTenantOwnership.CourseAsync(_tenant, _courses, quiz.CourseId);

        private async Task<bool> OwnsQuestionAsync(int questionId)
        {
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return true;
            var courses = await _courses.GetAllAsync(AcademyTenantOwnership.SiteId(_tenant));
            foreach (var course in courses)
            {
                var quiz = await _repo.GetByCourseAsync(course.Id);
                if (quiz == null) continue;
                var questions = await _repo.GetQuestionsAsync(quiz.Id);
                if (questions.Any(x => x.Id == questionId)) return true;
            }
            return false;
        }
    }

    public class AcademyPointsService : IAcademyPointsService
    {
        private readonly IAcademyPointsRepository _repo;
        private readonly IAcademyStudentRepository _students;
        private readonly ICurrentTenant _tenant;
        public AcademyPointsService(
            IAcademyPointsRepository repo,
            IAcademyStudentRepository students = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _students = students;
            _tenant = tenant;
        }

        public async Task AwardOnceAsync(int studentId, string reason, string relatedType, int relatedId, int points)
        {
            if (studentId <= 0 || points == 0) return;
            if (!await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId)) return;
            await _repo.AwardOnceAsync(studentId, points, reason, relatedType, relatedId);
        }

        public async Task<int> GetBalanceAsync(int studentId)
        {
            if (!await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId)) return 0;
            try { return await _repo.GetBalanceAsync(studentId); }
            catch { return 0; }
        }

        public async Task<List<AcademyPointEntryDto>> GetLedgerAsync(int studentId, int top = 100)
        {
            if (!await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId))
                return new List<AcademyPointEntryDto>();
            try { return await _repo.GetLedgerAsync(studentId, top); }
            catch { return new List<AcademyPointEntryDto>(); }
        }

        public async Task<List<AcademyPointEntryDto>> GetLeaderboardAsync(int top = 20)
        {
            try
            {
                var list = await _repo.GetLeaderboardAsync(top);
                if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
                var result = new List<AcademyPointEntryDto>();
                foreach (var item in list)
                    if (await AcademyTenantOwnership.StudentAsync(_tenant, _students, item.StudentId))
                        result.Add(item);
                return result;
            }
            catch { return new List<AcademyPointEntryDto>(); }
        }
    }

    internal static class AcademyStudentCourseAccess
    {
        public static async Task<bool> CanAccessAsync(IAcademyEnrollmentRepository enrollments, int courseId, int studentId)
        {
            if (courseId <= 0 || studentId <= 0) return false;
            var enroll = await enrollments.GetByCourseStudentAsync(courseId, studentId);
            if (enroll == null) return false;
            return enroll.Status == AcademyConstants.EnrollmentStatus.Approved
                || enroll.Status == AcademyConstants.EnrollmentStatus.Completed;
        }
    }
}
