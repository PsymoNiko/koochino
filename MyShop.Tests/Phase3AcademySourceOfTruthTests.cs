using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Services.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using MyShop.Core.Routing;

namespace MyShop.Tests;

public class Phase3AcademySourceOfTruthTests
{
    private static readonly DateTime Day = new(2026, 8, 23);

    [Fact]
    public async Task EducationSportCulture_UseSameCategoryAndActivityEngine()
    {
        var categories = new CategoryRepo(
            new AcademyActivityCategoryDto { Id = 1, SiteId = 7, Name = "Education" },
            new AcademyActivityCategoryDto { Id = 2, SiteId = 7, Name = "Sport" },
            new AcademyActivityCategoryDto { Id = 3, SiteId = 7, Name = "Culture" });
        var activities = new ActivityRepo();
        var service = new AcademyActivityService(activities, new Tenant(7), categories);

        foreach (var categoryId in new[] { 1, 2, 3 })
        {
            var result = await service.CreateAsync(new CreateAcademyActivityDto
            {
                SiteId = 99,
                CategoryId = categoryId,
                Title = $"activity-{categoryId}"
            });
            Assert.True(result.Ok);
        }

        Assert.Equal(3, activities.Inserted.Count);
        Assert.All(activities.Inserted, x => Assert.Equal(7, x.SiteId));
        Assert.Equal(new[] { 1, 2, 3 }, activities.Inserted.Select(x => x.CategoryId));
    }

    [Fact]
    public async Task CategoryAndActivity_RejectCrossTenantCategoryChains()
    {
        var categories = new CategoryRepo(
            new AcademyActivityCategoryDto { Id = 1, SiteId = 7 },
            new AcademyActivityCategoryDto { Id = 2, SiteId = 8 });
        var categoryService = new AcademyActivityCategoryService(categories, new Tenant(7));
        var activities = new ActivityRepo();
        var activityService = new AcademyActivityService(activities, new Tenant(7), categories);

        var child = await categoryService.CreateAsync(new CreateAcademyActivityCategoryDto
        {
            ParentId = 2,
            Name = "foreign-child"
        });
        var activity = await activityService.CreateAsync(new CreateAcademyActivityDto
        {
            CategoryId = 2,
            Title = "foreign-activity"
        });

        Assert.False(child.Ok);
        Assert.False(activity.Ok);
        Assert.Empty(activities.Inserted);
    }

    [Fact]
    public async Task Course_UsesTenantAndRejectsForeignLocation()
    {
        var courses = new CourseRepo();
        var locations = new LocationRepo(
            new AcademyLocationDto { Id = 10, SiteId = 8, Name = "foreign" });
        var service = new AcademyCourseService(
            courses, tenant: new Tenant(7), locations: locations, instructors: new InstructorRepo());

        var rejected = await service.CreateAsync(new CreateAcademyCourseDto
        {
            SiteId = 99,
            Title = "Course",
            LocationId = 10
        });

        Assert.False(rejected.Ok);
        Assert.Empty(courses.Inserted);

        var accepted = await service.CreateAsync(new CreateAcademyCourseDto
        {
            SiteId = 99,
            Title = "Course"
        });
        Assert.True(accepted.Ok);
        Assert.Equal(7, courses.Inserted.Single().SiteId);
    }

    [Fact]
    public async Task DailyCapacity_WaitlistsAndPromotesOnlyAfterSlotIsFree()
    {
        var courses = new CourseRepo
        {
            Course = new AcademyCourseDto { Id = 5, SiteId = 7, IsActive = true, InstructorId = 3 },
            Schedule = new AcademyCourseScheduleDto
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                StartDate = Day,
                EndDate = Day.AddDays(2),
                Capacity = 1
            }
        };
        var students = new StudentRepo(
            new AcademyStudentDto { Id = 7, SiteId = 7, Status = AcademyConstants.StudentStatus.Active },
            new AcademyStudentDto { Id = 8, SiteId = 7, Status = AcademyConstants.StudentStatus.Active });
        var requests = new RequestRepo();
        var service = new AcademyCourseScheduleRequestService(
            requests, courses, students, tenant: new Tenant(7));

