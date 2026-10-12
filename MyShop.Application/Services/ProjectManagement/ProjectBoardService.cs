using MyShop.Application.Interfaces.ProjectManagement;
using MyShop.Core.Models.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Application.Services.ProjectManagement
{
    public class ProjectBoardService : IProjectBoardService
    {
        private static readonly TimeSpan HardDeleteWindow = TimeSpan.FromMinutes(5);
        private readonly IProjectBoardRepository _repo;
        private readonly IProjectRepository _projects;

        public ProjectBoardService(IProjectBoardRepository repo, IProjectRepository projects)
        {
            _repo = repo;
            _projects = projects;
        }

        public async Task<List<ProjectDto>> ListProjectsForUserAsync(
            int userId, bool isFullAccess, int? siteId = null, IReadOnlyList<int> allowedSiteIds = null)
        {
            var items = await _repo.ListForUserAsync(userId, isFullAccess, siteId) ?? new List<ProjectDto>();
            if (allowedSiteIds != null && allowedSiteIds.Count > 0)
            {
                items = items
                    .Where(p => p.SiteId == null || allowedSiteIds.Contains(p.SiteId.Value))
                    .ToList();
            }
            return items;
        }

        public async Task<ProjectBoardDto> GetBoardAsync(int projectId)
        {
            var project = await _projects.GetByIdAsync(projectId);
            if (project == null) return null;

            var phases = await _repo.ListPhasesAsync(projectId);
            var tasks = await _repo.ListTasksAsync(projectId);
            var members = await _repo.ListMembersAsync(projectId);

            foreach (var t in tasks)
                t.Assignees = await _repo.ListAssigneesAsync(t.Id);

            foreach (var ph in phases)
                ph.Tasks = tasks.Where(t => t.PhaseId == ph.Id).OrderBy(t => t.SortOrder).ThenBy(t => t.Id).ToList();

            return new ProjectBoardDto
            {
                Project = project,
                Phases = phases,
                UnphasedTasks = tasks.Where(t => t.PhaseId == null || t.PhaseId == 0).OrderBy(t => t.SortOrder).ToList(),
                Members = members
            };
        }

        public Task<int> UpsertPhaseAsync(ProjectPhase phase) => _repo.UpsertPhaseAsync(phase);
        public Task DeletePhaseAsync(int id) => _repo.DeletePhaseAsync(id);
        public Task<List<ProjectMemberItem>> ListMembersAsync(int projectId) => _repo.ListMembersAsync(projectId);
        public Task<int> UpsertMemberAsync(int projectId, int userId, string role) =>
            _repo.UpsertMemberAsync(projectId, userId, role);
        public Task DeleteMemberAsync(int id) => _repo.DeleteMemberAsync(id);
        public Task<ProjectTaskItem> GetTaskAsync(int id) => _repo.GetTaskAsync(id);

        public async Task<int> UpsertTaskAsync(ProjectTaskItem task, IEnumerable<int> assigneeUserIds, int actingUserId)
        {
            var isNew = task.Id <= 0;
            if (isNew)
                task.CreatedBy = actingUserId > 0 ? actingUserId : task.CreatedBy;

            var previousAssignees = isNew
                ? new HashSet<int>()
                : new HashSet<int>((await _repo.ListAssigneesAsync(task.Id)).Select(a => a.UserId));

            await SyncTaskStatusFromPhaseAsync(task);
            var id = await _repo.UpsertTaskAsync(task);
            task.Id = id;
            await _repo.ReplaceAssigneesAsync(id, assigneeUserIds);

            var assigneeList = (assigneeUserIds ?? Enumerable.Empty<int>()).Distinct().Where(x => x > 0).ToList();
            if (isNew)
            {
                await _repo.InsertActivityAsync(task.ProjectId, id, actingUserId, "TaskCreated",
                    $"وظیفه «{task.Title}» ایجاد شد.");
                var notifyUsers = assigneeList.Where(u => u != actingUserId).ToList();
                if (notifyUsers.Count > 0)
                    await _repo.InsertNotificationsAsync(notifyUsers, task.ProjectId, id, task.Title);
            }
            else
            {
                await _repo.InsertActivityAsync(task.ProjectId, id, actingUserId, "TaskUpdated",
                    $"وظیفه «{task.Title}» به‌روزرسانی شد.");
                var newlyAssigned = assigneeList.Where(u => !previousAssignees.Contains(u) && u != actingUserId).ToList();
                if (newlyAssigned.Count > 0)
                    await _repo.InsertNotificationsAsync(newlyAssigned, task.ProjectId, id, task.Title);
            }

            if (task.ProjectId > 0)
                await RecalculateProgressAsync(task.ProjectId);
            return id;
        }

        public bool CanHardDeleteTask(ProjectTaskItem task, int actingUserId)
        {
            if (task == null || actingUserId <= 0) return false;
            if (task.CreatedBy != actingUserId) return false;
            var created = task.CreatedAt == default ? DateTime.UtcNow : task.CreatedAt;
            // DB often stores local/server time; compare with both utc and local safely
            var age = DateTime.Now - created;
            if (age < TimeSpan.Zero) age = DateTime.UtcNow - created.ToUniversalTime();
            return age <= HardDeleteWindow;
        }

        public bool CanDeactivateTask(ProjectTaskItem task, int actingUserId, IEnumerable<ProjectMemberItem> members)
        {
            if (task == null || actingUserId <= 0) return false;
            if (task.CreatedBy == actingUserId) return true;
            return members?.Any(m => m.UserId == actingUserId &&
                string.Equals(m.Role, ProjectMemberRoles.Owner, StringComparison.OrdinalIgnoreCase)) == true;
        }

        public async Task<(bool ok, string error)> DeleteOrDeactivateTaskAsync(int taskId, int actingUserId)
        {
            var task = await _repo.GetTaskAsync(taskId);
            if (task == null) return (false, "وظیفه یافت نشد.");

            var members = await _repo.ListMembersAsync(task.ProjectId);
            if (CanHardDeleteTask(task, actingUserId))
            {
                await _repo.DeleteTaskAsync(taskId);
                await _repo.InsertActivityAsync(task.ProjectId, taskId, actingUserId, "TaskDeleted",
                    $"وظیفه «{task.Title}» حذف شد.");
                if (task.ProjectId > 0) await RecalculateProgressAsync(task.ProjectId);
                return (true, null);
            }

            if (CanDeactivateTask(task, actingUserId, members))
            {
                await _repo.DeactivateTaskAsync(taskId);
                await _repo.InsertActivityAsync(task.ProjectId, taskId, actingUserId, "TaskDeactivated",
                    $"وظیفه «{task.Title}» غیرفعال شد.");
                if (task.ProjectId > 0) await RecalculateProgressAsync(task.ProjectId);
                return (true, null);
            }

            return (false, "اجازه حذف/غیرفعال‌سازی این وظیفه را ندارید. فقط سازنده (تا ۵ دقیقه) یا Owner پروژه می‌تواند.");
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _repo.GetTaskAsync(id);
            await _repo.DeleteTaskAsync(id);
            if (task?.ProjectId > 0)
                await RecalculateProgressAsync(task.ProjectId);
        }

        public async Task MoveTaskAsync(int id, int? phaseId, int sortOrder, int actingUserId)
        {
            var task = await _repo.GetTaskAsync(id);
            if (task == null) return;

            await _repo.MoveTaskAsync(id, phaseId, sortOrder);
            task.PhaseId = phaseId;
            await SyncTaskStatusFromPhaseAsync(task);
            await _repo.UpsertTaskAsync(task);
            await _repo.InsertActivityAsync(task.ProjectId, id, actingUserId, "TaskMoved",
                $"وظیفه «{task.Title}» جابه‌جا شد.");
            await RecalculateProgressAsync(task.ProjectId);
        }

        public async Task<byte> RecalculateProgressAsync(int projectId)
        {
            var phases = (await _repo.ListPhasesAsync(projectId) ?? new List<ProjectPhase>())
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToList();
            var tasks = await _repo.ListTasksAsync(projectId) ?? new List<ProjectTaskItem>();

            byte progress = 0;
            if (tasks.Count > 0)
            {
                var maxOrder = phases.Count > 0 ? Math.Max(1, phases.Max(p => p.SortOrder)) : 1;
                var lastPhaseId = phases.LastOrDefault()?.Id;
                double sum = 0;
                foreach (var t in tasks)
                {
                    if (string.Equals(t.Status, "Done", StringComparison.OrdinalIgnoreCase) || t.CompletedAt.HasValue)
                    {
                        sum += 100;
                        continue;
                    }
                    if (t.PhaseId == null || t.PhaseId <= 0)
                        continue;
                    if (lastPhaseId.HasValue && t.PhaseId == lastPhaseId.Value)
                    {
                        sum += 100;
                        continue;
                    }
                    var phase = phases.FirstOrDefault(p => p.Id == t.PhaseId);
                    if (phase == null) continue;
                    sum += Math.Min(100, (phase.SortOrder * 100.0) / maxOrder);
                }
                progress = (byte)Math.Clamp((int)Math.Round(sum / tasks.Count), 0, 100);
            }

            await _repo.UpdateProjectProgressAsync(projectId, progress);

            var project = await _projects.GetByIdAsync(projectId);
            if (project != null && progress >= 100 &&
                !string.Equals(project.Status, "Done", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(project.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                await _projects.UpdateAsync(new Project
                {
                    Id = project.Id,
                    StoreId = project.StoreId,
                    CompanyId = project.CompanyId,
                    SiteId = project.SiteId,
                    Title = project.Title,
                    Description = project.Description,
                    Status = "Done",
                    Priority = project.Priority,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                    Progress = progress
                });
            }

            return progress;
        }

        private async Task SyncTaskStatusFromPhaseAsync(ProjectTaskItem task)
        {
            if (task == null || task.ProjectId <= 0) return;
            var phases = (await _repo.ListPhasesAsync(task.ProjectId) ?? new List<ProjectPhase>())
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToList();
            if (phases.Count == 0) return;
            var last = phases.Last();
            if (task.PhaseId == last.Id)
            {
                task.Status = "Done";
                task.Progress = 100;
                task.CompletedAt ??= DateTime.UtcNow;
            }
            else if (string.Equals(task.Status, "Done", StringComparison.OrdinalIgnoreCase) && task.PhaseId != last.Id)
            {
                task.Status = "Doing";
                var order = phases.FirstOrDefault(p => p.Id == task.PhaseId)?.SortOrder ?? 0;
                task.Progress = (byte)Math.Clamp((int)(order * 100.0 / Math.Max(1, last.SortOrder)), 0, 99);
                task.CompletedAt = null;
            }
            else if (phases.Count > 1 && task.PhaseId == phases[0].Id)
            {
                task.Status = "Todo";
                task.Progress = 0;
            }
            else if (task.PhaseId.HasValue && task.PhaseId > 0)
            {
                task.Status = "Doing";
                var order = phases.FirstOrDefault(p => p.Id == task.PhaseId)?.SortOrder ?? 0;
                task.Progress = (byte)Math.Clamp((int)(order * 100.0 / Math.Max(1, last.SortOrder)), 0, 99);
            }
        }

        public Task<List<ProjectCommentItem>> ListCommentsAsync(int projectId, int? taskId) =>
            _repo.ListCommentsAsync(projectId, taskId);

        public async Task<int> AddCommentAsync(ProjectCommentItem comment)
        {
            var id = await _repo.InsertCommentAsync(comment);
            await _repo.InsertActivityAsync(comment.ProjectId, comment.TaskId, comment.UserId, "Comment",
                "نظر جدید ثبت شد.");
            return id;
        }

        public Task<List<AdminUserOption>> ListAdminsAsync() => _repo.ListAdminsAsync();
        public Task SetProjectSiteIdAsync(int projectId, int? siteId) => _repo.SetProjectSiteIdAsync(projectId, siteId);
        public Task LogActivityAsync(int projectId, int? taskId, int? userId, string eventType, string message) =>
            _repo.InsertActivityAsync(projectId, taskId, userId, eventType, message);
        public Task<List<ProjectActivityLogItem>> ListActivityAsync(int? projectId = null, int take = 200) =>
            _repo.ListActivityAsync(projectId, take);
        public Task<List<ProjectTaskEventItem>> ListTaskEventsAsync(int taskId) => _repo.ListTaskEventsAsync(taskId);

        public async Task<int> AddTaskEventAsync(ProjectTaskEventItem item)
        {
            var id = await _repo.InsertTaskEventAsync(item);
            await _repo.InsertActivityAsync(item.ProjectId, item.TaskId, item.UserId, "TaskEvent",
                "رویداد وضعیت روی وظیفه ثبت شد.");
            return id;
        }

        public Task<List<ProjectTaskNotificationItem>> ListNotificationsAsync(int userId, bool unreadOnly = false) =>
            _repo.ListNotificationsAsync(userId, unreadOnly);
        public Task<int> CountUnreadNotificationsAsync(int userId) => _repo.CountUnreadNotificationsAsync(userId);
        public Task MarkNotificationReadAsync(int id, int userId) => _repo.MarkNotificationReadAsync(id, userId);
        public Task MarkAllNotificationsReadAsync(int userId) => _repo.MarkAllNotificationsReadAsync(userId);

        public async Task EnsureDefaultPhasesAsync(int projectId)
        {
            var phases = await _repo.ListPhasesAsync(projectId);
            if (phases.Count > 0) return;
            var defaults = new[]
            {
                ("برای انجام", 10, "#94a3b8"),
                ("در حال انجام", 20, "#3b82f6"),
                ("بازبینی", 30, "#f59e0b"),
                ("انجام شد", 40, "#10b981")
            };
            foreach (var (title, order, color) in defaults)
            {
                await _repo.UpsertPhaseAsync(new ProjectPhase
                {
                    ProjectId = projectId,
                    Title = title,
                    SortOrder = order,
                    Color = color
                });
            }
        }
    }
}
