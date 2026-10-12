using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyCourseScheduleRequestRepository
    {
        Task<List<AcademyCourseScheduleRequestDto>> ListAsync(
            int? studentId = null, int? scheduleId = null, int? courseId = null,
            string status = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<AcademyCourseScheduleRequestDto>> ListByInstructorAsync(
            int instructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<AcademyCourseScheduleRequestDto> GetByIdAsync(int id);
        Task<int> InsertAsync(int scheduleId, int studentId, DateTime sessionDate, string studentNote, string status = null);
        Task<int> ReviewAsync(ReviewAcademyCourseScheduleRequestDto dto);
        Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(int requestId);
        Task<int> CountApprovedAsync(int scheduleId, DateTime sessionDate);
        Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime fromDate, DateTime toDate);
    }

    public interface IAcademyCourseScheduleRequestService
    {
        Task<List<AcademyCourseScheduleRequestDto>> ListMineAsync(
            int actingStudentId, int? courseId = null, DateTime? fromDate = null, DateTime? toDate = null, string status = null);
        Task<AcademyCourseScheduleRequestDto> GetMineByIdAsync(int id, int actingStudentId);
        Task<AcademyActionResult> CreateAsync(CreateAcademyCourseScheduleRequestDto dto, int actingStudentId);
        Task<List<AcademyCourseScheduleRequestDto>> ListForInstructorAsync(
            int actingInstructorId, string status = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<AcademyCourseScheduleRequestDto>> ListForAdminAsync(
            int? courseId = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<AcademyActionResult> ReviewAsync(
            ReviewAcademyCourseScheduleRequestDto dto, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<List<AcademyCourseScheduleRequestAuditDto>> ListAuditsAsync(
            int requestId, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<List<AcademyDailyApprovedCountDto>> ListApprovedCountsAsync(int scheduleId, DateTime fromDate, DateTime toDate);
        /// <summary>ظرفیت تأییدشده و وضعیت درخواست دانش‌آموز را روی روزهای منطقی می‌چسباند.</summary>
        Task HydrateStudentBoardAsync(int actingStudentId, IList<AcademyLogicalSessionDto> days, DateTime from, DateTime to);
    }
}
