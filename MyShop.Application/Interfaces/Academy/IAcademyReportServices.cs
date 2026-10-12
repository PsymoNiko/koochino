using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyReportRepository
    {
        Task<AcademyAdminSummaryDto> GetAdminSummaryAsync();
        Task<AcademyAdminSummaryDto> GetAdminSummaryBySiteAsync(int siteId)
            => GetAdminSummaryAsync();
        Task<List<AcademyCourseReportDto>> GetCoursesAsync(int? instructorId = null);
        Task<List<AcademyTuitionReportDto>> GetTuitionAsync(int? instructorId = null);
        Task<List<AcademyAttendanceReportDto>> GetAttendanceByCourseAsync(int? instructorId = null);
        Task<List<AcademyActivityReportDto>> GetActivitiesAsync(int? instructorId = null);
        Task<List<AcademyInstructorReportDto>> GetInstructorsAsync();
        Task<AcademyInstructorSummaryDto> GetInstructorSummaryAsync(int instructorId);
        Task<AcademyStudentSummaryDto> GetStudentSummaryAsync(int studentId);
        Task<AcademyDailyReportSummaryDto> GetDailySummaryAsync(int? instructorId = null);
        Task<AcademyDailyReportSummaryDto> GetDailySummaryBySiteAsync(int siteId, int? instructorId = null)
            => GetDailySummaryAsync(instructorId);
        Task<List<AcademyDailyByDateReportDto>> GetDailyByDateAsync(
            DateTime fromDate, DateTime toDate, int? courseId = null, int? instructorId = null);
        Task<List<AcademyDailyByCourseReportDto>> GetDailyByCourseAsync(int? instructorId = null);
    }

    public interface IAcademyReportService
    {
        Task<AcademyAdminSummaryDto> GetAdminSummaryAsync();
        Task<List<AcademyCourseReportDto>> GetCoursesAsync(int? instructorId = null);
        Task<List<AcademyTuitionReportDto>> GetTuitionAsync(int? instructorId = null);
        Task<List<AcademyAttendanceReportDto>> GetAttendanceByCourseAsync(int? instructorId = null);
        Task<List<AcademyActivityReportDto>> GetActivitiesAsync(int? instructorId = null);
        Task<List<AcademyInstructorReportDto>> GetInstructorsAsync();
        Task<AcademyInstructorSummaryDto> GetInstructorSummaryAsync(int instructorId);
        Task<AcademyStudentSummaryDto> GetStudentSummaryAsync(int studentId);
        Task<AcademyDailyReportSummaryDto> GetDailySummaryAsync(int? instructorId = null);
        Task<List<AcademyDailyByDateReportDto>> GetDailyByDateAsync(
            DateTime fromDate, DateTime toDate, int? courseId = null, int? instructorId = null);
        Task<List<AcademyDailyByCourseReportDto>> GetDailyByCourseAsync(int? instructorId = null);
    }
}
