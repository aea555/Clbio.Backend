using Clbio.Domain.Enums;

namespace Clbio.Domain.Extensions
{
    public static class PermissionMetadata
    {
        public static readonly IReadOnlyDictionary<Permission, PermissionScope> Scopes =
            new Dictionary<Permission, PermissionScope>
            {
            // ---- GLOBAL SCOPE ----
            { Permission.ManageUsers, PermissionScope.Global },
            { Permission.ViewAuditLog, PermissionScope.Global },

            // ---- USER SCOPE ----
            { Permission.ViewNotification, PermissionScope.User },
            { Permission.MarkNotificationAsRead, PermissionScope.User },

            // ---- WORKSPACE SCOPE ----
            { Permission.ViewWorkspace, PermissionScope.Workspace },
            { Permission.ManageWorkspace, PermissionScope.Workspace },
            { Permission.ArchiveWorkspace, PermissionScope.Workspace },
            { Permission.DeleteWorkspace, PermissionScope.Workspace },

            { Permission.ViewBoard, PermissionScope.Workspace },
            { Permission.CreateBoard, PermissionScope.Workspace },
            { Permission.UpdateBoard, PermissionScope.Workspace },
            { Permission.ReorderBoard, PermissionScope.Workspace },
            { Permission.DeleteBoard, PermissionScope.Workspace },

            { Permission.ViewColumn, PermissionScope.Workspace },
            { Permission.CreateColumn, PermissionScope.Workspace },
            { Permission.UpdateColumn, PermissionScope.Workspace },
            { Permission.ReorderColumn, PermissionScope.Workspace },
            { Permission.DeleteColumn, PermissionScope.Workspace },

            { Permission.ViewTask, PermissionScope.Workspace },
            { Permission.CreateTask, PermissionScope.Workspace },
            { Permission.AssignTask, PermissionScope.Workspace },
            { Permission.MoveTask, PermissionScope.Workspace },
            { Permission.UpdateTask, PermissionScope.Workspace },
            { Permission.CommentOnTask, PermissionScope.Workspace },
            { Permission.UpdateTaskStatus, PermissionScope.Workspace },
            { Permission.MarkTaskAsComplete, PermissionScope.Workspace },
            { Permission.ReopenTask, PermissionScope.Workspace },
            { Permission.DisapproveTask, PermissionScope.Workspace },
            { Permission.DeleteTask, PermissionScope.Workspace },

            { Permission.ViewMember, PermissionScope.Workspace },
            { Permission.AddMember, PermissionScope.Workspace },
            { Permission.RemoveMember, PermissionScope.Workspace },

            { Permission.ViewRole, PermissionScope.Workspace },
            { Permission.UpdateRole, PermissionScope.Workspace },

            { Permission.ViewComment, PermissionScope.Workspace },
            { Permission.CreateComment, PermissionScope.Workspace },

            { Permission.ViewAttachment, PermissionScope.Workspace },
            { Permission.CreateAttachment, PermissionScope.Workspace },
            };
    }

}
