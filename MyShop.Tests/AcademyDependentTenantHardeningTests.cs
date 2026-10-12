using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Services.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Tests;

public class AcademyDependentTenantHardeningTests
{
    [Fact]
    public async Task Enrollment_ListAndRegister_UseCourseAndStudentTenant()
    {
        var courses = new CourseRepo();
        courses.Items[1] = new AcademyCourseDto { Id = 1, SiteId = 7 };
        courses.Items[2] = new AcademyCourseDto { Id = 2, SiteId = 8 };
        var students = new StudentRepo();
        students.Items[10] = new AcademyStudentDto { Id = 10, SiteId = 7 };
        students.Items[20] = new AcademyStudentDto { Id = 20, SiteId = 8 };
        var enrollments = new EnrollmentRepo
        {
            Items =
            {
                new AcademyEnrollmentDto { Id = 1, CourseId = 1, StudentId = 10 },
                new AcademyEnrollmentDto { Id = 2, CourseId = 2, StudentId = 20 }
            }
        };
        var service = new AcademyEnrollmentService(
            enrollments, null, null, courses, students, new Tenant(7));

        var list = await service.GetAllAsync();
        var foreign = await service.RegisterAsync(new CreateAcademyEnrollmentDto { CourseId = 2, StudentId = 20 });

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.False(foreign.Ok);
        Assert.False(enrollments.RegisterCalled);
    }

    [Fact]
    public async Task Attendance_List_FiltersForeignSessionChain()
    {
        var courses = new CourseRepo();
        courses.Items[1] = new AcademyCourseDto { Id = 1, SiteId = 7 };
        courses.Items[2] = new AcademyCourseDto { Id = 2, SiteId = 8 };
        courses.Sessions[1] = new List<AcademyCourseSessionDto> { new() { Id = 101, CourseId = 1 } };
        courses.Sessions[2] = new List<AcademyCourseSessionDto> { new() { Id = 202, CourseId = 2 } };
        var students = new StudentRepo();
        students.Items[10] = new AcademyStudentDto { Id = 10, SiteId = 7 };
        students.Items[20] = new AcademyStudentDto { Id = 20, SiteId = 8 };
        var attendance = new AttendanceRepo
        {
            Items =
            {
                new AcademyAttendanceDto { Id = 1, CourseSessionId = 101, StudentId = 10 },
                new AcademyAttendanceDto { Id = 2, CourseSessionId = 202, StudentId = 20 }
            }
        };
        var service = new AcademyAttendanceService(
            attendance, courses, new ActivitySessionRepo(), null,
            activities: new ActivityRepo(), students: students, tenant: new Tenant(7));

        var list = await service.GetAllAsync();

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
    }

    [Fact]
    public async Task Certificate_GetAndRevoke_RejectForeignEnrollmentChain()
    {
        var courses = new CourseRepo();
        courses.Items[2] = new AcademyCourseDto { Id = 2, SiteId = 8 };
        var students = new StudentRepo();
        students.Items[20] = new AcademyStudentDto { Id = 20, SiteId = 8 };
        var enrollments = new EnrollmentRepo();
        enrollments.Items.Add(new AcademyEnrollmentDto { Id = 30, CourseId = 2, StudentId = 20 });
        var certificates = new CertificateRepo
        {
            Item = new AcademyCertificateDto { Id = 40, EnrollmentId = 30, CourseId = 2, StudentId = 20 }
        };
        var service = new AcademyCertificateService(
            certificates, null, enrollments, courses, students, new Tenant(7));

        Assert.Null(await service.GetByIdAsync(40));
        Assert.False((await service.RevokeAsync(40)).Ok);
        Assert.False(certificates.RevokeCalled);
    }

    [Fact]
    public async Task CourseScheduleRequest_AdminList_FiltersForeignCourse()
    {
        var courses = new CourseRepo();
        courses.Items[1] = new AcademyCourseDto { Id = 1, SiteId = 7 };
        courses.Items[2] = new AcademyCourseDto { Id = 2, SiteId = 8 };
        var students = new StudentRepo();
        students.Items[10] = new AcademyStudentDto { Id = 10, SiteId = 7 };
        students.Items[20] = new AcademyStudentDto { Id = 20, SiteId = 8 };
        var requests = new ScheduleRequestRepo
        {
            Items =
            {
                new AcademyCourseScheduleRequestDto { Id = 1, CourseId = 1, StudentId = 10 },
                new AcademyCourseScheduleRequestDto { Id = 2, CourseId = 2, StudentId = 20 }
            }
        };
        var service = new AcademyCourseScheduleRequestService(
            requests, courses, students, tenant: new Tenant(7));

        var list = await service.ListForAdminAsync();

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
    }

