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
    public class AcademyDailySchedulePhase3Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public async Task Create_PendingRequest_DoesNotRequireEnrollment()
        {
            var ctx = Context();
            var r = await ctx.Service.CreateAsync(NewDto(studentId: 7, date: Day1), actingStudentId: 7);

            Assert.True(r.Ok);
            Assert.True(r.Id > 0);
            var saved = ctx.Requests.Items.Single();
            Assert.Equal(AcademyConstants.RequestStatus.Pending, saved.Status);
            Assert.Equal(11, saved.ScheduleId);
            Assert.Equal(Day1, saved.SessionDate);
            Assert.Equal(7, saved.StudentId);
        }

        [Fact]
        public async Task Create_DuplicateSameDay_Fails()
        {
            var ctx = Context();
            var first = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var second = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);

            Assert.True(first.Ok);
            Assert.False(second.Ok);
            Assert.Contains("قبلاً", second.Error);
            Assert.Single(ctx.Requests.Items);
        }

        [Fact]
        public async Task Create_OutsideRange_Fails()
        {
            var ctx = Context();
            var r = await ctx.Service.CreateAsync(NewDto(7, new DateTime(2026, 8, 22)), 7);

            Assert.False(r.Ok);
            Assert.Empty(ctx.Requests.Items);
        }

        [Fact]
        public async Task Create_WeeklySchedule_Fails()
        {
            var ctx = Context();
            ctx.Courses.Schedule = Weekly();
            var r = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);

            Assert.False(r.Ok);
            Assert.Empty(ctx.Requests.Items);
        }

        [Fact]
        public async Task Create_Idor_OtherStudent_Fails()
        {
            var ctx = Context();
            var r = await ctx.Service.CreateAsync(NewDto(studentId: 99, date: Day1), actingStudentId: 7);

            Assert.False(r.Ok);
            Assert.Empty(ctx.Requests.Items);
        }

        [Fact]
        public async Task Create_CannotSetApprovedStatus()
        {
            var ctx = Context();
            ctx.Requests.ForceStatusOnInsert = AcademyConstants.RequestStatus.Approved;
            var r = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);

            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single().Status);
        }

        [Fact]
        public async Task Create_Day1AndDay2_AreIndependent()
        {
            var ctx = Context();
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(NewDto(7, Day2), 7);

            Assert.True(a.Ok);
            Assert.True(b.Ok);
            Assert.Equal(2, ctx.Requests.Items.Count);
            Assert.All(ctx.Requests.Items, x => Assert.Equal(AcademyConstants.RequestStatus.Pending, x.Status));
        }

        [Fact]
        public async Task ListMine_ReturnsOnlyActingStudent()
        {
            var ctx = Context();
            await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            await ctx.Service.CreateAsync(new CreateAcademyCourseScheduleRequestDto
            {
                ScheduleId = 11,
                StudentId = 8,
                SessionDate = Day1
            }, 8);

            var mine = await ctx.Service.ListMineAsync(7);
            Assert.Single(mine);
            Assert.Equal(7, mine[0].StudentId);

            var other = await ctx.Service.GetMineByIdAsync(mine[0].Id, actingStudentId: 8);
            Assert.Null(other);
        }

        static CreateAcademyCourseScheduleRequestDto NewDto(int studentId, DateTime date) =>
            new()
            {
                ScheduleId = 11,
                StudentId = studentId,
                SessionDate = date
            };

        static AcademyCourseScheduleDto Daily() =>
            new()
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                Minute = 0,
                DurationMinutes = 90,
                StartDate = Day1,
                EndDate = new DateTime(2026, 8, 29)
            };

        static AcademyCourseScheduleDto Weekly() =>
            new()
            {
                Id = 11,
                CourseId = 5,
                DayId = 1,
                Hour = 10,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly
            };

        static TestContext Context()
        {
            var courses = new FakeCourseRepo
            {
                Course = new AcademyCourseDto { Id = 5, IsActive = true, Title = "ریاضی" },
                Schedule = Daily()
            };
            var students = new FakeStudentRepo();
            students.Items.Add(new AcademyStudentDto { Id = 7, Status = AcademyConstants.StudentStatus.Active });
            var requests = new FakeRequestRepo();
            return new TestContext
            {
                Courses = courses,
                Students = students,
                Requests = requests,
                Service = new AcademyCourseScheduleRequestService(requests, courses, students)
            };
        }

        sealed class TestContext
        {
            public FakeCourseRepo Courses { get; set; }
            public FakeStudentRepo Students { get; set; }
            public FakeRequestRepo Requests { get; set; }
            public AcademyCourseScheduleRequestService Service { get; set; }
        }

        sealed class FakeRequestRepo : IAcademyCourseScheduleRequestRepository
        {
            public List<AcademyCourseScheduleRequestDto> Items { get; } = new();
            public string ForceStatusOnInsert { get; set; }

            public Task<List<AcademyCourseScheduleRequestDto>> ListAsync(
                int? studentId = null, int? scheduleId = null, int? courseId = null,
                string status = null, DateTime? fromDate = null, DateTime? toDate = null)
            {
                var q = Items.AsEnumerable();
                if (studentId.HasValue) q = q.Where(x => x.StudentId == studentId.Value);
                if (scheduleId.HasValue) q = q.Where(x => x.ScheduleId == scheduleId.Value);
                if (courseId.HasValue) q = q.Where(x => x.CourseId == courseId.Value);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                if (fromDate.HasValue) q = q.Where(x => x.SessionDate >= fromDate.Value.Date);
                if (toDate.HasValue) q = q.Where(x => x.SessionDate <= toDate.Value.Date);
                return Task.FromResult(q.ToList());
            }

            public Task<AcademyCourseScheduleRequestDto> GetByIdAsync(int id)
                => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

            public Task<int> InsertAsync(int scheduleId, int studentId, DateTime sessionDate, string studentNote, string status = null)
            {
                if (Items.Any(x => x.ScheduleId == scheduleId && x.StudentId == studentId && x.SessionDate.Date == sessionDate.Date))
                    return Task.FromResult(-1);
                var item = new AcademyCourseScheduleRequestDto
                {
                    Id = Items.Count + 1,
                    ScheduleId = scheduleId,
                    StudentId = studentId,
                    SessionDate = sessionDate.Date,
                    Status = string.Equals(status, AcademyConstants.RequestStatus.Waitlisted, StringComparison.OrdinalIgnoreCase)
                        ? AcademyConstants.RequestStatus.Waitlisted
                        : AcademyConstants.RequestStatus.Pending,
                    StudentNote = studentNote,
                    CourseId = 5,
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _ = ForceStatusOnInsert;
                Items.Add(item);
                return Task.FromResult(item.Id);
            }

            public Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(
                int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
            {
                var q = Items.Where(x => x.CourseInstructorId == instructorId);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                return Task.FromResult(q.ToList());
            }

            public Task<int> ReviewAsync(ReviewAcademyCourseScheduleRequestDto dto) => Task.FromResult(0);

            public Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId)
                => Task.FromResult(new List<AcademyCourseScheduleRequestAuditDto>());

            public Task<int> CountApprovedAsync(int scheduleId, DateTime sessionDate)
                => Task.FromResult(Items.Count(x => x.ScheduleId == scheduleId && x.SessionDate.Date == sessionDate.Date
                    && x.Status == AcademyConstants.RequestStatus.Approved));

            public Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime fromDate, DateTime toDate)
                => Task.FromResult(Items
                    .Where(x => x.ScheduleId == scheduleId && x.Status == AcademyConstants.RequestStatus.Approved
                        && x.SessionDate.Date >= fromDate.Date && x.SessionDate.Date <= toDate.Date)
                    .GroupBy(x => x.SessionDate.Date)
                    .Select(g => new AcademyDailyApprovedCountDto { ScheduleId = scheduleId, SessionDate = g.Key, ApprovedCount = g.Count() })
                    .ToList());
        }

        sealed class FakeStudentRepo : IAcademyStudentRepository
        {
            public List<AcademyStudentDto> Items { get; } = new();
            public Task<List<AcademyStudentDto>> GetAllAsync(int? siteId = null, string status = null)
                => Task.FromResult(Items.ToList());
            public Task<AcademyStudentDto> GetByIdAsync(int id)
                => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
            public Task<AcademyStudentDto> GetByUserIdAsync(int userId)
                => Task.FromResult(Items.FirstOrDefault(x => x.UserId == userId));
            public Task<int> InsertAsync(int? siteId, int userId, string status, string notes, bool mustChangePassword)
                => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateAcademyStudentDto dto) => Task.FromResult(true);
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
