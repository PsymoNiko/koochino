using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyAttendanceRepository
    {
        Task<List<AcademyAttendanceRosterItemDto>> GetCourseSessionRosterAsync(int courseSessionId);
        Task<List<AcademyAttendanceRosterItemDto>> GetActivitySessionRosterAsync(int activitySessionId);
        Task<List<AcademyAttendanceRosterItemDto>> GetDailyRosterAsync(int scheduleId, DateTime sessionDate);
        Task<List<AcademyAttendanceDto>> GetAllAsync(int? courseSessionId = null, int? activitySessionId = null, int? studentId = null);
        Task<int> UpsertAsync(UpsertAcademyAttendanceDto dto);
        Task<int> UpsertDailyAsync(UpsertAcademyAttendanceDto dto);
        Task<bool> DeleteAsync(int id);
        Task<int> CreateQrTokenAsync(string sessionType, int sessionId, string token, DateTime expiresAt, int? instructorId, int? adminUserId);
        Task<AcademyAttendanceQrTokenDto> GetQrByTokenAsync(string token);
        Task<AcademyAttendanceQrTokenDto> GetActiveQrAsync(string sessionType, int sessionId);
    }

    public interface IAcademyAttendanceService
    {
        Task<List<AcademyAttendanceRosterItemDto>> GetRosterAsync(string sessionType, int sessionId);
        Task<List<AcademyAttendanceRosterItemDto>> GetDailyRosterAsync(int scheduleId, DateTime sessionDate, int? actingInstructorId = null);
        Task<List<AcademyAttendanceDto>> GetAllAsync(int? courseSessionId = null, int? activitySessionId = null, int? studentId = null);
        Task<AcademyActionResult> MarkAsync(UpsertAcademyAttendanceDto dto, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<AcademyActionResult> MarkDailyAsync(UpsertAcademyAttendanceDto dto, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<AcademyActionResult> MarkBulkAsync(string sessionType, int sessionId, IEnumerable<(int StudentId, string Status)> marks, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<AcademyActionResult> MarkDailyBulkAsync(int scheduleId, DateTime sessionDate, IEnumerable<(int StudentId, string Status)> marks, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<bool> DeleteAsync(int id);
        Task<AcademyActionResult> CreateQrAsync(string sessionType, int sessionId, int validMinutes = 20, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<AcademyAttendanceQrTokenDto> GetActiveQrAsync(string sessionType, int sessionId);
        Task<AcademyAttendanceQrTokenDto> GetQrByTokenAsync(string token);
        Task<AcademyActionResult> CheckInByQrAsync(string token, int studentId);
    }
}
