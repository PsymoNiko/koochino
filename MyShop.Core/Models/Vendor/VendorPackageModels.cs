using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Vendor
{
    public class VendorPackage
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; } = 30;
        public string FeaturesJson { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<VendorPackageModule> Modules { get; set; } = new();
    }

    public class VendorPackageModule
    {
        public int PackageId { get; set; }
        public string ModuleKey { get; set; } = "";
        public string LimitsJson { get; set; }
    }

    public class VendorSubscription
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public int PackageId { get; set; }
        public string Status { get; set; } = VendorSubscriptionStatuses.Pending;
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string PaymentRef { get; set; }
        public string PaymentGateway { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PackageName { get; set; }
        public decimal PackagePrice { get; set; }
    }

    public static class VendorSubscriptionStatuses
    {
        public const string Pending = "Pending";
        public const string Active = "Active";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";
    }
}
