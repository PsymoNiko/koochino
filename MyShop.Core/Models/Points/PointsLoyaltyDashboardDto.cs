using System.Collections.Generic;

namespace MyShop.Core.Models.Dto
{
    public class PointsLoyaltyDashboardDto
    {
        public int MissionsTotal { get; set; }
        public int MissionsActive { get; set; }
        public int MissionsInactive { get; set; }
        public int MissionsExpired { get; set; }
        public int MissionsUpcoming { get; set; }
        public int TxPending { get; set; }
        public int TxApprovedMonth { get; set; }
        public int TxRejectedMonth { get; set; }
        public int TxTotalMonth { get; set; }
        public long PointsApprovedMonth { get; set; }
        public long PointsPendingTotal { get; set; }
        public int UsersWithPoints { get; set; }
        public long TotalAvailablePoints { get; set; }
        public int UsersWithPending { get; set; }
        public List<string> DailyLabels { get; set; } = new();
        public List<int> DailyApproved { get; set; } = new();
        public List<int> DailyPending { get; set; } = new();
        public List<string> TopMissionLabels { get; set; } = new();
        public List<int> TopMissionValues { get; set; } = new();
    }
}
