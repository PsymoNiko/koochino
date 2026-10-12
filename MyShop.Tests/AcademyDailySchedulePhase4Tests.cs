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
    public class AcademyDailySchedulePhase4Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public async Task Instructor_CanApproveOwnCourseRequest()
        {
            var ctx = Context();
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var r = await ctx.Service.ReviewAsync(Approve(created.Id), actingInstructorId: 3);

            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single().Status);
            Assert.Equal(3, ctx.Requests.Items.Single().ReviewedByInstructorId);
        }

        [Fact]
        public async Task Instructor_Idor_OtherCourse_Fails()
        {
            var ctx = Context();
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var r = await ctx.Service.ReviewAsync(Approve(created.Id), actingInstructorId: 99);

            Assert.False(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single().Status);
        }

        [Fact]
        public async Task Admin_CanApproveAnyCourse()
        {
            var ctx = Context();
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var r = await ctx.Service.ReviewAsync(Approve(created.Id), actingAdminUserId: 50);

            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single().Status);
            Assert.Equal(50, ctx.Requests.Items.Single().ReviewedByAdminUserId);
        }

        [Fact]
        public async Task Student_CannotReview()
        {
            var ctx = Context();
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var r = await ctx.Service.ReviewAsync(Approve(created.Id));

            Assert.False(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single().Status);
        }

        [Fact]
        public async Task Approve_Day1_DoesNotApproveDay2()
        {
            var ctx = Context();
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(NewDto(7, Day2), 7);
            var r = await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3);

            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single(x => x.Id == a.Id).Status);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single(x => x.Id == b.Id).Status);
        }

        [Fact]
        public async Task Review_WritesAudit()
        {
            var ctx = Context();
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            await ctx.Service.ReviewAsync(new ReviewAcademyCourseScheduleRequestDto
            {
                Id = created.Id,
                Status = AcademyConstants.RequestStatus.Rejected,
                ReviewerNote = "ظرفیت این روز پر است"
            }, actingInstructorId: 3);

            var audits = await ctx.Service.ListAuditsAsync(created.Id, actingInstructorId: 3);
            Assert.Single(audits);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, audits[0].OldStatus);
            Assert.Equal(AcademyConstants.RequestStatus.Rejected, audits[0].NewStatus);
            Assert.Equal("ظرفیت این روز پر است", audits[0].ReviewerNote);
            Assert.Equal(3, audits[0].ReviewedByInstructorId);
        }

        [Fact]
        public async Task Reject_WithoutNote_Fails()
        {
            var ctx = Context();
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var r = await ctx.Service.ReviewAsync(new ReviewAcademyCourseScheduleRequestDto
            {
                Id = created.Id,
                Status = AcademyConstants.RequestStatus.Rejected
            }, actingInstructorId: 3);

            Assert.False(r.Ok);
            Assert.Contains("دلیل", r.Error);
            Assert.Empty(ctx.Requests.Audits);
        }

        [Fact]
        public async Task Review_AlreadyReviewed_Fails()
        {
            var ctx = Context();
            var created = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            await ctx.Service.ReviewAsync(Approve(created.Id), actingInstructorId: 3);
            var second = await ctx.Service.ReviewAsync(new ReviewAcademyCourseScheduleRequestDto
            {
                Id = created.Id,
                Status = AcademyConstants.RequestStatus.Rejected,
                ReviewerNote = "بعداً"
            }, actingInstructorId: 3);

            Assert.False(second.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single().Status);
            Assert.Single(ctx.Requests.Audits);
        }

        [Fact]
        public async Task ListForInstructor_HidesOtherCourses()
        {
            var ctx = Context();
            await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            ctx.Requests.Items[0].CourseInstructorId = 3;

            var other = new AcademyCourseScheduleRequestDto
            {
                Id = 99,
                ScheduleId = 22,
                StudentId = 8,
                SessionDate = Day1,
                Status = AcademyConstants.RequestStatus.Pending,
                CourseId = 9,
                CourseInstructorId = 8
            };
            ctx.Requests.Items.Add(other);

            var mine = await ctx.Service.ListForInstructorAsync(3);
            Assert.Single(mine);
            Assert.Equal(7, mine[0].StudentId);

            var leaked = await ctx.Service.ListAuditsAsync(99, actingInstructorId: 3);
            Assert.Empty(leaked);
        }

        [Fact]
        public async Task Review_DoesNotCheckCapacity()
        {
            var ctx = Context();
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var a = await ctx.Service.CreateAsync(NewDto(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(new CreateAcademyCourseScheduleRequestDto
            {
                ScheduleId = 11,
                StudentId = 8,
                SessionDate = Day1
            }, 8);

            var r1 = await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3);
            var r2 = await ctx.Service.ReviewAsync(Approve(b.Id), actingInstructorId: 3);

            Assert.True(r1.Ok);
            Assert.True(r2.Ok);
            Assert.Equal(2, ctx.Requests.Items.Count(x => x.Status == AcademyConstants.RequestStatus.Approved));
        }

        static CreateAcademyCourseScheduleRequestDto NewDto(int studentId, DateTime date) =>
            new() { ScheduleId = 11, StudentId = studentId, SessionDate = date };

        static ReviewAcademyCourseScheduleRequestDto Approve(int id) =>
            new() { Id = id, Status = AcademyConstants.RequestStatus.Approved };

        static TestContext Context()
        {
            var courses = new FakeCourseRepo
            {
                Course = new AcademyCourseDto { Id = 5, IsActive = true, Title = "ریاضی", InstructorId = 3 },
                Schedule = new AcademyCourseScheduleDto
                {
                    Id = 11,
                    CourseId = 5,
                    RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                    Hour = 20,
                    StartDate = Day1,
                    EndDate = new DateTime(2026, 8, 29)
                }
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
            {
                var q = Items.AsEnumerable();
                if (studentId.HasValue) q = q.Where(x => x.StudentId == studentId.Value);
                if (courseId.HasValue) q = q.Where(x => x.CourseId == courseId.Value);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                return Task.FromResult(q.ToList());
            }

            public Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(
                int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
            {
                var q = Items.Where(x => x.CourseInstructorId == instructorId);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                return Task.FromResult(q.ToList());
            }

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
                    StudentNote = studentNote,
                    CourseId = 5,
                    CourseInstructorId = 3,
                    RequestedAt = DateTime.UtcNow,
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
                item.ReviewedByInstructorId = dto.ReviewedByInstructorId;
                item.ReviewedByAdminUserId = dto.ReviewedByAdminUserId;
                item.ReviewedAt = DateTime.UtcNow;
                Audits.Add(new AcademyCourseScheduleRequestAuditDto
                {
                    Id = Audits.Count + 1,
                    RequestId = item.Id,
                    OldStatus = old,
                    NewStatus = dto.Status,
                    ReviewerNote = dto.ReviewerNote,
                    ReviewedByInstructorId = dto.ReviewedByInstructorId,
                    ReviewedByAdminUserId = dto.ReviewedByAdminUserId,
                    CreatedAt = DateTime.UtcNow
                });
                return Task.FromResult(1);
            }

            public Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId)
                => Task.FromResult(Audits.Where(x => x.RequestId == requestId).ToList());

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
