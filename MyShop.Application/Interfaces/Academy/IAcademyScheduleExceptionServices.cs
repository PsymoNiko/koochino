using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyScheduleExceptionRepository
    {
        Task<List<AcademyScheduleExceptionDto>> ListAsync(
            int? scheduleId = null, int? courseId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<AcademyScheduleExceptionDto> GetByIdAsync(int id);
        Task<AcademyScheduleExceptionDto> GetByScheduleDateAsync(int scheduleId, DateTime exceptionDate);
        Task<int> UpsertAsync(UpsertAcademyScheduleExceptionDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IAcademyScheduleExceptionService
    {
        Task<List<AcademyScheduleExceptionDto>> ListByCourseAsync(int courseId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<AcademyScheduleExceptionDto>> ListByScheduleAsync(int scheduleId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<AcademyScheduleExceptionDto> GetByScheduleDateAsync(int scheduleId, DateTime exceptionDate);
        Task<AcademyActionResult> UpsertAsync(
            UpsertAcademyScheduleExceptionDto dto, int? actingInstructorId = null, int? actingAdminUserId = null);
        Task<AcademyActionResult> DeleteAsync(int id, int? actingInstructorId = null, int? actingAdminUserId = null);
    }
}
