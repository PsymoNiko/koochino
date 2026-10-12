using System;

namespace MyShop.Core.Models
{
    public class UserActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ActivityType { get; set; } // Login, Logout, Heartbeat
        public string SessionId { get; set; }
        public string ConnectionId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class OnlineUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string RoleName { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public int ActiveSessions { get; set; }
    }

    public class DailyLoginStat
    {
        public DateTime Date { get; set; }
        public int UniqueLogins { get; set; }
        public int TotalLogins { get; set; }
    }
}
