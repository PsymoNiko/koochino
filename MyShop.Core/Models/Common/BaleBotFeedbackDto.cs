using System;

namespace MyShop.Core.Models.Dto
{
    public class BaleBotFeedbackDto
    {
        public int Id { get; set; }
        public long BaleUserId { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Choice { get; set; } = "";  // "good" or "bad"
        public DateTime CreatedAt { get; set; }
    }
}
