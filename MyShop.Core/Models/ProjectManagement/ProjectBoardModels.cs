using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.ProjectManagement
{
    public static class ProjectMemberRoles
    {
        public const string Owner = "Owner";
        public const string Member = "Member";
        public const string Viewer = "Viewer";
    }

    public class ProjectPhase
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public int SortOrder { get; set; }
        public string Color { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProjectTaskItem> Tasks { get; set; } = new();
    }

    public class ProjectTaskItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? PhaseId { get; set; }
        public int? ParentTaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? AssigneeId { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Labels { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int SortOrder { get; set; }
        public byte Progress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public List<ProjectTaskAssignee> Assignees { get; set; } = new();
    }

    public class ProjectTaskAssignee
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : (Username ?? ("#" + UserId));
    }

    public class ProjectMemberItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? UserId { get; set; }
        public int? PersonnelId { get; set; }
        public string Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : (Username ?? PhoneNumber ?? ("#" + UserId));
    }

    public class ProjectCommentItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? TaskId { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : (Username ?? ("#" + UserId));
    }

    public class AdminUserOption
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : (Username ?? PhoneNumber ?? ("#" + Id));
    }

    public class ProjectBoardDto
    {
        public ProjectDto Project { get; set; }
        public List<ProjectPhase> Phases { get; set; } = new();
        public List<ProjectTaskItem> UnphasedTasks { get; set; } = new();
        public List<ProjectMemberItem> Members { get; set; } = new();
    }

    public class ProjectActivityLogItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? TaskId { get; set; }
        public int? UserId { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProjectTitle { get; set; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : (Username ?? ("#" + UserId));
    }

    public class ProjectTaskEventItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TaskId { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : (Username ?? ("#" + UserId));
    }

    public class ProjectTaskNotificationItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int TaskId { get; set; }
        public string Title { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ProjectTitle { get; set; }
    }
}
