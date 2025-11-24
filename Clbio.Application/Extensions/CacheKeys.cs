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

        // ─────────────────────────────────────────────────────
        // WORKSPACE
        // ─────────────────────────────────────────────────────
        public static string Workspace(Guid workspaceId)
            => $"workspace:{workspaceId}";

        // Membership of user inside a workspace 
        public static string Membership(Guid userId, Guid workspaceId)
            => $"membership:{userId}:{workspaceId}";

        // Workspace list per user 
        public static string UserWorkspaces(Guid userId)
            => $"userws:{userId}";

        // ─────────────────────────────────────────────────────
        // ROLE & PERMISSION
        // ─────────────────────────────────────────────────────
        public static string RolePermissions(WorkspaceRole role)
            => $"roleperms:workspace:{role}";

        public static string GlobalRolePermissions(GlobalRole role)
            => $"roleperms:global:{role}";

        // ─────────────────────────────────────────────────────
        // BOARDS (only metadata)
        // ─────────────────────────────────────────────────────
        public static string Board(Guid boardId)
            => $"board:{boardId}";

        public static string BoardsByWorkspace(Guid workspaceId)
            => $"boards:ws:{workspaceId}";

        // ─────────────────────────────────────────────────────
        // NOTIFICATIONS 
        // ─────────────────────────────────────────────────────
        public static string NotificationCount(Guid userId)
            => $"notifcount:{userId}";
    }
}
