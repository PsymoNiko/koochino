using Dapper;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Academy
{
    public class AcademyLessonRepository : IAcademyLessonRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyLessonRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyLessonDto>> GetByCourseAsync(int courseId, bool publishedOnly = false)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyLessonDto>(
                "koch.spAcademyCourseLessons_List",
                new { CourseId = courseId, PublishedOnly = publishedOnly },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyLessonDto>();
        }

        public async Task<AcademyLessonDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyLessonDto>(
                "koch.spAcademyCourseLessons_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(int courseId, string title, string body, int sortOrder, bool isPublished)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CourseId", courseId);
            p.Add("Title", title);
            p.Add("Body", body);
            p.Add("SortOrder", sortOrder);
            p.Add("IsPublished", isPublished);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourseLessons_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(int id, string title, string body, int sortOrder, bool isPublished)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Title", title);
            p.Add("Body", body);
            p.Add("SortOrder", sortOrder);
            p.Add("IsPublished", isPublished);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourseLessons_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourseLessons_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }
    }

    public class AcademyQuizRepository : IAcademyQuizRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyQuizRepository(IDbConnectionFactory db) => _db = db;

        public async Task<AcademyQuizDto> GetByCourseAsync(int courseId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyQuizDto>(
                "koch.spAcademyQuizzes_GetByCourse", new { CourseId = courseId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<AcademyQuizDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyQuizDto>(
                "koch.spAcademyQuizzes_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertQuizAsync(int courseId, string title, int passScore, bool isPublished)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("CourseId", courseId);
            p.Add("Title", title);
            p.Add("PassScore", passScore);
            p.Add("IsPublished", isPublished);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyQuizzes_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<List<AcademyQuizQuestionDto>> GetQuestionsAsync(int quizId)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyQuizQuestionDto>(
                "koch.spAcademyQuizQuestions_List", new { QuizId = quizId }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyQuizQuestionDto>();
        }

        public async Task<int> InsertQuestionAsync(AcademyQuizQuestionDto q)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("QuizId", q.QuizId);
            p.Add("Prompt", q.Prompt);
            p.Add("OptionA", q.OptionA);
            p.Add("OptionB", q.OptionB);
            p.Add("OptionC", q.OptionC);
            p.Add("OptionD", q.OptionD);
            p.Add("CorrectOption", q.CorrectOption);
            p.Add("SortOrder", q.SortOrder);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyQuizQuestions_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyQuizQuestions_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<AcademyQuizAttemptDto> GetAttemptAsync(int quizId, int studentId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcademyQuizAttemptDto>(
                "koch.spAcademyQuizAttempts_Get",
                new { QuizId = quizId, StudentId = studentId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertAttemptAsync(int quizId, int studentId, int score, bool passed, string answersJson)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("QuizId", quizId);
            p.Add("StudentId", studentId);
            p.Add("Score", score);
            p.Add("Passed", passed);
            p.Add("AnswersJson", answersJson);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyQuizAttempts_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }
    }

    public class AcademyPointsRepository : IAcademyPointsRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyPointsRepository(IDbConnectionFactory db) => _db = db;

        public async Task<int> AwardOnceAsync(int studentId, int points, string reason, string relatedType, int relatedId)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("StudentId", studentId);
            p.Add("Points", points);
            p.Add("Reason", reason);
            p.Add("RelatedType", relatedType);
            p.Add("RelatedId", relatedId);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyPoints_AwardOnce", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<int> GetBalanceAsync(int studentId)
        {
            using var conn = _db.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                "koch.spAcademyPoints_Balance", new { StudentId = studentId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<List<AcademyPointEntryDto>> GetLedgerAsync(int studentId, int top = 100)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyPointEntryDto>(
                "koch.spAcademyPoints_List", new { StudentId = studentId, Top = top }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyPointEntryDto>();
        }

        public async Task<List<AcademyPointEntryDto>> GetLeaderboardAsync(int top = 20)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyPointEntryDto>(
                "koch.spAcademyPoints_Leaderboard", new { Top = top }, commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyPointEntryDto>();
        }
    }
}
