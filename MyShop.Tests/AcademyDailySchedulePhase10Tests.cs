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
    public class AcademyDailySchedulePhase10Tests
    {
        static readonly DateTime Day1 = new(2026, 8, 23);
        static readonly DateTime Day2 = new(2026, 8, 24);

        [Fact]
        public async Task StudentBoard_HidesInactiveCourseAndCancelledDay()
        {
            var repo = new FakeCourseRepo();
            repo.Courses.Add(ActiveDaily(5, 11, "ریاضی"));
            repo.Courses.Add(new AcademyCourseDto
            {
                Id = 8,
                IsActive = false,
                Title = "غیرفعال",
                Schedules = new List<AcademyCourseScheduleDto> { Daily(33, 8) }
            });
            var exceptions = new FakeExceptionRepo();
            exceptions.Items.Add(new AcademyScheduleExceptionDto
            {
                ScheduleId = 11,
                ExceptionDate = Day1,
                IsCancelled = true
            });
            var svc = new AcademyCourseService(repo, exceptions);

            var all = await svc.ListDailyOccurrencesForStudentAsync(Day1, Day2);
            var one = await svc.ListDailyOccurrencesForStudentAsync(Day1, Day2, courseId: 5);
            var none = await svc.ListDailyOccurrencesForStudentAsync(Day1, Day2, courseId: 8);

            Assert.Single(all);
            Assert.Equal(Day2, all[0].SessionDate);
            Assert.Equal("ریاضی", all[0].CourseTitle);
            Assert.False(all[0].IsCancelled);
            Assert.Single(one);
            Assert.Empty(none);
        }

        [Fact]
        public async Task Hydrate_AttachesOwnStatusAndCapacity_NotOtherStudent()
        {
            var ctx = RequestContext();
            ctx.Requests.Items.Add(Req(1, 11, 7, Day1, AcademyConstants.RequestStatus.Pending));
            ctx.Requests.Items.Add(Req(2, 11, 99, Day1, AcademyConstants.RequestStatus.Approved));
            ctx.Requests.Items.Add(Req(3, 11, 7, Day2, AcademyConstants.RequestStatus.Rejected, "پر"));

            var days = new List<AcademyLogicalSessionDto>
            {
                new() { ScheduleId = 11, SessionDate = Day1, Capacity = 1 },
                new() { ScheduleId = 11, SessionDate = Day2, Capacity = 1 }
            };
            await ctx.Service.HydrateStudentBoardAsync(7, days, Day1, Day2);

            Assert.Equal(AcademyConstants.RequestStatus.Pending, days[0].MyRequestStatus);
            Assert.Equal(1, days[0].ApprovedCount);
            Assert.True(days[0].IsFull);
            Assert.Equal(AcademyConstants.RequestStatus.Rejected, days[1].MyRequestStatus);
            Assert.Equal("پر", days[1].MyReviewerNote);
            Assert.Equal(0, days[1].ApprovedCount);
            Assert.False(days[1].IsFull);
        }

        [Fact]
        public async Task ListMine_StatusFilter_IsOwnOnly()
        {
            var ctx = RequestContext();
            ctx.Requests.Items.Add(Req(1, 11, 7, Day1, AcademyConstants.RequestStatus.Approved));
            ctx.Requests.Items.Add(Req(2, 11, 7, Day2, AcademyConstants.RequestStatus.Pending));
            ctx.Requests.Items.Add(Req(3, 11, 99, Day1, AcademyConstants.RequestStatus.Approved));

            var approved = await ctx.Service.ListMineAsync(7, status: AcademyConstants.RequestStatus.Approved);
            var other = await ctx.Service.ListMineAsync(99, status: AcademyConstants.RequestStatus.Approved);
            var empty = await ctx.Service.ListMineAsync(0);

            Assert.Single(approved);
            Assert.Equal(Day1, approved[0].SessionDate);
            Assert.Single(other);
            Assert.Equal(99, other[0].StudentId);
            Assert.Empty(empty);
        }

        static AcademyCourseDto ActiveDaily(int courseId, int scheduleId, string title) =>
            new()
            {
                Id = courseId,
                IsActive = true,
                Title = title,
                Schedules = new List<AcademyCourseScheduleDto> { Daily(scheduleId, courseId) }
            };

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

        static AcademyCourseScheduleRequestDto Req(int id, int scheduleId, int studentId, DateTime date, string status, string note = null) =>
            new()
            {
                Id = id,
                ScheduleId = scheduleId,
                StudentId = studentId,
                SessionDate = date,
                Status = status,
                ReviewerNote = note,
                CourseId = 5
            };

        static (AcademyCourseScheduleRequestService Service, FakeRequestRepo Requests) RequestContext()
        {
            var requests = new FakeRequestRepo();
            var courses = new FakeCourseRepo();
            courses.Courses.Add(ActiveDaily(5, 11, "ریاضی"));
            var students = new FakeStudentRepo();
            students.Items.Add(new AcademyStudentDto { Id = 7, Status = AcademyConstants.StudentStatus.Active });
            var svc = new AcademyCourseScheduleRequestService(requests, courses, students);
            return (svc, requests);
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
                => Task.FromResult(0);
            public Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(
                int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
                => Task.FromResult(new List<AcademyCourseScheduleRequestDto>());
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

        sealed class FakeCourseRepo : IAcademyCourseRepository
        {
            public List<AcademyCourseDto> Courses { get; } = new();
            public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
            {
                var q = Courses.AsEnumerable();
                if (isActive.HasValue) q = q.Where(c => c.IsActive == isActive.Value);
                return Task.FromResult(q.ToList());
            }
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
