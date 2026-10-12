using MyShop.Application.Interfaces.Messaging;
using MyShop.Core.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Messaging
{
    public class SmsTemplateService : ISmsTemplateService
    {
        private readonly ISmsTemplateRepository _repo;
        public SmsTemplateService(ISmsTemplateRepository repo) => _repo = repo;

        public Task<List<SmsTemplate>> ListAsync(bool activeOnly = false) => _repo.ListAsync(activeOnly);
        public Task<SmsTemplate> GetByCodeAsync(string code) => _repo.GetByCodeAsync(code);
        public Task<int> UpsertAsync(SmsTemplate template) => _repo.UpsertAsync(template);
        public Task DeleteAsync(int id) => _repo.DeleteAsync(id);

        public string Render(string bodyTemplate, IDictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(bodyTemplate)) return "";
            var text = bodyTemplate;
            if (parameters == null || parameters.Count == 0) return text;
            foreach (var kv in parameters)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                var key = kv.Key.Trim();
                var value = kv.Value ?? "";
                text = Regex.Replace(text, @"\{\{\s*" + Regex.Escape(key) + @"\s*\}\}", value, RegexOptions.IgnoreCase);
            }
            return text;
        }
    }
}
