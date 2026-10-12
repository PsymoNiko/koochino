using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Services.Academy
{
    public class AcademyCalendarService : IAcademyCalendarService
    {
        private readonly IAcademyCalendarRepository _repo;
        private readonly IAcademyCourseRepository _courses;
        private readonly IAcademyActivityRepository _activities;
        private readonly IAcademyStudentRepository _students;
        private readonly IAcademyInstructorRepository _instructors;
        private readonly ICurrentTenant _tenant;
        public AcademyCalendarService(
            IAcademyCalendarRepository repo,
            IAcademyCourseRepository courses = null,
            IAcademyActivityRepository activities = null,
            IAcademyStudentRepository students = null,
            IAcademyInstructorRepository instructors = null,
            ICurrentTenant tenant = null)
        {
            _repo = repo;
            _courses = courses;
            _activities = activities;
            _students = students;
            _instructors = instructors;
            _tenant = tenant;
        }

        public async Task<List<AcademyCalendarEventDto>> GetAsync(
            DateTime from, DateTime to, int? instructorId = null, int? studentId = null, string kind = null)
        {
            if (to < from)
                return new List<AcademyCalendarEventDto>();
            if (studentId.HasValue && !await AcademyTenantOwnership.StudentAsync(_tenant, _students, studentId.Value))
                return new List<AcademyCalendarEventDto>();
            if (instructorId.HasValue && !await AcademyTenantOwnership.InstructorAsync(_tenant, _instructors, instructorId.Value))
                return new List<AcademyCalendarEventDto>();
            var list = await _repo.GetAsync(from, to, instructorId, studentId, kind)
                ?? new List<AcademyCalendarEventDto>();
            if (!AcademyTenantOwnership.SiteId(_tenant).HasValue) return list;
            var result = new List<AcademyCalendarEventDto>();
            foreach (var item in list)
            {
                var isActivity = string.Equals(
                    item.Kind, AcademyConstants.AttendanceSessionType.Activity, StringComparison.OrdinalIgnoreCase);
                var owned = item.IsCourse
                    ? await AcademyTenantOwnership.CourseAsync(_tenant, _courses, item.ParentId)
                    : isActivity && await AcademyTenantOwnership.ActivityAsync(_tenant, _activities, item.ParentId);
                if (owned) result.Add(item);
            }
            return result;
        }
    }
}
