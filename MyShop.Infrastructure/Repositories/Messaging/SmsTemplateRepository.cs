using Dapper;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Messaging;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repositories.Messaging
{
    public class SmsTemplateRepository : ISmsTemplateRepository
    {
        private readonly IDbConnectionFactory _factory;
        public SmsTemplateRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<SmsTemplate>> ListAsync(bool activeOnly = false)
        {
            using var conn = _factory.CreateConnection();
            try
            {
                var rows = await conn.QueryAsync<SmsTemplate>(
                    "koch.spSmsTemplates_List",
                    new { ActiveOnly = activeOnly ? 1 : 0 },
                    commandType: CommandType.StoredProcedure);
                return rows?.ToList() ?? new List<SmsTemplate>();
            }
            catch { return new List<SmsTemplate>(); }
        }

        public async Task<SmsTemplate> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            using var conn = _factory.CreateConnection();
            try
            {
                return await conn.QueryFirstOrDefaultAsync<SmsTemplate>(
                    "koch.spSmsTemplates_GetByCode",
                    new { Code = code.Trim() },
                    commandType: CommandType.StoredProcedure);
            }
            catch { return null; }
        }

        public async Task<int> UpsertAsync(SmsTemplate template)
        {
            using var conn = _factory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", template.Id > 0 ? template.Id : (int?)null);
            p.Add("Code", template.Code?.Trim());
            p.Add("Title", template.Title?.Trim());
            p.Add("BodyTemplate", template.BodyTemplate ?? "");
            p.Add("SmsIrTemplateId", template.SmsIrTemplateId);
            p.Add("MeliPayamakBodyId", template.MeliPayamakBodyId);
            p.Add("ParameterNames", template.ParameterNames);
            p.Add("IsActive", template.IsActive);
            p.Add("UseForSystem", template.UseForSystem);
            p.Add("Notes", template.Notes);
            p.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spSmsTemplates_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("NewId");
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync("koch.spSmsTemplates_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }
    }
}
