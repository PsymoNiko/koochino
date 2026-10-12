using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    internal static class AcademyTenantOwnership
    {
        public static int? SiteId(ICurrentTenant tenant) => SiteScope.CurrentSiteId(tenant);

        public static async Task<bool> CourseAsync(
            ICurrentTenant tenant, IAcademyCourseRepository courses, int courseId)
        {
            var siteId = SiteId(tenant);
            if (!siteId.HasValue) return true;
            if (courses == null || courseId <= 0) return false;
            var course = await courses.GetByIdAsync(courseId);
            return course?.SiteId == siteId;
        }

        public static async Task<bool> StudentAsync(
            ICurrentTenant tenant, IAcademyStudentRepository students, int studentId)
        {
            var siteId = SiteId(tenant);
            if (!siteId.HasValue) return true;
            if (students == null || studentId <= 0) return false;
            var student = await students.GetByIdAsync(studentId);
            return student?.SiteId == siteId;
        }

        public static async Task<bool> InstructorAsync(
            ICurrentTenant tenant, IAcademyInstructorRepository instructors, int instructorId)
        {
            var siteId = SiteId(tenant);
            if (!siteId.HasValue) return true;
            if (instructors == null || instructorId <= 0) return false;
            var instructor = await instructors.GetByIdAsync(instructorId);
            return instructor?.SiteId == siteId;
        }

        public static async Task<bool> EnrollmentAsync(
            ICurrentTenant tenant,
            IAcademyEnrollmentRepository enrollments,
            IAcademyCourseRepository courses,
            IAcademyStudentRepository students,
            int enrollmentId)
        {
            var siteId = SiteId(tenant);
            if (!siteId.HasValue) return true;
            if (enrollments == null || enrollmentId <= 0) return false;
            var item = await enrollments.GetByIdAsync(enrollmentId);
            return item != null
                && await CourseAsync(tenant, courses, item.CourseId)
                && await StudentAsync(tenant, students, item.StudentId);
        }

        public static async Task<bool> EnrollmentDtoAsync(
            ICurrentTenant tenant,
            IAcademyCourseRepository courses,
            IAcademyStudentRepository students,
            AcademyEnrollmentDto item)
        {
            if (item == null) return false;
            if (!SiteId(tenant).HasValue) return true;
            return await CourseAsync(tenant, courses, item.CourseId)
                && await StudentAsync(tenant, students, item.StudentId);
        }

        public static async Task<bool> ScheduleAsync(
            ICurrentTenant tenant, IAcademyCourseRepository courses, int scheduleId)
        {
            var siteId = SiteId(tenant);
            if (!siteId.HasValue) return true;
            if (courses == null || scheduleId <= 0) return false;
            var schedule = await courses.GetScheduleByIdAsync(scheduleId);
            return schedule != null && await CourseAsync(tenant, courses, schedule.CourseId);
        }

        public static async Task<bool> ActivityAsync(
            ICurrentTenant tenant, IAcademyActivityRepository activities, int activityId)
        {
            var siteId = SiteId(tenant);
            if (!siteId.HasValue) return true;
            if (activities == null || activityId <= 0) return false;
            var activity = await activities.GetByIdAsync(activityId);
            return activity?.SiteId == siteId;
        }

        public static async Task<bool> ActivitySessionAsync(
            ICurrentTenant tenant,
            IAcademyActivitySessionRepository sessions,
            IAcademyActivityRepository activities,
            int sessionId)
        {
            if (!SiteId(tenant).HasValue) return true;
            if (sessions == null || sessionId <= 0) return false;
            var session = await sessions.GetByIdAsync(sessionId);
            return session != null && await ActivityAsync(tenant, activities, session.ActivityId);
        }

        public static async Task<bool> CourseSessionAsync(
            ICurrentTenant tenant, IAcademyCourseRepository courses, int sessionId)
        {
            var siteId = SiteId(tenant);
            if (!siteId.HasValue) return true;
            if (courses == null || sessionId <= 0) return false;
            var scopedCourses = await courses.GetAllAsync(siteId);
            foreach (var course in scopedCourses)
            {
                var sessions = await courses.GetSessionsAsync(course.Id);
                if (sessions.Any(x => x.Id == sessionId)) return true;
            }
            return false;
        }

        public static async Task<bool> AttendanceAsync(
            ICurrentTenant tenant,
            IAcademyCourseRepository courses,
            IAcademyActivitySessionRepository activitySessions,
            IAcademyActivityRepository activities,
            IAcademyStudentRepository students,
            AcademyAttendanceDto item)
        {
            if (item == null) return false;
            if (!SiteId(tenant).HasValue) return true;
            if (!await StudentAsync(tenant, students, item.StudentId)) return false;
            if (item.ScheduleId.HasValue)
                return await ScheduleAsync(tenant, courses, item.ScheduleId.Value);
            if (item.CourseSessionId.HasValue)
                return await CourseSessionAsync(tenant, courses, item.CourseSessionId.Value);
            if (item.ActivitySessionId.HasValue)
                return await ActivitySessionAsync(tenant, activitySessions, activities, item.ActivitySessionId.Value);
            return false;
        }
    }
}
