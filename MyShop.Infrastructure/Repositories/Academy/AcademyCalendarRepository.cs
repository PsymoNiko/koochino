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
    public class AcademyCalendarRepository : IAcademyCalendarRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyCalendarRepository(IDbConnectionFactory db) => _db = db;

        public async Task<List<AcademyCalendarEventDto>> GetAsync(
            DateTime from, DateTime to, int? instructorId = null, int? studentId = null, string kind = null)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyCalendarEventDto>(
                "koch.spAcademyCalendar_List",
                new
                {
                    From = from,
                    To = to,
                    InstructorId = instructorId,
                    StudentId = studentId,
                    Kind = string.IsNullOrWhiteSpace(kind) ? null : kind
                },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyCalendarEventDto>();
        }
    }
}
