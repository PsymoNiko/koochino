namespace MyShop.Core.Models
{
    public class StoreCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }

    public class InviteStats
    {
        public string InviteCode { get; set; } = string.Empty;
        public string InviteLink { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public int StoreOwnerCount { get; set; }
        public int PendingActivationCount { get; set; }
        public int TotalInvitedCount { get; set; }
    }
}
