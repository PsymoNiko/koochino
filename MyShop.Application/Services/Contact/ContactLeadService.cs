using MyShop.Application.Interfaces.Contact;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.Contact;
using MyShop.Core.Models.Crm;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MyShop.Application.Services.Contact
{
    public class ContactLeadService : IContactLeadService
    {
        private readonly IContactLeadRepository _repo;
        private readonly ICurrentTenant _tenant;

        public ContactLeadService(IContactLeadRepository repo, ICurrentTenant tenant)
        {
            _repo = repo;
            _tenant = tenant;
        }

        public async Task<(bool ok, string message)> SubmitAsync(int? siteId, string phone, string email, string source = "landing")
        {
            siteId = SiteScope.CurrentSiteId(_tenant) ?? siteId;
            if (!siteId.HasValue || siteId.Value <= 0)
                return (false, "سایت درخواست مشخص نیست.");

            var normalizedPhone = PhoneNormalizer.Normalize(phone);
            var normalizedEmail = (email ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone.Length < 10 || normalizedPhone.Length > 13)
                return (false, "شماره تماس معتبر وارد کنید.");
            if (!string.IsNullOrWhiteSpace(normalizedEmail) && !IsValidEmail(normalizedEmail))
                return (false, "ایمیل معتبر وارد کنید.");

            if (await _repo.ExistsRecentAsync(siteId, normalizedPhone, normalizedEmail, 24))
                return (true, "این مشخصات قبلاً ثبت شده است. به‌زودی تماس می‌گیریم.");

            await _repo.InsertAsync(new ContactLead
            {
                SiteId = siteId is > 0 ? siteId : null,
                Phone = normalizedPhone,
                Email = normalizedEmail,
                Source = string.IsNullOrWhiteSpace(source) ? "landing" : source.Trim()
            });
            return (true, "ثبت شد. به‌زودی با شما تماس می‌گیریم.");
        }

        public Task<List<ContactLead>> ListAsync(int? siteId, int top = 300)
        {
            var currentSiteId = SiteScope.CurrentSiteId(_tenant);
            if (!currentSiteId.HasValue || (siteId.HasValue && siteId.Value != currentSiteId.Value))
                return Task.FromResult(new List<ContactLead>());
            return _repo.ListAsync(currentSiteId.Value, top);
        }

        public Task SetReadAsync(int id, bool isRead)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue ? _repo.SetReadAsync(id, siteId.Value, isRead) : Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(int id)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue ? _repo.DeleteAsync(id, siteId.Value) : Task.FromResult(false);
        }

        public Task<CrmContactDto> ConvertAsync(int id, int? createdBy = null)
        {
            var siteId = SiteScope.CurrentSiteId(_tenant);
            return siteId.HasValue
                ? _repo.ConvertAsync(id, siteId.Value, createdBy)
                : Task.FromResult<CrmContactDto>(null);
        }

        static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 200) return false;
            try
            {
                var addr = new MailAddress(email);
                return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

    }
}
