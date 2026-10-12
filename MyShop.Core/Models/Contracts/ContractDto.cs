using System;

namespace MyShop.Core.Models.Contracts
{
    public class ContractDto
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string ContractNo { get; set; }
        public string Title { get; set; }
        public string PartyName { get; set; }
        public string PartyContact { get; set; }
        public string PartyPhone { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Value { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string DocumentUrl { get; set; }
        public string Notes { get; set; }
        public int? ReminderDays { get; set; }
        public DateTime? ReminderSentAt { get; set; }
        public int RenewalCount { get; set; }
        public int? ParentContractId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateContractDto
    {
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string ContractNo { get; set; }
        public string Title { get; set; }
        public string PartyName { get; set; }
        public string PartyContact { get; set; }
        public string PartyPhone { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Value { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string DocumentUrl { get; set; }
        public string Notes { get; set; }
        public int? ReminderDays { get; set; }
        public int? ParentContractId { get; set; }
    }

    public class UpdateContractDto
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? StoreId { get; set; }
        public string ContractNo { get; set; }
        public string Title { get; set; }
        public string PartyName { get; set; }
        public string PartyContact { get; set; }
        public string PartyPhone { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Value { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string DocumentUrl { get; set; }
        public string Notes { get; set; }
        public int? ReminderDays { get; set; }
        public int? ParentContractId { get; set; }
    }
}
