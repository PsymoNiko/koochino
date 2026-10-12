using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.SupportChat
{
    public class SupportCsatRating
    {
        public long Id { get; set; }
        public Guid ChatId { get; set; }
        public int? SiteId { get; set; }
        public byte Score { get; set; }
        public string Comment { get; set; }
        public int? RatedByUserId { get; set; }
        public string GuestToken { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupportAnalyticsFilter
    {
        public int? SiteId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int? OperatorUserId { get; set; }
    }

    public class SupportAnalyticsSummary
    {
        public int TotalChats { get; set; }
        public int WaitingChats { get; set; }
        public int ActiveChats { get; set; }
        public int ClosedChats { get; set; }
        public double? AvgFirstResponseMinutes { get; set; }
        public double? AvgResolutionMinutes { get; set; }
        public double? AvgCsat { get; set; }
        public int CsatCount { get; set; }
        public int MessagesFromUsers { get; set; }
        public int MessagesFromAdmins { get; set; }
    }

    public class SupportDailyStat
    {
        public DateTime Day { get; set; }
        public int ChatsStarted { get; set; }
        public int ChatsClosed { get; set; }
        public int CsatCount { get; set; }
        public double? AvgCsat { get; set; }
    }

    public class SupportOperatorStat
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public int AssignedChats { get; set; }
        public int ClosedChats { get; set; }
        public int MessagesSent { get; set; }
        public double? AvgFirstResponseMinutes { get; set; }
        public double? AvgResolutionMinutes { get; set; }
        public double? AvgCsat { get; set; }
        public int CsatCount { get; set; }
    }
}
