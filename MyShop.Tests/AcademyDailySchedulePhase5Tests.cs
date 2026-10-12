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
    public class AcademyDailySchedulePhase5Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public void Helper_NullCapacity_IsNotFull()
        {
            Assert.False(AcademyDailyCapacity.IsLimited(null));
            Assert.False(AcademyDailyCapacity.IsFull(null, 100));
            Assert.False(AcademyDailyCapacity.IsFull(0, 1));
            Assert.True(AcademyDailyCapacity.IsFull(1, 1));
            Assert.False(AcademyDailyCapacity.IsFull(2, 1));
        }

        [Fact]
        public void Resolver_CopiesScheduleCapacity()
        {
            var daily = Daily(capacity: 3);
            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, Day1, out var occ));
            Assert.Equal(3, occ.Capacity);
            Assert.Equal(0, occ.ApprovedCount);
            Assert.False(occ.IsFull);
        }

        [Fact]
        public async Task Create_WhenApprovedAtCapacity_WaitlistsInsteadOfRejecting()
        {
            var ctx = Context(capacity: 1);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var first = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            Assert.True(first.Ok);
            await ctx.Service.ReviewAsync(Approve(first.Id), actingInstructorId: 3);

            var second = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);

            Assert.True(second.Ok);
            Assert.Contains("انتظار", second.Message);
            Assert.Equal(2, ctx.Requests.Items.Count);
            Assert.Equal(AcademyConstants.RequestStatus.Waitlisted, ctx.Requests.Items.Single(x => x.Id == second.Id).Status);
        }

        [Fact]
        public async Task Create_Day1Full_DoesNotBlockDay2()
        {
            var ctx = Context(capacity: 1);
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3);

            var b = await ctx.Service.CreateAsync(NewDto(7, Day2), 7);
            Assert.True(b.Ok);
            Assert.Equal(2, ctx.Requests.Items.Count);
        }

        [Fact]
        public async Task Create_NullCapacity_AllowsManyApproved()
        {
            var ctx = Context(capacity: null);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);
            Assert.True((await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3)).Ok);
            Assert.True((await ctx.Service.ReviewAsync(Approve(b.Id), actingInstructorId: 3)).Ok);
        }

        [Fact]
        public async Task Create_IgnoresCourseEnrollmentCapacity()
        {
            var ctx = Context(capacity: null);
            ctx.Courses.Course.Capacity = 1;
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);
            Assert.True(a.Ok);
            Assert.True(b.Ok);
        }

        [Fact]
        public async Task Approve_WhenAlreadyFull_FailsSecondPending()
        {
            var ctx = Context(capacity: 1);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);
            Assert.True((await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3)).Ok);

            var second = await ctx.Service.ReviewAsync(Approve(b.Id), actingInstructorId: 3);
            Assert.False(second.Ok);
            Assert.Contains("ظرفیت", second.Error);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single(x => x.Id == b.Id).Status);
        }

        [Fact]
        public async Task Reject_IsAllowedWhenFull()
        {
            var ctx = Context(capacity: 1);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);
            await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3);

            var r = await ctx.Service.ReviewAsync(new ReviewAcademyCourseScheduleRequestDto
            {
                Id = b.Id,
                Status = AcademyConstants.RequestStatus.Rejected,
                ReviewerNote = "ظرفیت پر است"
            }, actingInstructorId: 3);

            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Rejected, ctx.Requests.Items.Single(x => x.Id == b.Id).Status);
        }

        static CreateAcademyCourseScheduleRequestDto NewDto(int studentId, DateTime date) =>
            new() { ScheduleId = 11, StudentId = studentId, SessionDate = date };

        static ReviewAcademyCourseScheduleRequestDto Approve(int id) =>
            new() { Id = id, Status = AcademyConstants.RequestStatus.Approved };

        static AcademyCourseScheduleDto Daily(int? capacity) =>
            new()
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                StartDate = Day1,
                EndDate = new DateTime(2026, 8, 29),
                Capacity = capacity
            };

        static TestContext Context(int? capacity)
        {
            var courses = new FakeCourseRepo
            {
                Course = new AcademyCourseDto { Id = 5, IsActive = true, Title = "ریاضی", InstructorId = 3, Capacity = 99 },
                Schedule = Daily(capacity)
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
            public List<AcademyCourseScheduleRequestAuditDto> Audits { get; } = new();

            public Task<List<AcademyCourseScheduleRequestDto>> ListAsync(
                int? studentId = null, int? scheduleId = null, int? courseId = null,
                string status = null, DateTime? fromDate = null, DateTime? toDate = null)
                => Task.FromResult(Items.ToList());

            public Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(
                int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
                => Task.FromResult(Items.Where(x => x.CourseInstructorId == instructorId).ToList());

            public Task<AcademyCourseScheduleRequestDto> GetByIdAsync(int id)
                => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

            public Task<int> InsertAsync(int scheduleId, int studentId, DateTime sessionDate, string studentNote, string status = null)
            {
                var item = new AcademyCourseScheduleRequestDto
                {
                    Id = Items.Count + 1,
                    ScheduleId = scheduleId,
                    StudentId = studentId,
                    SessionDate = sessionDate.Date,
                    Status = string.Equals(status, AcademyConstants.RequestStatus.Waitlisted, StringComparison.OrdinalIgnoreCase)
                        ? AcademyConstants.RequestStatus.Waitlisted
                        : AcademyConstants.RequestStatus.Pending,
                    CourseId = 5,
                    CourseInstructorId = 3,
                    CreatedAt = DateTime.UtcNow
                };
                Items.Add(item);
                return Task.FromResult(item.Id);
            }

            public Task<int> ReviewAsync(ReviewAcademyCourseScheduleRequestDto dto)
            {
                var item = Items.FirstOrDefault(x => x.Id == dto.Id);
                if (item == null) return Task.FromResult(0);
                if (item.Status != AcademyConstants.RequestStatus.Pending
                    && item.Status != AcademyConstants.RequestStatus.Waitlisted)
                    return Task.FromResult(-7);
                var old = item.Status;
                item.Status = dto.Status;
                item.ReviewerNote = dto.ReviewerNote;
                Audits.Add(new AcademyCourseScheduleRequestAuditDto
                {
                    Id = Audits.Count + 1,
                    RequestId = item.Id,
                    OldStatus = old,
                    NewStatus = dto.Status
                });
                return Task.FromResult(1);
            }

            public Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId)
                => Task.FromResult(Audits.Where(x => x.RequestId == requestId).ToList());

            public Task<int> CountApprovedAsync(int scheduleId, DateTime sessionDate)
                => Task.FromResult(Items.Count(x => x.ScheduleId == scheduleId && x.SessionDate.Date == sessionDate.Date
                    && x.Status == AcademyConstants.RequestStatus.Approved));

            public Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime fromDate, DateTime toDate)
                => Task.FromResult(new List<AcademyDailyApprovedCountDto>());
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