    [Fact]
    public async Task Lesson_Update_RejectsForeignCourseOwnership()
    {
        var courses = new CourseRepo();
        courses.Items[2] = new AcademyCourseDto { Id = 2, SiteId = 8 };
        var lessons = new LessonRepo { Item = new AcademyLessonDto { Id = 5, CourseId = 2 } };
        var service = new AcademyLessonService(lessons, new EnrollmentRepo(), courses, new Tenant(7));

        var result = await service.UpdateAsync(5, "عنوان", null, 0, true);

        Assert.False(result.Ok);
        Assert.False(lessons.UpdateCalled);
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

    private sealed class CourseRepo : IAcademyCourseRepository
    {
        public Dictionary<int, AcademyCourseDto> Items { get; } = new();
        public Dictionary<int, List<AcademyCourseSessionDto>> Sessions { get; } = new();
        public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
            => Task.FromResult(Items.Values.Where(x => !siteId.HasValue || x.SiteId == siteId).ToList());
        public Task<AcademyCourseDto> GetByIdAsync(int id) => Task.FromResult(Items.GetValueOrDefault(id));
        public Task<int> InsertAsync(CreateAcademyCourseDto dto) => Task.FromResult(1);
        public Task<bool> UpdateAsync(UpdateAcademyCourseDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId) => Task.FromResult(new List<AcademyCourseScheduleDto>());
        public Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId) => Task.FromResult<AcademyCourseScheduleDto>(null);
        public Task SetSchedulesAsync(int courseId, IEnumerable<AcademyCourseScheduleDto> schedules) => Task.CompletedTask;
        public Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId)
            => Task.FromResult(Sessions.GetValueOrDefault(courseId) ?? new List<AcademyCourseSessionDto>());
        public Task ClearSessionsAsync(int courseId) => Task.CompletedTask;
        public Task<int> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto) => Task.FromResult(1);
        public Task<bool> DeleteSessionAsync(int id) => Task.FromResult(true);
    }

    private sealed class StudentRepo : IAcademyStudentRepository
    {
        public Dictionary<int, AcademyStudentDto> Items { get; } = new();
        public Task<List<AcademyStudentDto>> GetAllAsync(int? siteId = null, string status = null)
            => Task.FromResult(Items.Values.Where(x => !siteId.HasValue || x.SiteId == siteId).ToList());
        public Task<AcademyStudentDto> GetByIdAsync(int id) => Task.FromResult(Items.GetValueOrDefault(id));
        public Task<AcademyStudentDto> GetByUserIdAsync(int userId) => Task.FromResult(Items.Values.FirstOrDefault(x => x.UserId == userId));
        public Task<int> InsertAsync(int? siteId, int userId, string status, string notes, bool mustChangePassword) => Task.FromResult(1);
        public Task<bool> UpdateAsync(UpdateAcademyStudentDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class EnrollmentRepo : IAcademyEnrollmentRepository
    {
        public List<AcademyEnrollmentDto> Items { get; } = new();
        public bool RegisterCalled { get; private set; }
        public Task<List<AcademyEnrollmentDto>> GetAllAsync(int? courseId = null, int? studentId = null, int? instructorId = null, string status = null)
            => Task.FromResult(Items.ToList());
        public Task<AcademyEnrollmentDto> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<AcademyEnrollmentDto> GetByCourseStudentAsync(int courseId, int studentId)
            => Task.FromResult(Items.FirstOrDefault(x => x.CourseId == courseId && x.StudentId == studentId));
        public Task<int> RegisterAsync(int courseId, int studentId, string studentNote) { RegisterCalled = true; return Task.FromResult(1); }
        public Task<int> ReviewAsync(ReviewAcademyEnrollmentDto dto) => Task.FromResult(1);
        public Task<bool> CancelByStudentAsync(int id, int studentId) => Task.FromResult(true);
    }

    private sealed class AttendanceRepo : IAcademyAttendanceRepository
    {
        public List<AcademyAttendanceDto> Items { get; } = new();
        public Task<List<AcademyAttendanceRosterItemDto>> GetCourseSessionRosterAsync(int id) => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
        public Task<List<AcademyAttendanceRosterItemDto>> GetActivitySessionRosterAsync(int id) => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
        public Task<List<AcademyAttendanceRosterItemDto>> GetDailyRosterAsync(int id, DateTime date) => Task.FromResult(new List<AcademyAttendanceRosterItemDto>());
        public Task<List<AcademyAttendanceDto>> GetAllAsync(int? courseSessionId = null, int? activitySessionId = null, int? studentId = null) => Task.FromResult(Items.ToList());
        public Task<int> UpsertAsync(UpsertAcademyAttendanceDto dto) => Task.FromResult(1);
        public Task<int> UpsertDailyAsync(UpsertAcademyAttendanceDto dto) => Task.FromResult(1);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<int> CreateQrTokenAsync(string type, int id, string token, DateTime expires, int? instructorId, int? adminId) => Task.FromResult(1);
        public Task<AcademyAttendanceQrTokenDto> GetQrByTokenAsync(string token) => Task.FromResult<AcademyAttendanceQrTokenDto>(null);
        public Task<AcademyAttendanceQrTokenDto> GetActiveQrAsync(string type, int id) => Task.FromResult<AcademyAttendanceQrTokenDto>(null);
    }

    private sealed class ActivitySessionRepo : IAcademyActivitySessionRepository
    {
        public Task<List<AcademyActivitySessionDto>> GetAllAsync(int? activityId = null, int? instructorId = null, string status = null) => Task.FromResult(new List<AcademyActivitySessionDto>());
        public Task<AcademyActivitySessionDto> GetByIdAsync(int id) => Task.FromResult<AcademyActivitySessionDto>(null);
        public Task<int> InsertAsync(CreateAcademyActivitySessionDto dto) => Task.FromResult(1);
        public Task<bool> UpdateAsync(UpdateAcademyActivitySessionDto dto) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class ActivityRepo : IAcademyActivityRepository
    {
        public Task<List<AcademyActivityDto>> GetAllAsync(int? siteId = null, int? categoryId = null, bool? isActive = null) => Task.FromResult(new List<AcademyActivityDto>());
        public Task<AcademyActivityDto> GetByIdAsync(int id) => Task.FromResult<AcademyActivityDto>(null);
        public Task<int> InsertAsync(CreateAcademyActivityDto dto) => Task.FromResult(1);
        public Task<int> UpdateAsync(UpdateAcademyActivityDto dto) => Task.FromResult(1);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class CertificateRepo : IAcademyCertificateRepository
    {
        public AcademyCertificateDto Item { get; set; }
        public bool RevokeCalled { get; private set; }
        public Task<List<AcademyCertificateDto>> GetAllAsync(int? studentId = null, int? instructorId = null, int? courseId = null, string status = null) => Task.FromResult(new List<AcademyCertificateDto> { Item });
        public Task<AcademyCertificateDto> GetByIdAsync(int id) => Task.FromResult(Item);
        public Task<AcademyCertificateDto> GetByEnrollmentAsync(int enrollmentId) => Task.FromResult(Item);
        public Task<int> IssueAsync(int enrollmentId, int? userId, int? instructorId, string notes) => Task.FromResult(Item.Id);
        public Task<bool> RevokeAsync(int id, string reason) { RevokeCalled = true; return Task.FromResult(true); }
    }

    private sealed class ScheduleRequestRepo : IAcademyCourseScheduleRequestRepository
    {
        public List<AcademyCourseScheduleRequestDto> Items { get; } = new();
        public Task<List<AcademyCourseScheduleRequestDto>> ListAsync(int? studentId = null, int? scheduleId = null, int? courseId = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null) => Task.FromResult(Items.ToList());
        public Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(int id, string status = null, DateTime? fromDate = null, DateTime? toDate = null) => Task.FromResult(Items.ToList());
        public Task<AcademyCourseScheduleRequestDto> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<int> InsertAsync(int scheduleId, int studentId, DateTime date, string note, string status = null) => Task.FromResult(1);
        public Task<int> ReviewAsync(ReviewAcademyCourseScheduleRequestDto dto) => Task.FromResult(1);
        public Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId) => Task.FromResult(new List<AcademyCourseScheduleRequestAuditDto>());
        public Task<int> CountApprovedAsync(int scheduleId, DateTime date) => Task.FromResult(0);
        public Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime from, DateTime to) => Task.FromResult(new List<AcademyDailyApprovedCountDto>());
    }

    private sealed class LessonRepo : IAcademyLessonRepository
    {
        public AcademyLessonDto Item { get; set; }
        public bool UpdateCalled { get; private set; }
        public Task<List<AcademyLessonDto>> GetByCourseAsync(int courseId, bool publishedOnly = false) => Task.FromResult(new List<AcademyLessonDto> { Item });
        public Task<AcademyLessonDto> GetByIdAsync(int id) => Task.FromResult(Item);
        public Task<int> InsertAsync(int courseId, string title, string body, int sortOrder, bool isPublished) => Task.FromResult(1);
        public Task<bool> UpdateAsync(int id, string title, string body, int sortOrder, bool isPublished) { UpdateCalled = true; return Task.FromResult(true); }
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }
}
