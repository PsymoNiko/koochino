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
    public class AcademyDailySchedulePhase13Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public async Task FullDay_CreatesWaitlisted_NotPending()
        {
            var ctx = Context(capacity: 1);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var first = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            await ctx.Service.ReviewAsync(Approve(first.Id), actingInstructorId: 3);

            var second = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);

            Assert.True(second.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Waitlisted, ctx.Requests.Items.Single(x => x.Id == second.Id).Status);
            Assert.Contains("انتظار", second.Message);
        }

        [Fact]
        public async Task Day1Waitlist_DoesNotWaitlistDay2()
        {
            var ctx = Context(capacity: 1);
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3);
            var b = await ctx.Service.CreateAsync(NewDto(7, Day2), 7);
            Assert.True(b.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single(x => x.Id == b.Id).Status);
        }

        [Fact]
        public async Task ApproveWaitlisted_FailsWhileStillFull()
        {
            var ctx = Context(capacity: 1);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3);
            var b = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);

            var r = await ctx.Service.ReviewAsync(Approve(b.Id), actingInstructorId: 3);
            Assert.False(r.Ok);
            Assert.Contains("ظرفیت", r.Error);
            Assert.Equal(AcademyConstants.RequestStatus.Waitlisted, ctx.Requests.Items.Single(x => x.Id == b.Id).Status);
        }

        [Fact]
        public async Task ApproveWaitlisted_SucceedsAfterSlotFrees()
        {
            var ctx = Context(capacity: 1);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3);
            var b = await ctx.Service.CreateAsync(NewDto(8, Day1), 8);

            ctx.Requests.Items.Single(x => x.Id == a.Id).Status = AcademyConstants.RequestStatus.Rejected;
            var r = await ctx.Service.ReviewAsync(Approve(b.Id), actingInstructorId: 3);
            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single(x => x.Id == b.Id).Status);
        }

        [Fact]
        public async Task Instructor_CanMovePendingToWaitlist()
        {
            var ctx = Context(capacity: 3);
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var r = await ctx.Service.ReviewAsync(new ReviewAcademyCourseScheduleRequestDto
            {
                Id = created.Id,
                Status = AcademyConstants.RequestStatus.Waitlisted
            }, actingInstructorId: 3);
            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Waitlisted, ctx.Requests.Items.Single().Status);
        }

        [Fact]
        public async Task Student_CannotApproveViaCreate()
        {
            Assert.Null(typeof(CreateAcademyCourseScheduleRequestDto).GetProperty("Status"));
            var ctx = Context(capacity: 1);
            var r = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single().Status);
            Assert.True(r.Ok);
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
                Requests = requests,
                Students = students,
                Service = new AcademyCourseScheduleRequestService(requests, courses, students)
            };
        }

        sealed class TestContext
        {
            public FakeRequestRepo Requests { get; set; }
            public FakeStudentRepo Students { get; set; }
            public AcademyCourseScheduleRequestService Service { get; set; }
        }

        sealed class FakeRequestRepo : IAcademyCourseScheduleRequestRepository
        {
            public List<AcademyCourseScheduleRequestDto> Items { get; } = new();
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
                    CourseInstructorId = 3
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
                item.Status = dto.Status;
                item.ReviewedByInstructorId = dto.ReviewedByInstructorId;
                return Task.FromResult(1);
            }
            public Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId)
                => Task.FromResult(new List<AcademyCourseScheduleRequestAuditDto>());
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
