using Clbio.Domain.Enums;
using System.Collections.ObjectModel;

namespace Clbio.Domain.Extensions
{
    public static class RolePermissionMap
    {
        // Global Admins have everything
        public static readonly ReadOnlyCollection<Permission> GlobalAdminPermissions =
            Array.AsReadOnly(Enum.GetValues<Permission>());

        // Workspace Owners have nearly everything except true system-wide privileges
        public static readonly ReadOnlyCollection<Permission> WorkspaceOwnerPermissions =
            Array.AsReadOnly(
            [
            // Workspace
            Permission.ViewWorkspace,
            Permission.ManageWorkspace,
            Permission.ArchiveWorkspace,
            Permission.DeleteWorkspace,

            // Board
            Permission.ViewBoard,
            Permission.CreateBoard,
            Permission.UpdateBoard,
            Permission.ReorderBoard,
            Permission.DeleteBoard,

            // Column
            Permission.ViewColumn,
            Permission.CreateColumn,
            Permission.UpdateColumn,
            Permission.ReorderColumn,
            Permission.DeleteColumn,

            // Task
            Permission.ViewTask,
            Permission.CreateTask,
            Permission.AssignTask,
            Permission.MoveTask,
            Permission.UpdateTask,
            Permission.CommentOnTask,
            Permission.UpdateTaskStatus,
            Permission.MarkTaskAsComplete,
            Permission.DisapproveTask,
            Permission.ReopenTask,
            Permission.DeleteTask,

            // Member management
            Permission.ViewMember,
            Permission.AddMember,
            Permission.RemoveMember,

            // Role
            Permission.ViewRole,
            Permission.UpdateRole,

            // Attachments & comments
            Permission.ViewAttachment,
            Permission.CreateAttachment,
            Permission.ViewComment,
            Permission.CreateComment,

            // Notifications
            Permission.ViewNotification,
            Permission.MarkNotificationAsRead
            ]);

        // Privileged members: can manage tasks and members but not delete/archival actions
        public static readonly ReadOnlyCollection<Permission> PrivilegedMemberPermissions =
            Array.AsReadOnly(
            [
            Permission.ViewWorkspace,
            Permission.ManageWorkspace,

            Permission.ViewBoard,
            Permission.CreateBoard,
            Permission.UpdateBoard,
            Permission.ReorderBoard,

            Permission.ViewColumn,
            Permission.CreateColumn,
            Permission.UpdateColumn,
            Permission.ReorderColumn,

            Permission.ViewTask,
            Permission.CreateTask,
            Permission.AssignTask,
            Permission.MoveTask,
            Permission.UpdateTask,
            Permission.CommentOnTask,
            Permission.UpdateTaskStatus,
            Permission.MarkTaskAsComplete,
            Permission.DisapproveTask,
            Permission.ReopenTask,
            Permission.DeleteTask,

            Permission.ViewMember,
            Permission.AddMember,
            Permission.RemoveMember,

            Permission.ViewAttachment,
            Permission.CreateAttachment,
            Permission.ViewComment,
            Permission.CreateComment,
            Permission.ViewNotification,
            Permission.MarkNotificationAsRead
            ]);

        // Regular members: limited to their own work
        public static readonly ReadOnlyCollection<Permission> MemberPermissions =
            Array.AsReadOnly(
            [
            Permission.ViewWorkspace,

            Permission.ViewBoard,
            Permission.ViewColumn,
            Permission.ViewTask,
            Permission.UpdateTaskStatus,
            Permission.CommentOnTask,
            Permission.MarkTaskAsComplete,

            Permission.ViewAttachment,
            Permission.CreateAttachment,
            Permission.ViewComment,
            Permission.CreateComment,
            Permission.ViewNotification,
            Permission.MarkNotificationAsRead
            ]);

        public static ReadOnlyCollection<Permission> GetWorkspacePermissions(WorkspaceRole role) =>
            role switch
            {
                WorkspaceRole.Owner => WorkspaceOwnerPermissions,
                WorkspaceRole.PrivilegedMember => PrivilegedMemberPermissions,
                WorkspaceRole.Member => MemberPermissions,
                _ => Array.AsReadOnly(Array.Empty<Permission>())
            };

        public static ReadOnlyCollection<Permission> GetGlobalPermissions(GlobalRole role) =>
            role switch
            {
                GlobalRole.Admin => GlobalAdminPermissions,
                _ => Array.AsReadOnly(Array.Empty<Permission>())
            };
    }
}
