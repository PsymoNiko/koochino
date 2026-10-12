using System;

namespace MyShop.Core.Models.Loyalty
{
    public class SmsCampaign
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Title { get; set; } = "";
        public string MessageText { get; set; } = "";
        public string AudienceType { get; set; } = "AllCustomers";
        public int? SegmentId { get; set; }
        public string ManualPhones { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime? ScheduledAt { get; set; }
        public int SentCount { get; set; }
        public int FailCount { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }

    public class CustomerSegment
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; }
        public string RuleJson { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerRfmScore
    {
        public int UserId { get; set; }
        public int? SiteId { get; set; }
        public byte RecencyScore { get; set; } = 1;
        public byte FrequencyScore { get; set; } = 1;
        public byte MonetaryScore { get; set; } = 1;
        public DateTime? LastOrderAt { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpend { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PhoneNumber { get; set; }
        public string DisplayName { get; set; }
    }

    public class VipTier
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = "";
        public int MinPoints { get; set; }
        public int SortOrder { get; set; }
        public string ColorHex { get; set; }
        public string BenefitsJson { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class WalletTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string TxType { get; set; } = "Credit";
        public string Reason { get; set; }
        public string ReferenceId { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NpsResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? OrderId { get; set; }
        public byte Score { get; set; }
        public string Comment { get; set; }
        public int? SiteId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LoyaltyAutomation
    {
        public int Id { get; set; }
        public int? SiteId { get; set; }
        public string Name { get; set; } = "";
        public string TriggerType { get; set; } = "Birthday";
        public int? TriggerValue { get; set; }
        public string ActionType { get; set; } = "Sms";
        public string ActionPayload { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastRunAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
