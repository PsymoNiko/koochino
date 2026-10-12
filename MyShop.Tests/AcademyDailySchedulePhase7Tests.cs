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
    public class AcademyDailySchedulePhase7Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public async Task MarkDaily_RequiresActor()
        {
            var ctx = Context();
            var r = await ctx.Attendance.MarkDailyAsync(Mark(7, Day1));
            Assert.False(r.Ok);
            Assert.Contains("مجاز", r.Error);
        }

        [Fact]
        public async Task Instructor_CanMarkApprovedStudent()
        {
            var ctx = Context();
            ctx.AttendanceRepo.Approved.Add((11, Day1, 7));

            var r = await ctx.Attendance.MarkDailyAsync(Mark(7, Day1), actingInstructorId: 3);

            Assert.True(r.Ok);
            Assert.Single(ctx.AttendanceRepo.Items);
            Assert.Equal(AcademyConstants.AttendanceStatus.Present, ctx.AttendanceRepo.Items[0].Status);
            Assert.Equal(11, ctx.AttendanceRepo.Items[0].ScheduleId);
            Assert.Null(ctx.AttendanceRepo.Items[0].CourseSessionId);
        }

        [Fact]
        public async Task MarkDaily_PendingStudent_Fails()
        {
            var ctx = Context();
            var r = await ctx.Attendance.MarkDailyAsync(Mark(7, Day1), actingInstructorId: 3);
            Assert.False(r.Ok);
            Assert.Contains("تأیید", r.Error);
            Assert.Empty(ctx.AttendanceRepo.Items);
        }

        [Fact]
        public async Task MarkDaily_CancelledDay_Fails()
        {
            var ctx = Context();
            ctx.AttendanceRepo.Approved.Add((11, Day1, 7));
            ctx.Exceptions.Items.Add(new AcademyScheduleExceptionDto
            {
                ScheduleId = 11,
                ExceptionDate = Day1,
                IsCancelled = true
            });

            var r = await ctx.Attendance.MarkDailyAsync(Mark(7, Day1), actingInstructorId: 3);
            Assert.False(r.Ok);
            Assert.Contains("لغو", r.Error);
        }

        [Fact]
        public async Task MarkDaily_Day1_DoesNotAffectDay2()
        {
            var ctx = Context();
            ctx.AttendanceRepo.Approved.Add((11, Day1, 7));
            ctx.AttendanceRepo.Approved.Add((11, Day2, 7));

            Assert.True((await ctx.Attendance.MarkDailyAsync(Mark(7, Day1, AcademyConstants.AttendanceStatus.Absent), actingInstructorId: 3)).Ok);
            Assert.True((await ctx.Attendance.MarkDailyAsync(Mark(7, Day2), actingInstructorId: 3)).Ok);

            Assert.Equal(2, ctx.AttendanceRepo.Items.Count);
            Assert.Equal(AcademyConstants.AttendanceStatus.Absent, ctx.AttendanceRepo.Items.Single(x => x.SessionDate == Day1).Status);
            Assert.Equal(AcademyConstants.AttendanceStatus.Present, ctx.AttendanceRepo.Items.Single(x => x.SessionDate == Day2).Status);
        }

        [Fact]
        public async Task Instructor_Idor_OtherCourse_Fails()
        {
            var ctx = Context();
            ctx.AttendanceRepo.Approved.Add((11, Day1, 7));

            var r = await ctx.Attendance.MarkDailyAsync(Mark(7, Day1), actingInstructorId: 99);
            Assert.False(r.Ok);
            Assert.Empty(ctx.AttendanceRepo.Items);
        }

        [Fact]
        public async Task Admin_CanMarkAnyCourse()
        {
            var ctx = Context();
            ctx.AttendanceRepo.Approved.Add((11, Day1, 7));
            var r = await ctx.Attendance.MarkDailyAsync(Mark(7, Day1), actingAdminUserId: 1);
            Assert.True(r.Ok);
        }

        [Fact]
        public async Task Roster_Idor_IsEmptyForOtherInstructor()
        {
            var ctx = Context();
            ctx.AttendanceRepo.Roster.Add(new AcademyAttendanceRosterItemDto { StudentId = 7, ScheduleId = 11 });

            var own = await ctx.Attendance.GetDailyRosterAsync(11, Day1, actingInstructorId: 3);
            var other = await ctx.Attendance.GetDailyRosterAsync(11, Day1, actingInstructorId: 99);

            Assert.Single(own);
            Assert.Empty(other);
        }

        [Fact]
        public async Task CourseAttendance_StillRequiresPhysicalSession()
        {
            var ctx = Context();
            var r = await ctx.Attendance.MarkAsync(new UpsertAcademyAttendanceDto
            {
                StudentId = 7,
                Status = AcademyConstants.AttendanceStatus.Present,
                ScheduleId = 11,
                SessionDate = Day1
            }, actingAdminUserId: 1);
            Assert.False(r.Ok);
            Assert.Contains("جلسه", r.Error);
        }

        [Fact]
        public async Task SubstituteInstructor_CanMarkExceptionDay()
        {
            var ctx = Context();
            ctx.AttendanceRepo.Approved.Add((11, Day1, 7));
            ctx.Exceptions.Items.Add(new AcademyScheduleExceptionDto
            {
                ScheduleId = 11,
                ExceptionDate = Day1,
                InstructorId = 44
            });

            var r = await ctx.Attendance.MarkDailyAsync(Mark(7, Day1), actingInstructorId: 44);
            Assert.True(r.Ok);
        }

        static UpsertAcademyAttendanceDto Mark(int studentId, DateTime date, string status = null) =>
            new()
            {
                ScheduleId = 11,
                SessionDate = date,
                StudentId = studentId,
                Status = status ?? AcademyConstants.AttendanceStatus.Present
            };

        static TestContext Context()
        {
            var courses = new FakeCourseRepo
            {
                Course = new AcademyCourseDto { Id = 5, IsActive = true, InstructorId = 3, Title = "ریاضی" },
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
            var attendance = new FakeAttendanceRepo();
            var exceptions = new FakeExceptionRepo();
            return new TestContext
            {
                Courses = courses,
                AttendanceRepo = attendance,
                Exceptions = exceptions,
                Attendance = new AcademyAttendanceService(
                    attendance, courses, new FakeActivitySessions(), new FakePoints(), exceptions)
            };
        }

        sealed class TestContext
        {
            public FakeCourseRepo Courses { get; set; }
            public FakeAttendanceRepo AttendanceRepo { get; set; }
            public FakeExceptionRepo Exceptions { get; set; }
            public AcademyAttendanceService Attendance { get; set; }
        }

        sealed class FakeAttendanceRepo : IAcademyAttendanceRepository
        {
            public List<AcademyAttendanceDto> Items { get; } = new();
            public List<AcademyAttendanceRosterItemDto> Roster { get; } = new();
            public HashSet<(int ScheduleId, DateTime Date, int StudentId)> Approved { get; } = new();

            public Task<List<AcademyAttendanceRosterItemDto>> GetCourseSessionRosterAsync(int courseSessionId)
                => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
            public Task<List<AcademyAttendanceRosterItemDto>> GetActivitySessionRosterAsync(int activitySessionId)
                => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
            public Task<List<AcademyAttendanceRosterItemDto>> GetDailyRosterAsync(int scheduleId, DateTime sessionDate)
                => Task.FromResult(Roster.Where(x => x.ScheduleId == scheduleId).ToList());
            public Task<List<AcademyAttendanceDto>> GetAllAsync(int? courseSessionId = null, int? activitySessionId = null, int? studentId = null)
                => Task.FromResult(Items.ToList());

            public Task<int> UpsertAsync(UpsertAcademyAttendanceDto dto) => Task.FromResult(0);

            public Task<int> UpsertDailyAsync(UpsertAcademyAttendanceDto dto)
            {
                if (!Approved.Contains((dto.ScheduleId.GetValueOrDefault(), dto.SessionDate.GetValueOrDefault().Date, dto.StudentId)))
                    return Task.FromResult(-6);
                var existing = Items.FirstOrDefault(x =>
                    x.ScheduleId == dto.ScheduleId && x.SessionDate == dto.SessionDate?.Date && x.StudentId == dto.StudentId);
                if (existing == null)
                {
                    existing = new AcademyAttendanceDto { Id = Items.Count + 1 };
                    Items.Add(existing);
                }
                existing.ScheduleId = dto.ScheduleId;
                existing.SessionDate = dto.SessionDate?.Date;
                existing.StudentId = dto.StudentId;
                existing.Status = dto.Status;
                existing.CourseSessionId = null;
                existing.ActivitySessionId = null;
                return Task.FromResult(existing.Id);
            }

            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
            public Task<int> CreateQrTokenAsync(string sessionType, int sessionId, string token, DateTime expiresAt, int? instructorId, int? adminUserId)
                => Task.FromResult(0);
            public Task<AcademyAttendanceQrTokenDto> GetQrByTokenAsync(string token) => Task.FromResult<AcademyAttendanceQrTokenDto>(null);
            public Task<AcademyAttendanceQrTokenDto> GetActiveQrAsync(string sessionType, int sessionId)
                => Task.FromResult<AcademyAttendanceQrTokenDto>(null);
        }

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

        sealed class FakeActivitySessions : IAcademyActivitySessionRepository
        {
            public Task<List<AcademyActivitySessionDto>> GetAllAsync(int? activityId = null, int? instructorId = null, string status = null)
                => Task.FromResult(new List<AcademyActivitySessionDto>());
            public Task<AcademyActivitySessionDto> GetByIdAsync(int id) => Task.FromResult<AcademyActivitySessionDto>(null);
            public Task<int> InsertAsync(CreateAcademyActivitySessionDto dto) => Task.FromResult(0);
            public Task<bool> UpdateAsync(UpdateAcademyActivitySessionDto dto) => Task.FromResult(true);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        }

        sealed class FakePoints : IAcademyPointsService
        {
            public Task AwardOnceAsync(int studentId, string reason, string relatedType, int relatedId, int points)
                => Task.CompletedTask;
            public Task<int> GetBalanceAsync(int studentId) => Task.FromResult(0);
            public Task<List<AcademyPointEntryDto>> GetLedgerAsync(int studentId, int top = 100)
                => Task.FromResult(new List<AcademyPointEntryDto>());
            public Task<List<AcademyPointEntryDto>> GetLeaderboardAsync(int top = 20)
                => Task.FromResult(new List<AcademyPointEntryDto>());
        }
    }
}
