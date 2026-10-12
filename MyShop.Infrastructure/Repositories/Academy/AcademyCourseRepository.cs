using Dapper;
using MyShop.Application.Interfaces.Academy;
using MyShop.Core.Academy;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Academy;
using MyShop.Core.Models.Security;
using MyShop.Core.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Academy
{
    public class AcademyCourseRepository : IAcademyCourseRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyCourseRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
        {
            return await GetAllScopedAsync(DataScopeQuery.Unrestricted(), siteId, isActive);
        }

        public async Task<List<AcademyCourseDto>> GetAllScopedAsync(DataScopeQuery scope, int? siteId = null, bool? isActive = null)
        {
            using var conn = _db.CreateConnection();
            var scopeKind = scope?.Kind ?? "All";
            var scopeValues = scope?.Values != null && scope.Values.Count > 0
                ? string.Join(",", scope.Values) : null;
            var sql = $@"
                SELECT TOP 400 c.*
                FROM koch.AcademyCourses c
                WHERE (@SiteId IS NULL OR c.SiteId = @SiteId)
                  AND (@IsActive IS NULL OR c.IsActive = @IsActive)
                  AND {DataScopeSqlBuilder.SiteEntityScopePredicate("c")}
                ORDER BY c.Id DESC";
            var list = await conn.QueryAsync<AcademyCourseDto>(sql, new
            {
                SiteId = siteId,
                IsActive = isActive,
                ScopeKind = scopeKind,
                ScopeValues = scopeValues
            });
            return list?.ToList() ?? new List<AcademyCourseDto>();
        }

        public async Task<AcademyCourseDto> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<AcademyCourseDto>(
                "koch.spAcademyCourses_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
            if (item != null)
                item.Schedules = await GetSchedulesAsync(id);
            return item;
        }

        public async Task<int> InsertAsync(CreateAcademyCourseDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = ParamsFromCreate(dto);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourses_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(UpdateAcademyCourseDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = ParamsFromCreate(dto);
            p.Add("Id", dto.Id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourses_Update", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourses_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        public async Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId)
        {
            using var conn = _db.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<AcademyCourseScheduleDto>(
                    @"SELECT s.Id, s.CourseId, s.DayId, s.Hour, s.Minute, s.DurationMinutes, s.SortOrder,
                             d.Name AS DayName,
                             ISNULL(NULLIF(LTRIM(RTRIM(s.RecurrenceType)), N''), N'Weekly') AS RecurrenceType,
                             s.StartDate, s.EndDate, s.Capacity
                      FROM koch.AcademyCourseSchedules s
                      LEFT JOIN koch.Days d ON d.Id = s.DayId
                      WHERE s.CourseId = @CourseId
                      ORDER BY s.SortOrder, s.DayId, s.Hour, s.Minute, s.Id",
                    new { CourseId = courseId });
                return list?.ToList() ?? new List<AcademyCourseScheduleDto>();
            }
            catch
            {
                try
                {
                    var list = await conn.QueryAsync<AcademyCourseScheduleDto>(
                        @"SELECT s.Id, s.CourseId, s.DayId, s.Hour, s.Minute, s.DurationMinutes, s.SortOrder,
                                 d.Name AS DayName,
                                 ISNULL(NULLIF(LTRIM(RTRIM(s.RecurrenceType)), N''), N'Weekly') AS RecurrenceType,
                                 s.StartDate, s.EndDate
                          FROM koch.AcademyCourseSchedules s
                          LEFT JOIN koch.Days d ON d.Id = s.DayId
                          WHERE s.CourseId = @CourseId
                          ORDER BY s.SortOrder, s.DayId, s.Hour, s.Minute, s.Id",
                        new { CourseId = courseId });
                    return list?.ToList() ?? new List<AcademyCourseScheduleDto>();
                }
                catch
                {
                    var list = await conn.QueryAsync<AcademyCourseScheduleDto>(
                        "koch.spAcademyCourseSchedules_Get",
                        new { CourseId = courseId },
                        commandType: CommandType.StoredProcedure);
                    return list?.ToList() ?? new List<AcademyCourseScheduleDto>();
                }
            }
        }

        public async Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId)
        {
            if (scheduleId <= 0)
                return null;

            using var conn = _db.CreateConnection();
            try
            {
                return await conn.QueryFirstOrDefaultAsync<AcademyCourseScheduleDto>(
                    @"SELECT s.Id, s.CourseId, s.DayId, s.Hour, s.Minute, s.DurationMinutes, s.SortOrder,
                             d.Name AS DayName,
                             ISNULL(NULLIF(LTRIM(RTRIM(s.RecurrenceType)), N''), N'Weekly') AS RecurrenceType,
                             s.StartDate, s.EndDate, s.Capacity
                      FROM koch.AcademyCourseSchedules s
                      LEFT JOIN koch.Days d ON d.Id = s.DayId
                      WHERE s.Id = @Id",
                    new { Id = scheduleId });
            }
            catch
            {
                try
                {
                    return await conn.QueryFirstOrDefaultAsync<AcademyCourseScheduleDto>(
                        @"SELECT s.Id, s.CourseId, s.DayId, s.Hour, s.Minute, s.DurationMinutes, s.SortOrder,
                                 d.Name AS DayName,
                                 ISNULL(NULLIF(LTRIM(RTRIM(s.RecurrenceType)), N''), N'Weekly') AS RecurrenceType,
                                 s.StartDate, s.EndDate
                          FROM koch.AcademyCourseSchedules s
                          LEFT JOIN koch.Days d ON d.Id = s.DayId
                          WHERE s.Id = @Id",
                        new { Id = scheduleId });
                }
                catch
                {
                    return null;
                }
            }
        }

        public async Task SetSchedulesAsync(int courseId, IEnumerable<AcademyCourseScheduleDto> schedules)
        {
            var list = (schedules ?? Enumerable.Empty<AcademyCourseScheduleDto>()).ToList();
            var weekly = list
                .Where(s => AcademyDailyScheduleResolver.IsWeekly(s) && s.DayId > 0 && s.Hour >= 0 && s.Hour <= 23)
                .ToList();
            var daily = list
                .Where(s => AcademyDailyScheduleResolver.IsDaily(s) && s.Hour >= 0 && s.Hour <= 23)
                .ToList();

            using var conn = _db.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            // اگر ستون‌های Phase 1 نباشد، فقط Weekly با SP قدیمی ذخیره می‌شود.
            // SP را بعد از خطای عمومی صدا نزن؛ DELETE آن همه ردیف‌ها از جمله Daily را پاک می‌کند.
            if (!await HasScheduleColumnAsync(conn, "RecurrenceType"))
            {
                if (daily.Count > 0)
                    throw new System.InvalidOperationException(
                        "ستون‌های برنامه روزانه هنوز در دیتابیس اعمال نشده‌اند. اسکریپت sql/Academy_Phase20_DailySchedules.sql را اجرا کنید.");

                await SetWeeklyViaLegacyProcedureAsync(conn, courseId, weekly);
                return;
            }

            var hasDailyCapacityColumn = await HasScheduleColumnAsync(conn, "Capacity");

            using var tx = conn.BeginTransaction();
                await conn.ExecuteAsync(
                    @"DELETE FROM koch.AcademyCourseSchedules
                      WHERE CourseId = @CourseId
                        AND (RecurrenceType = N'Weekly' OR RecurrenceType IS NULL)",
                    new { CourseId = courseId }, tx);

                var sort = 0;
                foreach (var s in weekly)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO koch.AcademyCourseSchedules
                            (CourseId, DayId, Hour, Minute, DurationMinutes, SortOrder, RecurrenceType, StartDate, EndDate)
                          VALUES
                            (@CourseId, @DayId, @Hour, @Minute, @DurationMinutes, @SortOrder, N'Weekly', NULL, NULL)",
                        new
                        {
                            CourseId = courseId,
                            s.DayId,
                            Hour = (byte)s.Hour,
                            Minute = (byte)(s.Minute < 0 || s.Minute > 59 ? 0 : s.Minute),
                            s.DurationMinutes,
                            SortOrder = s.SortOrder != 0 ? s.SortOrder : sort
                        }, tx);
                    sort++;
                }

                // Daily را upsert می‌کنیم تا Id و استثناهای یک‌روزه حفظ شوند.
                var existingDaily = (await conn.QueryAsync<int>(
                    @"SELECT Id FROM koch.AcademyCourseSchedules
                      WHERE CourseId = @CourseId AND RecurrenceType = N'Daily'",
                    new { CourseId = courseId }, tx)).ToList();
                var keepDaily = new HashSet<int>();

                sort = 0;
                foreach (var s in daily)
                {
                    var hour = (byte)s.Hour;
                    var minute = (byte)(s.Minute < 0 || s.Minute > 59 ? 0 : s.Minute);
                    var sortOrder = s.SortOrder != 0 ? s.SortOrder : sort;
                    var startDate = s.StartDate?.Date;
                    var endDate = s.EndDate?.Date;
                    var capacity = s.Capacity is > 0 ? s.Capacity : (int?)null;
                    var canUpdate = s.Id > 0 && existingDaily.Contains(s.Id);

                    if (canUpdate)
                    {
                        keepDaily.Add(s.Id);
                        if (hasDailyCapacityColumn)
                        {
                            await conn.ExecuteAsync(
                                @"UPDATE koch.AcademyCourseSchedules
                                  SET Hour = @Hour, Minute = @Minute, DurationMinutes = @DurationMinutes,
                                      SortOrder = @SortOrder, StartDate = @StartDate, EndDate = @EndDate,
                                      Capacity = @Capacity, DayId = NULL
                                  WHERE Id = @Id AND CourseId = @CourseId AND RecurrenceType = N'Daily'",
                                new
                                {
                                    Id = s.Id,
                                    CourseId = courseId,
                                    Hour = hour,
                                    Minute = minute,
                                    s.DurationMinutes,
                                    SortOrder = sortOrder,
                                    StartDate = startDate,
                                    EndDate = endDate,
                                    Capacity = capacity
                                }, tx);
                        }
                        else
                        {
                            await conn.ExecuteAsync(
                                @"UPDATE koch.AcademyCourseSchedules
                                  SET Hour = @Hour, Minute = @Minute, DurationMinutes = @DurationMinutes,
                                      SortOrder = @SortOrder, StartDate = @StartDate, EndDate = @EndDate, DayId = NULL
                                  WHERE Id = @Id AND CourseId = @CourseId AND RecurrenceType = N'Daily'",
                                new
                                {
                                    Id = s.Id,
                                    CourseId = courseId,
                                    Hour = hour,
                                    Minute = minute,
                                    s.DurationMinutes,
                                    SortOrder = sortOrder,
                                    StartDate = startDate,
                                    EndDate = endDate
                                }, tx);
                        }
                    }
                    else if (hasDailyCapacityColumn)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO koch.AcademyCourseSchedules
                                (CourseId, DayId, Hour, Minute, DurationMinutes, SortOrder, RecurrenceType, StartDate, EndDate, Capacity)
                              VALUES
                                (@CourseId, NULL, @Hour, @Minute, @DurationMinutes, @SortOrder, N'Daily', @StartDate, @EndDate, @Capacity)",
                            new
                            {
                                CourseId = courseId,
                                Hour = hour,
                                Minute = minute,
                                s.DurationMinutes,
                                SortOrder = sortOrder,
                                StartDate = startDate,
                                EndDate = endDate,
                                Capacity = capacity
                            }, tx);
                    }
                    else
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO koch.AcademyCourseSchedules
                                (CourseId, DayId, Hour, Minute, DurationMinutes, SortOrder, RecurrenceType, StartDate, EndDate)
                              VALUES
                                (@CourseId, NULL, @Hour, @Minute, @DurationMinutes, @SortOrder, N'Daily', @StartDate, @EndDate)",
                            new
                            {
                                CourseId = courseId,
                                Hour = hour,
                                Minute = minute,
                                s.DurationMinutes,
                                SortOrder = sortOrder,
                                StartDate = startDate,
                                EndDate = endDate
                            }, tx);
                    }
                    sort++;
                }

                var removedDaily = existingDaily.Where(id => !keepDaily.Contains(id)).ToList();
                if (removedDaily.Count > 0)
                {
                    var exceptionsTableId = await conn.ExecuteScalarAsync<int?>(
                        "SELECT OBJECT_ID(N'koch.AcademyScheduleExceptions', N'U')",
                        transaction: tx);
                    if (exceptionsTableId is > 0)
                    {
                        await conn.ExecuteAsync(
                            "DELETE FROM koch.AcademyScheduleExceptions WHERE ScheduleId IN @Ids",
                            new { Ids = removedDaily }, tx);
                    }

                    await conn.ExecuteAsync(
                        @"DELETE FROM koch.AcademyCourseSchedules
                          WHERE CourseId = @CourseId AND RecurrenceType = N'Daily' AND Id IN @Ids",
                        new { CourseId = courseId, Ids = removedDaily }, tx);
                }

            tx.Commit();
        }

        static async Task<bool> HasScheduleColumnAsync(IDbConnection conn, string columnName)
        {
            var length = await conn.ExecuteScalarAsync<int?>(
                "SELECT COL_LENGTH(N'koch.AcademyCourseSchedules', @Name)",
                new { Name = columnName });
            return length.HasValue && length.Value > 0;
        }

        static async Task SetWeeklyViaLegacyProcedureAsync(
            IDbConnection conn, int courseId, List<AcademyCourseScheduleDto> weekly)
        {
            var payload = weekly.Select(s => new
            {
                s.DayId,
                s.Hour,
                Minute = s.Minute < 0 || s.Minute > 59 ? 0 : s.Minute,
                s.DurationMinutes,
                s.SortOrder
            });
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            await conn.ExecuteAsync(
                "koch.spAcademyCourseSchedules_Set",
                new { CourseId = courseId, Json = json },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyCourseSessionDto>(
                "koch.spAcademyCourseSessions_List",
                new { CourseId = courseId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyCourseSessionDto>();
        }

        public async Task ClearSessionsAsync(int courseId)
        {
            using var conn = _db.CreateConnection();
            await conn.ExecuteAsync(
                "koch.spAcademyCourseSessions_ClearByCourse",
                new { CourseId = courseId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", dto.Id);
            p.Add("CourseId", dto.CourseId);
            p.Add("SessionNumber", dto.SessionNumber);
            p.Add("SessionDate", dto.SessionDate.Date);
            p.Add("StartTime", dto.StartTime);
            p.Add("EndTime", dto.EndTime);
            p.Add("LocationId", dto.LocationId);
            p.Add("Title", dto.Title);
            p.Add("Status", dto.Status ?? AcademyConstants.SessionStatus.Scheduled);
            p.Add("Notes", dto.Notes);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourseSessions_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<bool> DeleteSessionAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", id);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyCourseSessions_Delete", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Result") == 1;
        }

        private static DynamicParameters ParamsFromCreate(CreateAcademyCourseDto dto)
        {
            var p = new DynamicParameters();
            p.Add("SiteId", dto.SiteId);
            p.Add("Title", dto.Title);
            p.Add("Description", dto.Description);
            p.Add("LocationId", dto.LocationId);
            p.Add("InstructorId", dto.InstructorId);
            p.Add("Price", dto.Price);
            p.Add("Capacity", dto.Capacity);
            p.Add("NumberOfSessions", dto.NumberOfSessions);
            p.Add("RegistrationStart", dto.RegistrationStart);
            p.Add("RegistrationEnd", dto.RegistrationEnd);
            p.Add("CourseStartDate", dto.CourseStartDate?.Date);
            p.Add("CourseEndDate", dto.CourseEndDate?.Date);
            p.Add("Requirements", dto.Requirements);
            p.Add("ThingsToBring", dto.ThingsToBring);
            p.Add("ImageUrl", dto.ImageUrl);
            p.Add("IsActive", dto.IsActive);
            p.Add("SortOrder", dto.SortOrder);
            return p;
        }
    }
}
