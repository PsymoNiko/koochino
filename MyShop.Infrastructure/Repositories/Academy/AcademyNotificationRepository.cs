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
    public class AcademyNotificationRepository : IAcademyNotificationRepository
    {
        private readonly IDbConnectionFactory _db;
        public AcademyNotificationRepository(IDbConnectionFactory db) => _db = db;

        public async Task<int> InsertAsync(AcademyNotificationLogDto row)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("EventCode", row.EventCode);
            p.Add("StudentId", row.StudentId);
            p.Add("Phone", row.Phone);
            p.Add("RelatedType", row.RelatedType);
            p.Add("RelatedId", row.RelatedId);
            p.Add("TemplateCode", row.TemplateCode);
            p.Add("MessageText", row.MessageText);
            p.Add("Success", row.Success);
            p.Add("Error", row.Error);
            p.Add("ExternalId", row.ExternalId);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyNotificationLog_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Id");
        }

        public async Task<List<AcademyNotificationLogDto>> GetAllAsync(string eventCode = null, int? studentId = null, bool? success = null, int top = 200)
        {
            using var conn = _db.CreateConnection();
            var list = await conn.QueryAsync<AcademyNotificationLogDto>(
                "koch.spAcademyNotificationLog_List",
                new { EventCode = eventCode, StudentId = studentId, Success = success, Top = top },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AcademyNotificationLogDto>();
        }

        public async Task<bool> ExistsSuccessfulAsync(string eventCode, int studentId, string relatedType, int relatedId, DateTime since)
        {
            using var conn = _db.CreateConnection();
            var p = new DynamicParameters();
            p.Add("EventCode", eventCode);
            p.Add("StudentId", studentId);
            p.Add("RelatedType", relatedType);
            p.Add("RelatedId", relatedId);
            p.Add("Since", since);
            p.Add("Found", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spAcademyNotificationLog_Exists", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("Found") == 1;
        }
    }
}
