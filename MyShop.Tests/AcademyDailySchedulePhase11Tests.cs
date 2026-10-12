using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Services.Academy;
using MyShop.Core.Academy;
using MyShop.Core.Models.Academy;
using Xunit;

namespace MyShop.Tests
{
    public class AcademyDailySchedulePhase11Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public void Summarize_Day1IndependentOfDay2()
        {
            var rows = new[]
            {
                new AcademyDailyByDateReportDto
                {
                    CourseId = 5, SessionDate = Day1, ApprovedCount = 2, PresentCount = 1, PendingCount = 1
                },
                new AcademyDailyByDateReportDto
                {
                    CourseId = 5, SessionDate = Day2, ApprovedCount = 4, AbsentCount = 3, RejectedCount = 1
                }
            };

            var sum = AcademyDailyReportMath.Summarize(rows);
            Assert.Equal(1, sum.PendingRequests);
            Assert.Equal(6, sum.ApprovedRequests);
            Assert.Equal(1, sum.RejectedRequests);
            Assert.Equal(1, sum.PresentCount);
            Assert.Equal(3, sum.AbsentCount);

            var onlyDay1 = AcademyDailyReportMath.Summarize(rows.Where(x => x.SessionDate == Day1));
            Assert.Equal(2, onlyDay1.ApprovedRequests);
            Assert.Equal(0, onlyDay1.AbsentCount);
        }

        [Fact]
        public async Task Service_PassesInstructorFilter_AndDoesNotMixCourses()
        {
            var repo = new FakeReportRepo();
            repo.ByCourse.Add(new AcademyDailyByCourseReportDto { CourseId = 5, Title = "من", ApprovedCount = 3 });
            repo.ByCourse.Add(new AcademyDailyByCourseReportDto { CourseId = 9, Title = "دیگران", ApprovedCount = 9 });
            repo.ByDate.Add(new AcademyDailyByDateReportDto { CourseId = 5, SessionDate = Day1, ApprovedCount = 3 });
            repo.ByDate.Add(new AcademyDailyByDateReportDto { CourseId = 9, SessionDate = Day1, ApprovedCount = 9 });
            var svc = new AcademyReportService(repo);

            var mine = await svc.GetDailyByCourseAsync(3);
            var all = await svc.GetDailyByCourseAsync();
            var dates = await svc.GetDailyByDateAsync(Day1, Day1, instructorId: 3);

            Assert.Single(mine);
            Assert.Equal(5, mine[0].CourseId);
            Assert.Equal(2, all.Count);
            Assert.Single(dates);
            Assert.Equal(5, dates[0].CourseId);
            Assert.Equal(3, repo.LastInstructorId);
        }

        [Fact]
        public async Task EmptyRange_ReturnsEmpty()
        {
            var repo = new FakeReportRepo();
            repo.ByDate.Add(new AcademyDailyByDateReportDto { CourseId = 5, SessionDate = Day1, ApprovedCount = 1 });
            var svc = new AcademyReportService(repo);
            var list = await svc.GetDailyByDateAsync(Day2, Day2);
            Assert.Empty(list);
        }

        sealed class FakeReportRepo : IAcademyReportRepository
        {
            public List<AcademyDailyByCourseReportDto> ByCourse { get; } = new();
            public List<AcademyDailyByDateReportDto> ByDate { get; } = new();
            public int? LastInstructorId { get; private set; }

            public Task<AcademyAdminSummaryDto> GetAdminSummaryAsync() => Task.FromResult(new AcademyAdminSummaryDto());
            public Task<List<AcademyCourseReportDto>> GetCoursesAsync(int? instructorId = null)
                => Task.FromResult(new List<AcademyCourseReportDto>());
            public Task<List<AcademyTuitionReportDto>> GetTuitionAsync(int? instructorId = null)
                => Task.FromResult(new List<AcademyTuitionReportDto>());
            public Task<List<AcademyAttendanceReportDto>> GetAttendanceByCourseAsync(int? instructorId = null)
                => Task.FromResult(new List<AcademyAttendanceReportDto>());
            public Task<List<AcademyActivityReportDto>> GetActivitiesAsync(int? instructorId = null)
                => Task.FromResult(new List<AcademyActivityReportDto>());
            public Task<List<AcademyInstructorReportDto>> GetInstructorsAsync()
                => Task.FromResult(new List<AcademyInstructorReportDto>());
            public Task<AcademyInstructorSummaryDto> GetInstructorSummaryAsync(int instructorId)
                => Task.FromResult(new AcademyInstructorSummaryDto());
            public Task<AcademyStudentSummaryDto> GetStudentSummaryAsync(int studentId)
                => Task.FromResult(new AcademyStudentSummaryDto());
            public Task<AcademyDailyReportSummaryDto> GetDailySummaryAsync(int? instructorId = null)
            {
                LastInstructorId = instructorId;
                return Task.FromResult(new AcademyDailyReportSummaryDto());
            }
            public Task<List<AcademyDailyByDateReportDto>> GetDailyByDateAsync(
                DateTime fromDate, DateTime toDate, int? courseId = null, int? instructorId = null)
            {
                LastInstructorId = instructorId;
                var q = ByDate.Where(x => x.SessionDate.Date >= fromDate.Date && x.SessionDate.Date <= toDate.Date);
                if (instructorId == 3) q = q.Where(x => x.CourseId == 5);
                if (courseId.HasValue) q = q.Where(x => x.CourseId == courseId.Value);
                return Task.FromResult(q.ToList());
            }
            public Task<List<AcademyDailyByCourseReportDto>> GetDailyByCourseAsync(int? instructorId = null)
            {
                LastInstructorId = instructorId;
                var q = ByCourse.AsEnumerable();
                if (instructorId == 3) q = q.Where(x => x.CourseId == 5);
                return Task.FromResult(q.ToList());
            }
        }
    }
}
