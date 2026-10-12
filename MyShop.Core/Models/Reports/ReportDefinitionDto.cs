using System;

namespace MyShop.Core.Models.Reports
{
    public class ReportDefinitionDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ModuleKey { get; set; }
        public string QueryOrConfig { get; set; }
        public string OutputType { get; set; }
        public string ParametersJson { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateReportDefinitionDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ModuleKey { get; set; }
        public string QueryOrConfig { get; set; }
        public string OutputType { get; set; }
        public string ParametersJson { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpdateReportDefinitionDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ModuleKey { get; set; }
        public string QueryOrConfig { get; set; }
        public string OutputType { get; set; }
        public string ParametersJson { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}
