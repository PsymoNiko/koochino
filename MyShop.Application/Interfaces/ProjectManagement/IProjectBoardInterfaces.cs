using MyShop.Core.Models.ProjectManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Application.Interfaces.ProjectManagement
{
    public interface IProjectBoardRepository
    {
        Task<List<ProjectDto>> ListForUserAsync(int userId, bool isFullAccess, int? siteId = null);
        Task<List<ProjectPhase>> ListPhasesAsync(int projectId);
        Task<int> UpsertPhaseAsync(ProjectPhase phase);
        Task DeletePhaseAsync(int id);
        Task<List<ProjectMemberItem>> ListMembersAsync(int projectId);
        Task<int> UpsertMemberAsync(int projectId, int userId, string role);
        Task DeleteMemberAsync(int id);
        Task<List<ProjectTaskItem>> ListTasksAsync(int projectId);
        Task<ProjectTaskItem> GetTaskAsync(int id);
        Task<int> UpsertTaskAsync(ProjectTaskItem task);
        Task DeleteTaskAsync(int id);
        Task DeactivateTaskAsync(int id);
        Task MoveTaskAsync(int id, int? phaseId, int sortOrder);
        Task UpdateProjectProgressAsync(int projectId, byte progress);
        Task SetProjectSiteIdAsync(int projectId, int? siteId);
        Task<List<ProjectTaskAssignee>> ListAssigneesAsync(int taskId);
        Task ReplaceAssigneesAsync(int taskId, IEnumerable<int> userIds);
        Task<List<ProjectCommentItem>> ListCommentsAsync(int projectId, int? taskId);
        Task<int> InsertCommentAsync(ProjectCommentItem comment);
        Task<List<AdminUserOption>> ListAdminsAsync();
        Task InsertActivityAsync(int projectId, int? taskId, int? userId, string eventType, string message);
        Task<List<ProjectActivityLogItem>> ListActivityAsync(int? projectId, int take = 200);
        Task<List<ProjectTaskEventItem>> ListTaskEventsAsync(int taskId);
        Task<int> InsertTaskEventAsync(ProjectTaskEventItem item);
        Task InsertNotificationsAsync(IEnumerable<int> userIds, int projectId, int taskId, string title);
        Task<List<ProjectTaskNotificationItem>> ListNotificationsAsync(int userId, bool unreadOnly = false, int take = 50);
        Task<int> CountUnreadNotificationsAsync(int userId);
        Task MarkNotificationReadAsync(int id, int userId);
        Task MarkAllNotificationsReadAsync(int userId);
    }

    public interface IProjectBoardService
    {
        Task<List<ProjectDto>> ListProjectsForUserAsync(int userId, bool isFullAccess, int? siteId = null, IReadOnlyList<int> allowedSiteIds = null);
        Task<ProjectBoardDto> GetBoardAsync(int projectId);
        Task<int> UpsertPhaseAsync(ProjectPhase phase);
        Task DeletePhaseAsync(int id);
        Task<List<ProjectMemberItem>> ListMembersAsync(int projectId);
        Task<int> UpsertMemberAsync(int projectId, int userId, string role);
        Task DeleteMemberAsync(int id);
        Task<ProjectTaskItem> GetTaskAsync(int id);
        Task<int> UpsertTaskAsync(ProjectTaskItem task, IEnumerable<int> assigneeUserIds, int actingUserId);
        Task<(bool ok, string error)> DeleteOrDeactivateTaskAsync(int taskId, int actingUserId);
        Task DeleteTaskAsync(int id);
        Task MoveTaskAsync(int id, int? phaseId, int sortOrder, int actingUserId);
        Task<byte> RecalculateProgressAsync(int projectId);
        Task<List<ProjectCommentItem>> ListCommentsAsync(int projectId, int? taskId);
        Task<int> AddCommentAsync(ProjectCommentItem comment);
        Task<List<AdminUserOption>> ListAdminsAsync();
        Task EnsureDefaultPhasesAsync(int projectId);
        Task SetProjectSiteIdAsync(int projectId, int? siteId);
        Task LogActivityAsync(int projectId, int? taskId, int? userId, string eventType, string message);
        Task<List<ProjectActivityLogItem>> ListActivityAsync(int? projectId = null, int take = 200);
        Task<List<ProjectTaskEventItem>> ListTaskEventsAsync(int taskId);
        Task<int> AddTaskEventAsync(ProjectTaskEventItem item);
        Task<List<ProjectTaskNotificationItem>> ListNotificationsAsync(int userId, bool unreadOnly = false);
        Task<int> CountUnreadNotificationsAsync(int userId);
        Task MarkNotificationReadAsync(int id, int userId);
        Task MarkAllNotificationsReadAsync(int userId);
        bool CanHardDeleteTask(ProjectTaskItem task, int actingUserId);
        bool CanDeactivateTask(ProjectTaskItem task, int actingUserId, IEnumerable<ProjectMemberItem> members);
    }
}
