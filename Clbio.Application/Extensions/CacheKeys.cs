using Clbio.Domain.Enums;

namespace Clbio.Application.Extensions
{
    public static class CacheKeys
    {
        // ─────────────────────────────────────────────────────
        // USERS
        // ─────────────────────────────────────────────────────
        public static string User(Guid userId)
            => $"user:{userId}";

        // VERSION KEYS ----------------------------------------------------
        public static string WorkspaceVersionKey(Guid workspaceId)
            => $"version:workspace:{workspaceId}";

        public static string WorkspaceRoleVersionKey(WorkspaceRole role)
            => $"version:wsrole:{role}";

        public static string MembershipVersionKey(Guid userId, Guid workspaceId)
            => $"version:membership:{workspaceId}:{userId}";

        // ─────────────────────────────────────────────────────
        // WORKSPACE & MEMBERSHIP
        // ─────────────────────────────────────────────────────
        public static string Workspace(Guid workspaceId, long version) =>
            $"workspace:v{version}:{workspaceId}";

        // Membership of user inside a workspace 
        public static string Membership(Guid userId, Guid workspaceId, long version) =>
            $"membership:v{version}:{userId}:{workspaceId}";
        
        public static string WorkspaceMembers(Guid workspaceId, long version) =>
            $"members:ws:v{version}:{workspaceId}";

        // Workspace list per user 
        public static string UserWorkspaces(Guid userId)
            => $"userws:{userId}";

        // ─────────────────────────────────────────────────────
        // ROLE & PERMISSION
        // ─────────────────────────────────────────────────────
        public static string RolePermissions(WorkspaceRole role, long version) 
            => $"roleperms:ws:v{version}:{role}";

        public static string GlobalRolePermissions(GlobalRole role)
            => $"roleperms:global:{role}";

        // ─────────────────────────────────────────────────────
        // BOARDS
        // ─────────────────────────────────────────────────────
        public static string Board(Guid boardId, long version)
            => $"board:v{version}:{boardId}";

        public static string BoardsByWorkspace(Guid workspaceId, long version) 
            => $"boards:ws:v{version}:{workspaceId}";
        
        public static string BoardMetaWorkspaceId(Guid boardId) 
            => $"json:meta:board_ws:{boardId}";

        // ─────────────────────────────────────────────────────
        // COLUMNS
        // ─────────────────────────────────────────────────────
        public static string ColumnsByBoard(Guid boardId, long version)
            => $"columns:board:v{version}:{boardId}";

        // ─────────────────────────────────────────────────────
        // TASKS
        // ─────────────────────────────────────────────────────
        public static string Task(Guid taskId, long version)
            => $"task:v{version}:{taskId}";

        public static string BoardTasks(Guid boardId, long version)
            => $"tasks:board:v{version}:{boardId}";
        public static string TaskMetaWorkspaceId(Guid taskId) 
            => $"meta:task_ws:{taskId}";

        // ─────────────────────────────────────────────────────
        // NOTIFICATIONS 
        // ─────────────────────────────────────────────────────
        public static string NotificationCount(Guid userId)
            => $"notifcount:{userId}";

        // ─────────────────────────────────────────────────────
        // COMMENTS
        // ─────────────────────────────────────────────────────
        public static string TaskComments(Guid taskId) 
            => $"comments:task:{taskId}";
    }
}
