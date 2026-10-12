using System;

namespace MyShop.Core.Models.Academy
{
    public class AcademyNotificationLogDto
    {
        public int Id { get; set; }
        public string EventCode { get; set; }
        public int? StudentId { get; set; }
        public string Phone { get; set; }
        public string RelatedType { get; set; }
        public int? RelatedId { get; set; }
        public string TemplateCode { get; set; }
        public string MessageText { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public string ExternalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StudentName { get; set; }
    }
}
