using Dapper;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Academy
{
    public class AcademyScheduleExceptionRepository : IAcademyScheduleExceptionRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyScheduleExceptionRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyScheduleExceptionDto>> ListAsync(
            int? scheduleId = null, int? courseId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                using var conn = _db.CreateConnection();
                var list = await conn.QueryAsync<AcademyScheduleExceptionDto>(
                    "koch.spAcademyScheduleExceptions_List",
                    new
                    {
                        ScheduleId = scheduleId,
                        CourseId = courseId,
                        FromDate = fromDate?.Date,
                        ToDate = toDate?.Date
                    },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<AcademyScheduleExceptionDto>();
            }
            catch
            {
                return new List<AcademyScheduleExceptionDto>();
            }
        }

        public async Task<AcademyScheduleExceptionDto> GetByIdAsync(int id)
        {
            try
            {
                using var conn = _db.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<AcademyScheduleExceptionDto>(
                    "koch.spAcademyScheduleExceptions_GetById",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                return null;
            }
        }

        public async Task<AcademyScheduleExceptionDto> GetByScheduleDateAsync(int scheduleId, DateTime exceptionDate)
        {
            try
            {
                using var conn = _db.CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<AcademyScheduleExceptionDto>(
                    "koch.spAcademyScheduleExceptions_GetByScheduleDate",
                    new { ScheduleId = scheduleId, ExceptionDate = exceptionDate.Date },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                return null;
            }
        }

        public async Task<int> UpsertAsync(UpsertAcademyScheduleExceptionDto dto)
        {
            try
            {
                using var conn = _db.CreateConnection();
                var p = new DynamicParameters();
                p.Add("ScheduleId", dto.ScheduleId);
                p.Add("ExceptionDate", dto.ExceptionDate.Date, DbType.Date);
                p.Add("IsCancelled", dto.IsCancelled);
                p.Add("Hour", dto.Hour);
                p.Add("Minute", dto.Minute);
                p.Add("DurationMinutes", dto.DurationMinutes);
                p.Add("Capacity", dto.Capacity is > 0 ? dto.Capacity : null);
                p.Add("InstructorId", dto.InstructorId is > 0 ? dto.InstructorId : null);
                p.Add("Note", dto.Note);
                p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("koch.spAcademyScheduleExceptions_Upsert", p, commandType: CommandType.StoredProcedure);
                return p.Get<int>("Id");
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var conn = _db.CreateConnection();
                var p = new DynamicParameters();
                p.Add("Id", id);
                p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("koch.spAcademyScheduleExceptions_Delete", p, commandType: CommandType.StoredProcedure);
                return p.Get<int>("Result") == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
