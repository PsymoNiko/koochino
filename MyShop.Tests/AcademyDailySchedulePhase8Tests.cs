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
    public class AcademyDailySchedulePhase8Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public void IncludeCancelled_ShowsCancelledDay_DefaultStillHidesIt()
        {
            var daily = Daily();
            var cancelled = new AcademyScheduleExceptionDto
            {
                ScheduleId = 11,
                ExceptionDate = Day1,
                IsCancelled = true,
                Note = "تعطیل"
            };

            var hidden = AcademyDailyScheduleResolver.ListOccurrences(daily, Day1, Day2, new[] { cancelled });
            Assert.Single(hidden);
            Assert.Equal(Day2, hidden[0].SessionDate);

            var shown = AcademyDailyScheduleResolver.ListOccurrences(
                daily, Day1, Day2, new[] { cancelled }, includeCancelled: true);
            Assert.Equal(2, shown.Count);
            Assert.True(shown[0].IsCancelled);
            Assert.Equal("تعطیل", shown[0].ExceptionNote);
            Assert.False(shown[1].IsCancelled);
        }

        [Fact]
        public async Task AdminList_IncludesInactiveCourse_WhenAsked()
        {
            var repo = new FakeCourseRepo
            {
                Course = new AcademyCourseDto
                {
                    Id = 5,
                    IsActive = false,
                    Title = "ریاضی",
                    Schedules = new List<AcademyCourseScheduleDto> { Daily() }
                },
                Schedule = Daily()
            };
            var svc = new AcademyCourseService(repo);

            var hidden = await svc.ListDailyOccurrencesAsync(5, Day1, Day2);
            var shown = await svc.ListDailyOccurrencesAsync(5, Day1, Day2, includeInactive: true);

            Assert.Empty(hidden);
            Assert.Equal(2, shown.Count);
            Assert.Equal("ریاضی", shown[0].CourseTitle);
        }

        [Fact]
        public async Task StudentList_StillHidesCancelled()
        {
            var repo = new FakeCourseRepo
            {
                Course = new AcademyCourseDto
                {
                    Id = 5,
                    IsActive = true,
                    Title = "ریاضی",
                    Schedules = new List<AcademyCourseScheduleDto> { Daily() }
                },
                Schedule = Daily()
            };
            var exceptions = new FakeExceptionRepo();
            exceptions.Items.Add(new AcademyScheduleExceptionDto
            {
                ScheduleId = 11,
                ExceptionDate = Day1,
                IsCancelled = true
            });
            var svc = new AcademyCourseService(repo, exceptions);

            var student = await svc.ListDailyOccurrencesAsync(5, Day1, Day2);
            var admin = await svc.ListDailyOccurrencesAsync(5, Day1, Day2, includeInactive: true, includeCancelled: true);

            Assert.Single(student);
            Assert.Equal(Day2, student[0].SessionDate);
            Assert.Equal(2, admin.Count);
            Assert.Contains(admin, x => x.IsCancelled && x.SessionDate == Day1);
        }

        static AcademyCourseScheduleDto Daily() =>
            new()
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                StartDate = Day1,
                EndDate = new DateTime(2026, 8, 29)
            };

        sealed class FakeExceptionRepo : IAcademyScheduleExceptionRepository
        {
            public List<AcademyScheduleExceptionDto> Items { get; } = new();
            public Task<List<AcademyScheduleExceptionDto>> ListAsync(
                int? scheduleId = null, int? courseId = null, DateTime? fromDate = null, DateTime? toDate = null)
                => Task.FromResult(Items.ToList());
            public Task<AcademyScheduleExceptionDto> GetByIdAsync(int id)
                => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
            public Task<AcademyScheduleExceptionDto> GetByScheduleDateAsync(int scheduleId, DateTime exceptionDate)
                => Task.FromResult(Items.FirstOrDefault(x => x.ScheduleId == scheduleId && x.ExceptionDate.Date == exceptionDate.Date));
            public Task<int> UpsertAsync(UpsertAcademyScheduleExceptionDto dto) => Task.FromResult(0);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class FakeCourseRepo : IAcademyCourseRepository
        {
            public AcademyCourseDto Course { get; set; }
            public AcademyCourseScheduleDto Schedule { get; set; }
            public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
                => Task.FromResult(new List<AcademyCourseDto>());
            public Task<AcademyCourseDto> GetByIdAsync(int id)
                => Task.FromResult(Course != null && Course.Id == id ? Course : null);
            public Task<int> InsertAsync(CreateAcademyCourseDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateAcademyCourseDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
            public Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId)
                => Task.FromResult(Schedule != null ? new List<AcademyCourseScheduleDto> { Schedule } : new());
            public Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId)
                => Task.FromResult(Schedule != null && Schedule.Id == scheduleId ? Schedule : null);
            public Task SetSchedulesAsync(int courseId, IEnumerable<AcademyCourseScheduleDto> schedules)
                => Task.CompletedTask;
            public Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId)
                => Task.FromResult(new List<AcademyCourseSessionDto>());
            public Task ClearSessionsAsync(int courseId) => Task.CompletedTask;
            public Task<int> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto) => Task.FromResult(0);
            public Task<bool> DeleteSessionAsync(int id) => Task.FromResult(true);
        }
    }
}
