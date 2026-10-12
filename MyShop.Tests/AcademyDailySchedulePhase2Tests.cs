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
    public class AcademyDailySchedulePhase2Tests
    {
        static AcademyCourseScheduleDto Daily(int id = 11, int courseId = 5, DateTime? start = null, DateTime? end = null) =>
            new()
            {
                Id = id,
                CourseId = courseId,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                Minute = 0,
                DurationMinutes = 90,
                StartDate = start ?? new DateTime(2026, 8, 23),
                EndDate = end ?? new DateTime(2026, 8, 29)
            };

        static AcademyCourseScheduleDto Weekly() =>
            new()
            {
                Id = 1,
                CourseId = 5,
                DayId = 1,
                Hour = 10,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly
            };

        [Fact]
        public void ListOccurrences_ReturnsEachDayInWindow()
        {
            var daily = Daily();
            var list = AcademyDailyScheduleResolver.ListOccurrences(
                daily, new DateTime(2026, 8, 23), new DateTime(2026, 8, 25));

            Assert.Equal(3, list.Count);
            Assert.Equal(new DateTime(2026, 8, 23), list[0].SessionDate);
            Assert.Equal(new DateTime(2026, 8, 24), list[1].SessionDate);
            Assert.Equal(new DateTime(2026, 8, 25), list[2].SessionDate);
            Assert.All(list, x => Assert.Equal(11, x.ScheduleId));
            Assert.All(list, x => Assert.Equal(20, x.Hour));
        }

        [Fact]
        public void ListOccurrences_ClipsToScheduleRange()
        {
            var daily = Daily(start: new DateTime(2026, 8, 23), end: new DateTime(2026, 8, 25));
            var list = AcademyDailyScheduleResolver.ListOccurrences(
                daily, new DateTime(2026, 8, 1), new DateTime(2026, 9, 30));

            Assert.Equal(3, list.Count);
            Assert.Equal(new DateTime(2026, 8, 23), list[0].SessionDate);
            Assert.Equal(new DateTime(2026, 8, 25), list[^1].SessionDate);
        }

        [Fact]
        public void ListOccurrences_OutsideRange_IsEmpty()
        {
            var daily = Daily();
            var before = AcademyDailyScheduleResolver.ListOccurrences(
                daily, new DateTime(2026, 8, 1), new DateTime(2026, 8, 22));
            var after = AcademyDailyScheduleResolver.ListOccurrences(
                daily, new DateTime(2026, 8, 30), new DateTime(2026, 9, 10));
            var inverted = AcademyDailyScheduleResolver.ListOccurrences(
                daily, new DateTime(2026, 8, 29), new DateTime(2026, 8, 23));

            Assert.Empty(before);
            Assert.Empty(after);
            Assert.Empty(inverted);
        }

        [Fact]
        public void ListOccurrences_WeeklyIsIgnored()
        {
            var mixed = new[] { Weekly(), Daily() };
            var list = AcademyDailyScheduleResolver.ListOccurrences(
                mixed, new DateTime(2026, 8, 23), new DateTime(2026, 8, 23));

            Assert.Single(list);
            Assert.Equal(11, list[0].ScheduleId);
            Assert.Equal(AcademyConstants.ScheduleRecurrenceType.Daily, list[0].RecurrenceType);
        }

        [Fact]
        public void ListOccurrences_DoesNotCreate360PhysicalRows()
        {
            var daily = Daily(start: new DateTime(2026, 8, 23), end: new DateTime(2027, 8, 17));
            var list = AcademyDailyScheduleResolver.ListOccurrences(
                daily, new DateTime(2026, 8, 23), new DateTime(2027, 8, 17));

            Assert.True(list.Count <= AcademyDailyScheduleResolver.MaxListedDaysPerSchedule);
            Assert.True(list.Count >= 360);
            Assert.All(list, x => Assert.True(x.ScheduleId > 0 && x.SessionDate != default));
        }

        [Fact]
        public async Task Service_ListByCourse_SkipsInactiveAndWeekly_AndDoesNotInsertSessions()
        {
            var repo = new FakeCourseRepo
            {
                Course = new AcademyCourseDto
                {
                    Id = 5,
                    IsActive = true,
                    Schedules = new List<AcademyCourseScheduleDto> { Weekly(), Daily() }
                }
            };
            var svc = new AcademyCourseService(repo);

            var list = await svc.ListDailyOccurrencesAsync(5, new DateTime(2026, 8, 23), new DateTime(2026, 8, 24));

            Assert.Equal(2, list.Count);
            Assert.Empty(repo.Upserts);
            Assert.Equal(0, repo.ClearCount);

            repo.Course.IsActive = false;
            var hidden = await svc.ListDailyOccurrencesAsync(5, new DateTime(2026, 8, 23), new DateTime(2026, 8, 24));
            Assert.Empty(hidden);
        }

        [Fact]
        public async Task Service_ListBySchedule_ReturnsEmptyForWeekly()
        {
            var repo = new FakeCourseRepo
            {
                Schedule = Weekly(),
                Course = new AcademyCourseDto { Id = 5, IsActive = true }
            };
            var svc = new AcademyCourseService(repo);

            var list = await svc.ListDailyOccurrencesForScheduleAsync(1, new DateTime(2026, 8, 23), new DateTime(2026, 8, 29));
            Assert.Empty(list);
            Assert.Empty(repo.Upserts);
        }

        sealed class FakeCourseRepo : IAcademyCourseRepository
        {
            public AcademyCourseDto Course { get; set; }
            public AcademyCourseScheduleDto Schedule { get; set; }
            public int ClearCount { get; private set; }
            public List<UpsertAcademyCourseSessionDto> Upserts { get; } = new();

            public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
                => Task.FromResult(new List<AcademyCourseDto>());

            public Task<AcademyCourseDto> GetByIdAsync(int id)
                => Task.FromResult(Course != null && Course.Id == id ? Course : null);

            public Task<int> InsertAsync(CreateAcademyCourseDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateAcademyCourseDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);

            public Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId)
                => Task.FromResult(Course?.Schedules ?? new List<AcademyCourseScheduleDto>());

            public Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId)
                => Task.FromResult(Schedule != null && Schedule.Id == scheduleId ? Schedule : null);

            public Task SetSchedulesAsync(int courseId, IEnumerable<AcademyCourseScheduleDto> schedules)
                => Task.CompletedTask;

            public Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId)
                => Task.FromResult(new List<AcademyCourseSessionDto>());

            public Task ClearSessionsAsync(int courseId)
            {
                ClearCount++;
                return Task.CompletedTask;
            }

            public Task<int> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto)
            {
                Upserts.Add(dto);
                return Task.FromResult(Upserts.Count);
            }

            public Task<bool> DeleteSessionAsync(int id) => Task.FromResult(true);
        }
    }
}