        var first = await service.CreateAsync(Request(7), 7);
        Assert.True((await service.ReviewAsync(Approve(first.Id), actingInstructorId: 3)).Ok);
        var second = await service.CreateAsync(Request(8), 8);

        Assert.True(second.Ok);
        Assert.Equal(AcademyConstants.RequestStatus.Waitlisted, requests.ById(second.Id)!.Status);
        Assert.False((await service.ReviewAsync(Approve(second.Id), actingInstructorId: 3)).Ok);

        requests.ById(first.Id)!.Status = AcademyConstants.RequestStatus.Rejected;
        Assert.True((await service.ReviewAsync(Approve(second.Id), actingInstructorId: 3)).Ok);
        Assert.Equal(AcademyConstants.RequestStatus.Approved, requests.ById(second.Id)!.Status);
    }

    [Fact]
    public async Task Attendance_RequiresActorAndOwnsActivitySessionAndStudent()
    {
        var activities = new ActivityRepo(
            new AcademyActivityDto { Id = 1, SiteId = 7 },
            new AcademyActivityDto { Id = 2, SiteId = 8 });
        var sessions = new ActivitySessionRepo(
            new AcademyActivitySessionDto { Id = 101, ActivityId = 1 },
            new AcademyActivitySessionDto { Id = 202, ActivityId = 2 });
        var students = new StudentRepo(
            new AcademyStudentDto { Id = 7, SiteId = 7, Status = AcademyConstants.StudentStatus.Active });
        var attendance = new AttendanceRepo();
        var service = new AcademyAttendanceService(
            attendance, new CourseRepo(), sessions, null,
            activities: activities, students: students, tenant: new Tenant(7));

        Assert.False((await service.MarkAsync(Mark(101))).Ok);
        Assert.False((await service.MarkAsync(Mark(202), actingAdminUserId: 1)).Ok);
        Assert.True((await service.MarkAsync(Mark(101), actingAdminUserId: 1)).Ok);
        Assert.Single(attendance.Upserts);
    }

    [Fact]
    public async Task Calendar_FiltersForeignAndUnknownKinds()
    {
        var courses = new CourseRepo(
            new AcademyCourseDto { Id = 1, SiteId = 7 },
            new AcademyCourseDto { Id = 2, SiteId = 8 });
        var activities = new ActivityRepo(
            new AcademyActivityDto { Id = 3, SiteId = 7 },
            new AcademyActivityDto { Id = 4, SiteId = 8 });
        var calendar = new CalendarRepo(
            Event(AcademyConstants.AttendanceSessionType.Course, 1),
            Event(AcademyConstants.AttendanceSessionType.Course, 2),
            Event(AcademyConstants.AttendanceSessionType.Activity, 3),
            Event(AcademyConstants.AttendanceSessionType.Activity, 4),
            Event("Unknown", 3));
        var service = new AcademyCalendarService(
            calendar, courses, activities, new StudentRepo(), new InstructorRepo(), new Tenant(7));

        var result = await service.GetAsync(Day, Day.AddDays(1));

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Kind == AcademyConstants.AttendanceSessionType.Course && x.ParentId == 1);
        Assert.Contains(result, x => x.Kind == AcademyConstants.AttendanceSessionType.Activity && x.ParentId == 3);
    }

    [Theory]
    [InlineData("/admin/academy/courses", true)]
    [InlineData("/admin/academy/activities", true)]
    [InlineData("/admin/academy/daily-requests", true)]
    [InlineData("/admin/academy/attendance", true)]
    [InlineData("/admin/academy/calendar", true)]
    [InlineData("/academy/courses", false)]
    [InlineData("/academy/daily", false)]
    [InlineData("/academy/attendance", false)]
    [InlineData("/academy/calendar", false)]
    [InlineData("/instructor/daily-requests", false)]
    public void AcademyScenarioPages_AreModuleMapped(string route, bool admin)
    {
        var module = admin
            ? AdminRouteModuleMapping.GetModuleForPath(route)
            : PublicRouteModuleMapping.GetModuleForPath(route);
        Assert.Equal("Academy", module);
    }

    private static CreateAcademyCourseScheduleRequestDto Request(int studentId) =>
        new() { ScheduleId = 11, StudentId = studentId, SessionDate = Day };

    private static ReviewAcademyCourseScheduleRequestDto Approve(int id) =>
        new() { Id = id, Status = AcademyConstants.RequestStatus.Approved };

    private static UpsertAcademyAttendanceDto Mark(int sessionId) =>
        new()
        {
            StudentId = 7,
            ActivitySessionId = sessionId,
            Status = AcademyConstants.AttendanceStatus.Present
        };

    private static AcademyCalendarEventDto Event(string kind, int parentId) =>
        new() { Kind = kind, ParentId = parentId, SessionId = parentId * 10, StartsAt = Day };

    private static string ProjectFile(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private sealed class Tenant : ICurrentTenant
    {
        public Tenant(int siteId) => SiteId = siteId;
        public string TenantKey => "test";
        public string ConnectionString => "";
        public int SiteId { get; }
        public string SiteCode => "test";
        public string SiteName => "test";
        public bool IsResolved => true;
        public bool HasSite => SiteId > 0;
    }

    private sealed class CategoryRepo : IAcademyActivityCategoryRepository
    {
        private readonly Dictionary<int, AcademyActivityCategoryDto> _items;
        public CategoryRepo(params AcademyActivityCategoryDto[] items) => _items = items.ToDictionary(x => x.Id);
        public Task<List<AcademyActivityCategoryDto>> GetAllAsync(int? siteId = null, bool? isActive = null) =>
            Task.FromResult(_items.Values.Where(x => !siteId.HasValue || x.SiteId == siteId).ToList());
        public Task<AcademyActivityCategoryDto> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<int> InsertAsync(CreateAcademyActivityCategoryDto dto) => Task.FromResult(10);
        public Task<int> UpdateAsync(UpdateAcademyActivityCategoryDto dto) => Task.FromResult(1);
        public Task<int> DeleteAsync(int id) => Task.FromResult(1);
    }

    private sealed class ActivityRepo : IAcademyActivityRepository
    {
        private readonly Dictionary<int, AcademyActivityDto> _items;
        public ActivityRepo(params AcademyActivityDto[] items) => _items = items.ToDictionary(x => x.Id);
        public List<CreateAcademyActivityDto> Inserted { get; } = new();
        public Task<List<AcademyActivityDto>> GetAllAsync(int? siteId = null, int? categoryId = null, bool? isActive = null) =>
            Task.FromResult(_items.Values.Where(x => !siteId.HasValue || x.SiteId == siteId).ToList());
        public Task<AcademyActivityDto> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<int> InsertAsync(CreateAcademyActivityDto dto)
        {
            Inserted.Add(dto);
            return Task.FromResult(Inserted.Count);
        }
        public Task<int> UpdateAsync(UpdateAcademyActivityDto dto) => Task.FromResult(1);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class LocationRepo : IAcademyLocationRepository
    {
        private readonly Dictionary<int, AcademyLocationDto> _items;
        public LocationRepo(params AcademyLocationDto[] items) => _items = items.ToDictionary(x => x.Id);
        public Task<List<AcademyLocationDto>> GetAllAsync(int? siteId = null, bool? isActive = null) =>
            Task.FromResult(_items.Values.Where(x => !siteId.HasValue || x.SiteId == siteId).ToList());
        public Task<AcademyLocationDto> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<int> InsertAsync(CreateAcademyLocationDto dto) => Task.FromResult(1);
        public Task<bool> UpdateAsync(UpdateAcademyLocationDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class InstructorRepo : IAcademyInstructorRepository
    {
        public Task<List<AcademyInstructorDto>> GetAllAsync(int? siteId = null, bool? isActive = null) => Task.FromResult(new List<AcademyInstructorDto>());
        public Task<AcademyInstructorDto> GetByIdAsync(int id) => Task.FromResult<AcademyInstructorDto>(null);
        public Task<AcademyInstructorDto> GetByUserIdAsync(int userId) => Task.FromResult<AcademyInstructorDto>(null);
        public Task<int> InsertAsync(int? siteId, int personnelId, int userId, string bio, bool isActive, bool mustChangePassword) => Task.FromResult(1);
        public Task<bool> UpdateAsync(UpdateAcademyInstructorDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<List<int>> GetCategoryIdsAsync(int instructorId) => Task.FromResult(new List<int>());
        public Task SetCategoryIdsAsync(int instructorId, IEnumerable<int> categoryIds) => Task.CompletedTask;
        public Task<int> GetRoleIdByNameAsync(string roleName) => Task.FromResult(1);
    }

    private sealed class CourseRepo : IAcademyCourseRepository
    {
        private readonly Dictionary<int, AcademyCourseDto> _items = new();
        public CourseRepo(params AcademyCourseDto[] items)
        {
            foreach (var item in items) _items[item.Id] = item;
        }
        public AcademyCourseDto Course { get; set; }
        public AcademyCourseScheduleDto Schedule { get; set; }
        public List<CreateAcademyCourseDto> Inserted { get; } = new();
        public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
        {
            var values = _items.Values.Concat(Course == null ? Array.Empty<AcademyCourseDto>() : new[] { Course });
            return Task.FromResult(values.Where(x => !siteId.HasValue || x.SiteId == siteId).ToList());
        }
        public Task<AcademyCourseDto> GetByIdAsync(int id) =>
            Task.FromResult(Course?.Id == id ? Course : _items.GetValueOrDefault(id));
        public Task<int> InsertAsync(CreateAcademyCourseDto dto)
        {
            Inserted.Add(dto);
            return Task.FromResult(Inserted.Count);
        }
        public Task<bool> UpdateAsync(UpdateAcademyCourseDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId) =>
            Task.FromResult(Schedule == null ? new List<AcademyCourseScheduleDto>() : new() { Schedule });
        public Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId) =>
            Task.FromResult(Schedule?.Id == scheduleId ? Schedule : null);
        public Task SetSchedulesAsync(int courseId, IEnumerable<AcademyCourseScheduleDto> schedules) => Task.CompletedTask;
        public Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId) => Task.FromResult(new List<AcademyCourseSessionDto>());
        public Task ClearSessionsAsync(int courseId) => Task.CompletedTask;
        public Task<int> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto) => Task.FromResult(1);
        public Task<bool> DeleteSessionAsync(int id) => Task.FromResult(true);
    }

    private sealed class StudentRepo : IAcademyStudentRepository
    {
        private readonly Dictionary<int, AcademyStudentDto> _items;
        public StudentRepo(params AcademyStudentDto[] items) => _items = items.ToDictionary(x => x.Id);
        public Task<List<AcademyStudentDto>> GetAllAsync(int? siteId = null, string status = null) =>
            Task.FromResult(_items.Values.Where(x => !siteId.HasValue || x.SiteId == siteId).ToList());
        public Task<AcademyStudentDto> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<AcademyStudentDto> GetByUserIdAsync(int userId) => Task.FromResult(_items.Values.FirstOrDefault(x => x.UserId == userId));
        public Task<int> InsertAsync(int? siteId, int userId, string status, string notes, bool mustChangePassword) => Task.FromResult(1);
        public Task<bool> UpdateAsync(UpdateAcademyStudentDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class RequestRepo : IAcademyCourseScheduleRequestRepository
    {
        private readonly List<AcademyCourseScheduleRequestDto> _items = new();
        public AcademyCourseScheduleRequestDto ById(int id) => _items.FirstOrDefault(x => x.Id == id);
        public Task<List<AcademyCourseScheduleRequestDto>> ListAsync(int? studentId = null, int? scheduleId = null, int? courseId = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null) => Task.FromResult(_items.ToList());
        public Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null) => Task.FromResult(_items.ToList());
        public Task<AcademyCourseScheduleRequestDto> GetByIdAsync(int id) => Task.FromResult(ById(id));
        public Task<int> InsertAsync(int scheduleId, int studentId, DateTime sessionDate, string studentNote, string status = null)
        {
            var item = new AcademyCourseScheduleRequestDto
            {
                Id = _items.Count + 1,
                ScheduleId = scheduleId,
                StudentId = studentId,
                SessionDate = sessionDate.Date,
                Status = status ?? AcademyConstants.RequestStatus.Pending,
                CourseId = 5,
                CourseInstructorId = 3
            };
            _items.Add(item);
            return Task.FromResult(item.Id);
        }
        public Task<int> ReviewAsync(ReviewAcademyCourseScheduleRequestDto dto)
        {
            var item = ById(dto.Id);
            if (item == null) return Task.FromResult(0);
            item.Status = dto.Status;
            return Task.FromResult(1);
        }
        public Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId) => Task.FromResult(new List<AcademyCourseScheduleRequestAuditDto>());
        public Task<int> CountApprovedAsync(int scheduleId, DateTime sessionDate) =>
            Task.FromResult(_items.Count(x => x.ScheduleId == scheduleId && x.SessionDate.Date == sessionDate.Date && x.Status == AcademyConstants.RequestStatus.Approved));
        public Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime fromDate, DateTime toDate) => Task.FromResult(new List<AcademyDailyApprovedCountDto>());
    }

    private sealed class ActivitySessionRepo : IAcademyActivitySessionRepository
    {
        private readonly Dictionary<int, AcademyActivitySessionDto> _items;
        public ActivitySessionRepo(params AcademyActivitySessionDto[] items) => _items = items.ToDictionary(x => x.Id);
        public Task<List<AcademyActivitySessionDto>> GetAllAsync(int? activityId = null, int? instructorId = null, string status = null) => Task.FromResult(_items.Values.ToList());
        public Task<AcademyActivitySessionDto> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<int> InsertAsync(CreateAcademyActivitySessionDto dto) => Task.FromResult(1);
        public Task<bool> UpdateAsync(UpdateAcademyActivitySessionDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class AttendanceRepo : IAcademyAttendanceRepository
    {
        public List<UpsertAcademyAttendanceDto> Upserts { get; } = new();
        public Task<List<AcademyAttendanceRosterItemDto>> GetCourseSessionRosterAsync(int courseSessionId) => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
        public Task<List<AcademyAttendanceRosterItemDto>> GetActivitySessionRosterAsync(int activitySessionId) => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
        public Task<List<AcademyAttendanceRosterItemDto>> GetDailyRosterAsync(int scheduleId, DateTime sessionDate) => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
        public Task<List<AcademyAttendanceDto>> GetAllAsync(int? courseSessionId = null, int? activitySessionId = null, int? studentId = null) => Task.FromResult(new List<AcademyAttendanceDto>());
        public Task<int> UpsertAsync(UpsertAcademyAttendanceDto dto)
        {
            Upserts.Add(dto);
            return Task.FromResult(Upserts.Count);
        }
        public Task<int> UpsertDailyAsync(UpsertAcademyAttendanceDto dto) => Task.FromResult(1);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<int> CreateQrTokenAsync(string sessionType, int sessionId, string token, DateTime expiresAt, int? instructorId, int? adminUserId) => Task.FromResult(1);
        public Task<AcademyAttendanceQrTokenDto> GetQrByTokenAsync(string token) => Task.FromResult<AcademyAttendanceQrTokenDto>(null);
        public Task<AcademyAttendanceQrTokenDto> GetActiveQrAsync(string sessionType, int sessionId) => Task.FromResult<AcademyAttendanceQrTokenDto>(null);
    }

    private sealed class CalendarRepo : IAcademyCalendarRepository
    {
        private readonly List<AcademyCalendarEventDto> _items;
        public CalendarRepo(params AcademyCalendarEventDto[] items) => _items = items.ToList();
        public Task<List<AcademyCalendarEventDto>> GetAsync(DateTime from, DateTime to, int? instructorId = null, int? studentId = null, string kind = null) => Task.FromResult(_items.ToList());
    }
}
