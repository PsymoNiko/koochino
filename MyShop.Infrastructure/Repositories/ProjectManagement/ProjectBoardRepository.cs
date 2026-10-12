using Dapper;
using MyShop.Application.Interfaces.ProjectManagement;
using MyShop.Core.Interfaces;
using MyShop.Core.Models.ProjectManagement;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Infrastructure.Repository
{
    public class ProjectBoardRepository : IProjectBoardRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProjectBoardRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<ProjectDto>> ListForUserAsync(int userId, bool isFullAccess, int? siteId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<ProjectDto>(
                    "koch.spProjects_ListForUser",
                    new { UserId = userId, IsFullAccess = isFullAccess, SiteId = siteId },
                    commandType: CommandType.StoredProcedure);
                return list?.ToList() ?? new List<ProjectDto>();
            }
            catch
            {
                // Fallback if SP not yet updated with SiteId param
                var list = await conn.QueryAsync<ProjectDto>(
                    "koch.spProjects_ListForUser",
                    new { UserId = userId, IsFullAccess = isFullAccess },
                    commandType: CommandType.StoredProcedure);
                var items = list?.ToList() ?? new List<ProjectDto>();
                if (siteId.HasValue)
                    items = items.Where(p => p.SiteId == null || p.SiteId == siteId).ToList();
                return items;
            }
        }

        public async Task<List<ProjectPhase>> ListPhasesAsync(int projectId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ProjectPhase>(
                "koch.spProjectPhases_List", new { ProjectId = projectId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<ProjectPhase>();
        }

        public async Task<int> UpsertPhaseAsync(ProjectPhase phase)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", phase.Id > 0 ? phase.Id : (int?)null);
            p.Add("ProjectId", phase.ProjectId);
            p.Add("Title", phase.Title);
            p.Add("SortOrder", phase.SortOrder);
            p.Add("Color", phase.Color);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProjectPhases_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task DeletePhaseAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spProjectPhases_Delete", new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<ProjectMemberItem>> ListMembersAsync(int projectId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ProjectMemberItem>(
                "koch.spProjectMembers_List", new { ProjectId = projectId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<ProjectMemberItem>();
        }

        public async Task<int> UpsertMemberAsync(int projectId, int userId, string role)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("ProjectId", projectId);
            p.Add("UserId", userId);
            p.Add("Role", role ?? ProjectMemberRoles.Member);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProjectMembers_Upsert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task DeleteMemberAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spProjectMembers_Delete", new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<ProjectTaskItem>> ListTasksAsync(int projectId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ProjectTaskItem>(
                "koch.spProjectTasks_ListByProject", new { ProjectId = projectId },
                commandType: CommandType.StoredProcedure);
            var items = list?.ToList() ?? new List<ProjectTaskItem>();
            return items.Where(t => t.IsActive).ToList();
        }

        public async Task<ProjectTaskItem> GetTaskAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var task = await conn.QueryFirstOrDefaultAsync<ProjectTaskItem>(
                "koch.spProjectTasks_GetById", new { Id = id },
                commandType: CommandType.StoredProcedure);
            if (task != null)
                task.Assignees = await ListAssigneesAsync(id);
            return task;
        }

        public async Task<int> UpsertTaskAsync(ProjectTaskItem task)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("Id", task.Id > 0 ? task.Id : (int?)null);
            p.Add("ProjectId", task.ProjectId);
            p.Add("PhaseId", task.PhaseId);
            p.Add("Title", task.Title);
            p.Add("Description", task.Description);
            p.Add("Status", task.Status ?? "Todo");
            p.Add("Priority", task.Priority);
            p.Add("DueDate", task.DueDate);
            p.Add("StartDate", task.StartDate);
            p.Add("EndDate", task.EndDate);
            p.Add("Labels", task.Labels);
            p.Add("SortOrder", task.SortOrder);
            p.Add("Progress", task.Progress);
            p.Add("CreatedBy", task.CreatedBy);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            try
            {
                await conn.ExecuteAsync("koch.spProjectTasks_Upsert", p, commandType: CommandType.StoredProcedure);
            }
            catch
            {
                p = new DynamicParameters();
                p.Add("Id", task.Id > 0 ? task.Id : (int?)null);
                p.Add("ProjectId", task.ProjectId);
                p.Add("PhaseId", task.PhaseId);
                p.Add("Title", task.Title);
                p.Add("Description", task.Description);
                p.Add("Status", task.Status ?? "Todo");
                p.Add("Priority", task.Priority);
                p.Add("DueDate", task.DueDate);
                p.Add("StartDate", task.StartDate);
                p.Add("EndDate", task.EndDate);
                p.Add("Labels", task.Labels);
                p.Add("SortOrder", task.SortOrder);
                p.Add("Progress", task.Progress);
                p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("koch.spProjectTasks_Upsert", p, commandType: CommandType.StoredProcedure);
                var idFallback = p.Get<int>("OutId");
                if (task.Id <= 0 && task.CreatedBy.HasValue && task.CreatedBy > 0)
                {
                    try
                    {
                        await conn.ExecuteAsync(
                            "UPDATE koch.ProjectTasks SET CreatedBy = @CreatedBy WHERE Id = @Id AND CreatedBy IS NULL",
                            new { Id = idFallback, CreatedBy = task.CreatedBy });
                    }
                    catch { /* column may not exist yet */ }
                }
                return idFallback;
            }
            return p.Get<int>("OutId");
        }

        public async Task DeleteTaskAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spProjectTasks_Delete", new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeactivateTaskAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                await conn.ExecuteAsync("koch.spProjectTasks_Deactivate", new { Id = id },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                await conn.ExecuteAsync(
                    "UPDATE koch.ProjectTasks SET IsActive = 0, UpdatedAt = SYSDATETIME() WHERE Id = @Id",
                    new { Id = id });
            }
        }

        public async Task MoveTaskAsync(int id, int? phaseId, int sortOrder)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spProjectTasks_Move",
                new { Id = id, PhaseId = phaseId, SortOrder = sortOrder },
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateProjectProgressAsync(int projectId, byte progress)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(
                @"UPDATE koch.Projects
                  SET Progress = @Progress, UpdatedAt = SYSDATETIME()
                  WHERE Id = @ProjectId",
                new { ProjectId = projectId, Progress = progress });
        }

        public async Task SetProjectSiteIdAsync(int projectId, int? siteId)
        {
            if (projectId <= 0) return;
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                await conn.ExecuteAsync(
                    "UPDATE koch.Projects SET SiteId = @SiteId WHERE Id = @Id",
                    new { Id = projectId, SiteId = siteId });
            }
            catch { /* column may not exist until migration */ }
        }

        public async Task<List<ProjectTaskAssignee>> ListAssigneesAsync(int taskId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ProjectTaskAssignee>(
                "koch.spProjectTaskAssignees_List", new { TaskId = taskId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<ProjectTaskAssignee>();
        }

        public async Task ReplaceAssigneesAsync(int taskId, IEnumerable<int> userIds)
        {
            var csv = string.Join(",", (userIds ?? Enumerable.Empty<int>()).Distinct().Where(x => x > 0));
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("koch.spProjectTaskAssignees_Replace",
                new { TaskId = taskId, UserIds = csv },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<ProjectCommentItem>> ListCommentsAsync(int projectId, int? taskId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<ProjectCommentItem>(
                "koch.spProjectComments_List",
                new { ProjectId = projectId, TaskId = taskId },
                commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<ProjectCommentItem>();
        }

        public async Task<int> InsertCommentAsync(ProjectCommentItem comment)
        {
            using var conn = _connectionFactory.CreateConnection();
            var p = new DynamicParameters();
            p.Add("ProjectId", comment.ProjectId);
            p.Add("TaskId", comment.TaskId);
            p.Add("UserId", comment.UserId);
            p.Add("Content", comment.Content);
            p.Add("OutId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("koch.spProjectComments_Insert", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("OutId");
        }

        public async Task<List<AdminUserOption>> ListAdminsAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var list = await conn.QueryAsync<AdminUserOption>(
                "koch.spUsers_ListAdmins", commandType: CommandType.StoredProcedure);
            return list?.ToList() ?? new List<AdminUserOption>();
        }

        public async Task InsertActivityAsync(int projectId, int? taskId, int? userId, string eventType, string message)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                await conn.ExecuteAsync(@"
INSERT INTO koch.ProjectActivityLog (ProjectId, TaskId, UserId, EventType, Message)
VALUES (@ProjectId, @TaskId, @UserId, @EventType, @Message)",
                    new { ProjectId = projectId, TaskId = taskId, UserId = userId, EventType = eventType ?? "Info", Message = message });
            }
            catch { /* table may not exist until migration */ }
        }

        public async Task<List<ProjectActivityLogItem>> ListActivityAsync(int? projectId, int take = 200)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<ProjectActivityLogItem>(@"
SELECT TOP (@Take) a.Id, a.ProjectId, a.TaskId, a.UserId, a.EventType, a.Message, a.CreatedAt,
       u.Username, u.FirstName, u.LastName, p.Title AS ProjectTitle
FROM koch.ProjectActivityLog a
LEFT JOIN koch.Users u ON u.Id = a.UserId
LEFT JOIN koch.Projects p ON p.Id = a.ProjectId
WHERE (@ProjectId IS NULL OR a.ProjectId = @ProjectId)
ORDER BY a.CreatedAt DESC",
                    new { ProjectId = projectId, Take = take });
                return list?.ToList() ?? new List<ProjectActivityLogItem>();
            }
            catch
            {
                return new List<ProjectActivityLogItem>();
            }
        }

        public async Task<List<ProjectTaskEventItem>> ListTaskEventsAsync(int taskId)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<ProjectTaskEventItem>(@"
SELECT e.Id, e.ProjectId, e.TaskId, e.UserId, e.Content, e.CreatedAt,
       u.Username, u.FirstName, u.LastName
FROM koch.ProjectTaskEvents e
LEFT JOIN koch.Users u ON u.Id = e.UserId
WHERE e.TaskId = @TaskId
ORDER BY e.CreatedAt DESC",
                    new { TaskId = taskId });
                return list?.ToList() ?? new List<ProjectTaskEventItem>();
            }
            catch { return new List<ProjectTaskEventItem>(); }
        }

        public async Task<int> InsertTaskEventAsync(ProjectTaskEventItem item)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(@"
INSERT INTO koch.ProjectTaskEvents (ProjectId, TaskId, UserId, Content)
VALUES (@ProjectId, @TaskId, @UserId, @Content);
SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { item.ProjectId, item.TaskId, item.UserId, item.Content });
        }

        public async Task InsertNotificationsAsync(IEnumerable<int> userIds, int projectId, int taskId, string title)
        {
            var ids = (userIds ?? Enumerable.Empty<int>()).Distinct().Where(x => x > 0).ToList();
            if (ids.Count == 0) return;
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                foreach (var uid in ids)
                {
                    await conn.ExecuteAsync(@"
INSERT INTO koch.ProjectTaskNotifications (UserId, ProjectId, TaskId, Title, IsRead)
VALUES (@UserId, @ProjectId, @TaskId, @Title, 0)",
                        new { UserId = uid, ProjectId = projectId, TaskId = taskId, Title = title ?? "ماموریت جدید" });
                }
            }
            catch { /* table may not exist */ }
        }

        public async Task<List<ProjectTaskNotificationItem>> ListNotificationsAsync(int userId, bool unreadOnly = false, int take = 50)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                var list = await conn.QueryAsync<ProjectTaskNotificationItem>(@"
SELECT TOP (@Take) n.Id, n.UserId, n.ProjectId, n.TaskId, n.Title, n.IsRead, n.CreatedAt,
       p.Title AS ProjectTitle
FROM koch.ProjectTaskNotifications n
LEFT JOIN koch.Projects p ON p.Id = n.ProjectId
WHERE n.UserId = @UserId AND (@UnreadOnly = 0 OR n.IsRead = 0)
ORDER BY n.CreatedAt DESC",
                    new { UserId = userId, UnreadOnly = unreadOnly ? 1 : 0, Take = take });
                return list?.ToList() ?? new List<ProjectTaskNotificationItem>();
            }
            catch { return new List<ProjectTaskNotificationItem>(); }
        }

        public async Task<int> CountUnreadNotificationsAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM koch.ProjectTaskNotifications WHERE UserId = @UserId AND IsRead = 0",
                    new { UserId = userId });
            }
            catch { return 0; }
        }

        public async Task MarkNotificationReadAsync(int id, int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                await conn.ExecuteAsync(
                    "UPDATE koch.ProjectTaskNotifications SET IsRead = 1 WHERE Id = @Id AND UserId = @UserId",
                    new { Id = id, UserId = userId });
            }
            catch { }
        }

        public async Task MarkAllNotificationsReadAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            try
            {
                await conn.ExecuteAsync(
                    "UPDATE koch.ProjectTaskNotifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0",
                    new { UserId = userId });
            }
            catch { }
        }
    }
}
