namespace MyShop.Core.Models.Dto
{
    /// <summary>
    /// خلاصه امتیاز کاربر
    /// </summary>
    public class UserPointsSummaryDto
    {
        public int TotalPoints { get; set; }
        public int AvailablePoints { get; set; }
        public int PendingPoints { get; set; }
        public int RejectedPoints { get; set; }
    }
}
