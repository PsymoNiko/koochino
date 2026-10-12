using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Services.Academy;
using MyShop.Core.Models.Academy;
using Xunit;

namespace MyShop.Tests
{
    public class AcademyDailySchedulePhase9Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public async Task Instructor_SeesOnlyOwnActiveDailyDays()
        {
            var repo = new FakeCourseRepo();
            repo.Courses.Add(new AcademyCourseDto
            {
                Id = 5,
                IsActive = true,
                InstructorId = 3,
                Title = "ریاضی من",
                Schedules = new List<AcademyCourseScheduleDto> { Daily(11, 5) }
            });
            repo.Courses.Add(new AcademyCourseDto
            {
                Id = 9,
                IsActive = true,
                InstructorId = 99,
                Title = "دوره دیگران",
                Schedules = new List<AcademyCourseScheduleDto> { Daily(22, 9) }
            });
            repo.Courses.Add(new AcademyCourseDto
            {
                Id = 8,
                IsActive = false,
                InstructorId = 3,
                Title = "غیرفعال",
                Schedules = new List<AcademyCourseScheduleDto> { Daily(33, 8) }
            });
            var svc = new AcademyCourseService(repo);

            var mine = await svc.ListDailyOccurrencesForInstructorAsync(3, Day1, Day2);
            var other = await svc.ListDailyOccurrencesForInstructorAsync(99, Day1, Day2);
            var nobody = await svc.ListDailyOccurrencesForInstructorAsync(7, Day1, Day2);

            Assert.Equal(2, mine.Count);
            Assert.All(mine, x => Assert.Equal(5, x.CourseId));
            Assert.Equal("ریاضی من", mine[0].CourseTitle);
            Assert.Equal(2, other.Count);
            Assert.All(other, x => Assert.Equal(9, x.CourseId));
            Assert.Empty(nobody);
        }

        [Fact]
        public async Task Instructor_TodayIsIndependentOfTomorrow()
        {
            var repo = new FakeCourseRepo();
            repo.Courses.Add(new AcademyCourseDto
            {
                Id = 5,
                IsActive = true,
                InstructorId = 3,
                Title = "ریاضی",
                Schedules = new List<AcademyCourseScheduleDto> { Daily(11, 5) }
            });
            var svc = new AcademyCourseService(repo);
            var today = await svc.ListDailyOccurrencesForInstructorAsync(3, Day1, Day1);
            var tomorrow = await svc.ListDailyOccurrencesForInstructorAsync(3, Day2, Day2);
            Assert.Single(today);
            Assert.Equal(Day1, today[0].SessionDate);
            Assert.Single(tomorrow);
            Assert.Equal(Day2, tomorrow[0].SessionDate);
        }

        static AcademyCourseScheduleDto Daily(int id, int courseId) =>
            new()
            {
                Id = id,
                CourseId = courseId,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                StartDate = Day1,
                EndDate = new DateTime(2026, 8, 29)
            };

        sealed class FakeCourseRepo : IAcademyCourseRepository
        {
            public List<AcademyCourseDto> Courses { get; } = new();
            public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
                => Task.FromResult(Courses.ToList());
            public Task<AcademyCourseDto> GetByIdAsync(int id)
                => Task.FromResult(Courses.FirstOrDefault(c => c.Id == id));
            public Task<int> InsertAsync(CreateAcademyCourseDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateAcademyCourseDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
            public Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId)
                => Task.FromResult(Courses.FirstOrDefault(c => c.Id == courseId)?.Schedules ?? new());
            public Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId)
                => Task.FromResult(Courses.SelectMany(c => c.Schedules ?? new()).FirstOrDefault(s => s.Id == scheduleId));
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
