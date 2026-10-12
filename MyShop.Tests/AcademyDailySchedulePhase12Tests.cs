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
    /// <summary>تست‌های تجمیعی ۱–۱۶ معماری Daily (Schedule منطقی، نه جلسه فیزیکی).</summary>
    public class AcademyDailySchedulePhase12Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public void T01_Weekly_RemainsWeekly_AndIsNotDaily()
        {
            var weekly = new AcademyCourseScheduleDto
            {
                DayId = 1,
                Hour = 20,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly
            };
            Assert.True(weekly.IsWeekly);
            Assert.False(weekly.IsDaily);
            Assert.Empty(AcademyDailyScheduleResolver.ListOccurrences(weekly, Day1, Day2));
        }

        [Fact]
        public void T02_Daily_DoesNotCreate360PhysicalSessions()
        {
            var daily = Daily(capacity: 10);
            daily.EndDate = new DateTime(2027, 8, 17);
            var list = AcademyDailyScheduleResolver.ListOccurrences(daily, Day1, daily.EndDate.Value);
            Assert.True(list.Count <= AcademyDailyScheduleResolver.MaxListedDaysPerSchedule);
            Assert.All(list, x => Assert.Equal(11, x.ScheduleId));
            Assert.All(list, x => Assert.True(x.SessionDate.Date >= Day1.Date));
        }

        [Fact]
        public void T03_LogicalKey_IsScheduleIdPlusSessionDate()
        {
            Assert.True(AcademyDailyScheduleResolver.TryResolve(Daily(), Day1, out var occ));
            Assert.Equal(11, occ.ScheduleId);
            Assert.Equal(Day1, occ.SessionDate);
            Assert.Equal(AcademyConstants.ScheduleRecurrenceType.Daily, occ.RecurrenceType);
        }

        [Fact]
        public async Task T04_Request_DoesNotRequireEnrollment()
        {
            var ctx = RequestCtx();
            var r = await ctx.Service.CreateAsync(NewReq(7, Day1), 7);
            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single().Status);
        }

        [Fact]
        public async Task T05_Unique_SameDaySameStudent_Fails()
        {
            var ctx = RequestCtx();
            Assert.True((await ctx.Service.CreateAsync(NewReq(7, Day1), 7)).Ok);
            var dup = await ctx.Service.CreateAsync(NewReq(7, Day1), 7);
            Assert.False(dup.Ok);
            Assert.Single(ctx.Requests.Items);
        }

        [Fact]
        public async Task T06_Day1AndDay2_AreIndependent()
        {
            var ctx = RequestCtx();
            var a = await ctx.Service.CreateAsync(NewReq(7, Day1), 7);
            var b = await ctx.Service.CreateAsync(NewReq(7, Day2), 7);
            Assert.True(a.Ok && b.Ok);
            Assert.True((await ctx.Service.ReviewAsync(Approve(a.Id), actingInstructorId: 3)).Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single(x => x.Id == a.Id).Status);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single(x => x.Id == b.Id).Status);
        }

        [Fact]
        public async Task T07_Student_CannotApproveOwnRequest()
        {
            var ctx = RequestCtx();
            var created = await ctx.Service.CreateAsync(NewReq(7, Day1), 7);
            var asStudent = await ctx.Service.ReviewAsync(Approve(created.Id));
            Assert.False(asStudent.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single().Status);
            Assert.Null(typeof(CreateAcademyCourseScheduleRequestDto).GetProperty("Status"));
        }

        [Fact]
        public async Task T08_Instructor_OnlyOwnCourse_IdorBlocked()
        {
            var ctx = RequestCtx();
            var created = await ctx.Service.CreateAsync(NewReq(7, Day1), 7);
            var other = await ctx.Service.ReviewAsync(Approve(created.Id), actingInstructorId: 99);
            Assert.False(other.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Pending, ctx.Requests.Items.Single().Status);
        }

        [Fact]
        public async Task T09_Admin_CanReviewAnyCourse()
        {
            var ctx = RequestCtx();
            var created = await ctx.Service.CreateAsync(NewReq(7, Day1), 7);
            var r = await ctx.Service.ReviewAsync(Approve(created.Id), actingAdminUserId: 50);
            Assert.True(r.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single().Status);
            Assert.Equal(50, ctx.Requests.Items.Single().ReviewedByAdminUserId);
        }

        [Fact]
        public void T10_DailyCapacity_IgnoresCourseEnrollmentCapacity()
        {
            Assert.False(AcademyDailyCapacity.IsFull(null, 999));
            Assert.True(AcademyDailyCapacity.IsFull(2, 2));
            var occ = new AcademyLogicalSessionDto { Capacity = 2, ApprovedCount = 2 };
            Assert.True(occ.IsFull);
        }

        [Fact]
        public async Task T11_FullDay_WaitlistsNewRequest_DoesNotCreatePendingQueue()
        {
            var ctx = RequestCtx(capacity: 1);
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });
            var first = await ctx.Service.CreateAsync(NewReq(7, Day1), 7);
            Assert.True((await ctx.Service.ReviewAsync(Approve(first.Id), actingInstructorId: 3)).Ok);
            var second = await ctx.Service.CreateAsync(NewReq(8, Day1), 8);
            Assert.True(second.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Waitlisted, ctx.Requests.Items.Single(x => x.Id == second.Id).Status);
            Assert.Equal(AcademyConstants.RequestStatus.Approved, ctx.Requests.Items.Single(x => x.Id == first.Id).Status);
        }

        [Fact]
        public void T12_CancelledDay_HiddenFromStudentList()
        {
            var cancelled = new AcademyScheduleExceptionDto { ScheduleId = 11, ExceptionDate = Day1, IsCancelled = true };
            var list = AcademyDailyScheduleResolver.ListOccurrences(Daily(), Day1, Day2, new[] { cancelled });
            Assert.Single(list);
            Assert.Equal(Day2, list[0].SessionDate);
        }

        [Fact]
        public void T13_ExceptionTime_AppliesOnlyToThatDay()
        {
            var ex = new AcademyScheduleExceptionDto { ScheduleId = 11, ExceptionDate = Day1, Hour = 18, Minute = 30 };
            Assert.True(AcademyDailyScheduleResolver.TryResolve(Daily(), Day1, ex, out var d1));
            Assert.True(AcademyDailyScheduleResolver.TryResolve(Daily(), Day2, ex, out var d2));
            Assert.Equal(18, d1.Hour);
            Assert.Equal(20, d2.Hour);
        }

        [Fact]
        public void T14_DailyAttendance_HasNoPhysicalCourseSession()
        {
            var row = new AcademyAttendanceDto
            {
                ScheduleId = 11,
                SessionDate = Day1,
                StudentId = 7,
                Status = AcademyConstants.AttendanceStatus.Present,
                CourseSessionId = null,
                ActivitySessionId = null
            };
            Assert.Null(row.CourseSessionId);
            Assert.Null(row.ActivitySessionId);
            Assert.Equal(11, row.ScheduleId);
            Assert.Equal(Day1, row.SessionDate);
        }

        [Fact]
        public void T15_DailyRoster_IsApprovedCountNotCourseEnrollment()
        {
            var roster = new List<AcademyAttendanceRosterItemDto>
            {
                new() { ScheduleId = 11, SessionDate = Day1, StudentId = 7 },
                new() { ScheduleId = 11, SessionDate = Day1, StudentId = 8 }
            };
            Assert.All(roster, x => Assert.Equal(11, x.ScheduleId));
            Assert.All(roster, x => Assert.Null(x.CourseSessionId));
            Assert.False(AcademyDailyCapacity.IsFull(null, roster.Count));
            Assert.True(AcademyDailyCapacity.IsFull(2, roster.Count));
        }

        [Fact]
        public void T16_Report_PerDateIndependent()
        {
            var rows = new[]
            {
                new AcademyDailyByDateReportDto { SessionDate = Day1, ApprovedCount = 1, PresentCount = 1 },
                new AcademyDailyByDateReportDto { SessionDate = Day2, ApprovedCount = 5, AbsentCount = 2 }
            };
            var all = AcademyDailyReportMath.Summarize(rows);
            var day1 = AcademyDailyReportMath.Summarize(rows.Where(x => x.SessionDate == Day1));
            Assert.Equal(6, all.ApprovedRequests);
            Assert.Equal(1, day1.ApprovedRequests);
            Assert.Equal(0, day1.AbsentCount);
        }

        static AcademyCourseScheduleDto Daily(int? capacity = 3) =>
            new()
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                DurationMinutes = 90,
                StartDate = Day1,
                EndDate = new DateTime(2026, 8, 29),
                Capacity = capacity
            };

        static CreateAcademyCourseScheduleRequestDto NewReq(int studentId, DateTime date) =>
            new() { ScheduleId = 11, StudentId = studentId, SessionDate = date };

        static ReviewAcademyCourseScheduleRequestDto Approve(int id) =>
            new() { Id = id, Status = AcademyConstants.RequestStatus.Approved };

        static TestContext RequestCtx(int? capacity = 3)
        {
            var courses = new FakeCourseRepo
            {
                Course = new AcademyCourseDto
                {
                    Id = 5,
                    IsActive = true,
                    InstructorId = 3,
                    Title = "ریاضی",
                    Capacity = 100,
                    Schedules = new List<AcademyCourseScheduleDto> { Daily(capacity) }
                },
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

        sealed class FakeRequestRepo : IAcademyCourseScheduleRequestRepository
        {
            public List<AcademyCourseScheduleRequestDto> Items { get; } = new();
            public Task<List<AcademyCourseScheduleRequestDto>> ListAsync(
                int? studentId = null, int? scheduleId = null, int? courseId = null,
                string status = null, DateTime? fromDate = null, DateTime? toDate = null)
            {
                var q = Items.AsEnumerable();
                if (studentId.HasValue) q = q.Where(x => x.StudentId == studentId.Value);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                return Task.FromResult(q.ToList());
            }
            public Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(
                int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
                => Task.FromResult(Items.Where(x => x.CourseInstructorId == instructorId).ToList());
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
                item.ReviewedByAdminUserId = dto.ReviewedByAdminUserId;
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

        sealed class FakeCourseRepo : IAcademyCourseRepository
        {
            public AcademyCourseDto Course { get; set; }
            public AcademyCourseScheduleDto Schedule { get; set; }
            public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
                => Task.FromResult(Course == null ? new() : new List<AcademyCourseDto> { Course });
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
