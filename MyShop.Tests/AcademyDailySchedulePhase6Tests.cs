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
    public class AcademyDailySchedulePhase6Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);
        static readonly DateTime End = new(2026, 8, 29);

        [Fact]
        public void CancelledDay_IsExcludedFromListAndResolve()
        {
            var daily = Daily();
            var cancelled = Ex(Day1, cancelled: true);
            Assert.False(AcademyDailyScheduleResolver.TryResolve(daily, Day1, cancelled, out _));
            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, Day2, cancelled, out _));

            var list = AcademyDailyScheduleResolver.ListOccurrences(daily, Day1, Day2, new[] { cancelled });
            Assert.Single(list);
            Assert.Equal(Day2, list[0].SessionDate);
        }

        [Fact]
        public void TimeOverride_AppliesOnlyToThatDay()
        {
            var daily = Daily();
            var ex = Ex(Day1, hour: 18, minute: 30, duration: 45);
            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, Day1, ex, out var day1));
            Assert.Equal(18, day1.Hour);
            Assert.Equal(30, day1.Minute);
            Assert.Equal(45, day1.DurationMinutes);
            Assert.True(day1.HasException);

            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, Day2, ex, out var day2));
            Assert.Equal(20, day2.Hour);
            Assert.Equal(0, day2.Minute);
            Assert.False(day2.HasException);
        }

        [Fact]
        public void CapacityOverride_IsIndependentPerDay()
        {
            var daily = Daily(capacity: 5);
            var ex = Ex(Day1, capacity: 1);
            Assert.Equal(1, AcademyDailyCapacity.Effective(5, 1));
            Assert.True(AcademyDailyCapacity.IsFull(AcademyDailyCapacity.Effective(5, 1), 1));
            Assert.False(AcademyDailyCapacity.IsFull(AcademyDailyCapacity.Effective(5, null), 1));

            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, Day1, ex, out var day1));
            Assert.Equal(1, day1.Capacity);
            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, Day2, ex, out var day2));
            Assert.Equal(5, day2.Capacity);
        }

        [Fact]
        public void NullOverrideFields_InheritFromSchedule()
        {
            var daily = Daily(capacity: 3);
            var ex = Ex(Day1);
            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, Day1, ex, out var occ));
            Assert.Equal(20, occ.Hour);
            Assert.Equal(3, occ.Capacity);
            Assert.Null(occ.InstructorId);
        }

        [Fact]
        public void Weekly_IsStillIgnored()
        {
            var weekly = new AcademyCourseScheduleDto
            {
                Id = 9,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly,
                DayId = 1,
                Hour = 10,
                StartDate = Day1,
                EndDate = End
            };
            var ex = new AcademyScheduleExceptionDto
            {
                ScheduleId = 9,
                ExceptionDate = Day1,
                IsCancelled = true
            };
            Assert.False(AcademyDailyScheduleResolver.TryResolve(weekly, Day1, ex, out _));
            Assert.Empty(AcademyDailyScheduleResolver.ListOccurrences(weekly, Day1, Day2, new[] { ex }));
        }

        [Fact]
        public async Task Create_OnCancelledDay_Fails()
        {
            var ctx = Context();
            ctx.Exceptions.Items.Add(Ex(Day1, cancelled: true));

            var r = await ctx.Requests.CreateAsync(NewDto(7, Day1), 7);

            Assert.False(r.Ok);
            Assert.Contains("لغو", r.Error);
            Assert.Empty(ctx.RequestRepo.Items);
        }

        [Fact]
        public async Task Create_Day2_IsIndependentOfDay1Cancel()
        {
            var ctx = Context();
            ctx.Exceptions.Items.Add(Ex(Day1, cancelled: true));

            var r = await ctx.Requests.CreateAsync(NewDto(7, Day2), 7);
            Assert.True(r.Ok);
            Assert.Single(ctx.RequestRepo.Items);
        }

        [Fact]
        public async Task Approve_UsesExceptionCapacity()
        {
            var ctx = Context(scheduleCapacity: 5);
            ctx.Exceptions.Items.Add(Ex(Day1, capacity: 1));
            ctx.Students.Items.Add(new AcademyStudentDto { Id = 8, Status = AcademyConstants.StudentStatus.Active });

            var a = await ctx.Requests.CreateAsync(NewDto(7, Day1), 7);
            Assert.True((await ctx.Requests.ReviewAsync(Approve(a.Id), actingInstructorId: 3)).Ok);

            var waitlisted = await ctx.Requests.CreateAsync(NewDto(8, Day1), 8);
            Assert.True(waitlisted.Ok);
            Assert.Equal(AcademyConstants.RequestStatus.Waitlisted, ctx.RequestRepo.Items.Single(x => x.Id == waitlisted.Id).Status);

            var stillFull = await ctx.Requests.ReviewAsync(Approve(waitlisted.Id), actingInstructorId: 3);
            Assert.False(stillFull.Ok);
            Assert.Contains("ظرفیت", stillFull.Error);

            ctx.Exceptions.Items.Clear();
            var second = await ctx.Requests.ReviewAsync(Approve(waitlisted.Id), actingInstructorId: 3);
            Assert.True(second.Ok);
        }

        [Fact]
        public async Task Instructor_Idor_CannotUpsertOtherCourseException()
        {
            var ctx = Context();
            var svc = new AcademyScheduleExceptionService(ctx.Exceptions, ctx.Courses);

            var denied = await svc.UpsertAsync(new UpsertAcademyScheduleExceptionDto
            {
                ScheduleId = 11,
                ExceptionDate = Day1,
                IsCancelled = true
            }, actingInstructorId: 99);
            Assert.False(denied.Ok);

            var own = await svc.UpsertAsync(new UpsertAcademyScheduleExceptionDto
            {
                ScheduleId = 11,
                ExceptionDate = Day1,
                IsCancelled = true
            }, actingInstructorId: 3);
            Assert.True(own.Ok);
            Assert.Single(ctx.Exceptions.Items);
        }

        [Fact]
        public async Task CourseService_List_SkipsCancelledException()
        {
            var courses = new FakeCourseRepo
            {
                Course = new AcademyCourseDto
                {
                    Id = 5,
                    IsActive = true,
                    Schedules = new List<AcademyCourseScheduleDto> { Daily() }
                },
                Schedule = Daily()
            };
            var exceptions = new FakeExceptionRepo();
            exceptions.Items.Add(Ex(Day1, cancelled: true));
            var svc = new AcademyCourseService(courses, exceptions);

            var list = await svc.ListDailyOccurrencesAsync(5, Day1, Day2);
            Assert.Single(list);
            Assert.Equal(Day2, list[0].SessionDate);
        }

        static CreateAcademyCourseScheduleRequestDto NewDto(int studentId, DateTime date) =>
            new() { ScheduleId = 11, StudentId = studentId, SessionDate = date };

        static ReviewAcademyCourseScheduleRequestDto Approve(int id) =>
            new() { Id = id, Status = AcademyConstants.RequestStatus.Approved };

        static AcademyCourseScheduleDto Daily(int? capacity = null) =>
            new()
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                Minute = 0,
                DurationMinutes = 90,
                StartDate = Day1,
                EndDate = End,
                Capacity = capacity
            };

        static AcademyScheduleExceptionDto Ex(
            DateTime date, bool cancelled = false, int? hour = null, int? minute = null,
            int? duration = null, int? capacity = null, int? instructorId = null) =>
            new()
            {
                ScheduleId = 11,
                ExceptionDate = date,
                IsCancelled = cancelled,
                Hour = hour,
                Minute = minute,
                DurationMinutes = duration,
                Capacity = capacity,
                InstructorId = instructorId
            };

        static TestContext Context(int? scheduleCapacity = null)
        {
            var courses = new FakeCourseRepo
            {
                Course = new AcademyCourseDto { Id = 5, IsActive = true, Title = "ریاضی", InstructorId = 3, Capacity = 99 },
                Schedule = Daily(scheduleCapacity)
            };
            var students = new FakeStudentRepo();
            students.Items.Add(new AcademyStudentDto { Id = 7, Status = AcademyConstants.StudentStatus.Active });
            var requests = new FakeRequestRepo();
            var exceptions = new FakeExceptionRepo();
            return new TestContext
            {
                Courses = courses,
                Students = students,
                RequestRepo = requests,
                Exceptions = exceptions,
                Requests = new AcademyCourseScheduleRequestService(requests, courses, students, exceptions)
            };
        }

        sealed class TestContext
        {
            public FakeCourseRepo Courses { get; set; }
            public FakeStudentRepo Students { get; set; }
            public FakeRequestRepo RequestRepo { get; set; }
            public FakeExceptionRepo Exceptions { get; set; }
            public AcademyCourseScheduleRequestService Requests { get; set; }
        }

        sealed class FakeExceptionRepo : IAcademyScheduleExceptionRepository
        {
            public List<AcademyScheduleExceptionDto> Items { get; } = new();

            public Task<List<AcademyScheduleExceptionDto>> ListAsync(
                int? scheduleId = null, int? courseId = null, DateTime? fromDate = null, DateTime? toDate = null)
            {
                var q = Items.AsEnumerable();
                if (scheduleId.HasValue) q = q.Where(x => x.ScheduleId == scheduleId.Value);
                if (fromDate.HasValue) q = q.Where(x => x.ExceptionDate.Date >= fromDate.Value.Date);
                if (toDate.HasValue) q = q.Where(x => x.ExceptionDate.Date <= toDate.Value.Date);
                return Task.FromResult(q.ToList());
            }

            public Task<AcademyScheduleExceptionDto> GetByIdAsync(int id)
                => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

            public Task<AcademyScheduleExceptionDto> GetByScheduleDateAsync(int scheduleId, DateTime exceptionDate)
                => Task.FromResult(Items.FirstOrDefault(x => x.ScheduleId == scheduleId && x.ExceptionDate.Date == exceptionDate.Date));

            public Task<int> UpsertAsync(UpsertAcademyScheduleExceptionDto dto)
            {
                var existing = Items.FirstOrDefault(x => x.ScheduleId == dto.ScheduleId && x.ExceptionDate.Date == dto.ExceptionDate.Date);
                if (existing == null)
                {
                    existing = new AcademyScheduleExceptionDto { Id = Items.Count + 1, CourseId = 5, CourseInstructorId = 3 };
                    Items.Add(existing);
                }
                existing.ScheduleId = dto.ScheduleId;
                existing.ExceptionDate = dto.ExceptionDate.Date;
                existing.IsCancelled = dto.IsCancelled;
                existing.Hour = dto.Hour;
                existing.Minute = dto.Minute;
                existing.DurationMinutes = dto.DurationMinutes;
                existing.Capacity = dto.Capacity;
                existing.InstructorId = dto.InstructorId;
                existing.Note = dto.Note;
                return Task.FromResult(existing.Id);
            }

            public Task<bool> DeleteAsync(int id)
            {
                var n = Items.RemoveAll(x => x.Id == id);
                return Task.FromResult(n > 0);
            }
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
                item.ReviewerNote = dto.ReviewerNote;
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
