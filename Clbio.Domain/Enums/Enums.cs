using System.ComponentModel;

namespace Clbio.Domain.Enums
{
    #region v1.0 - Global and Workspace Roles

    /// <summary>
    /// v1.0 - Defines system-level roles that apply across all workspaces.
    /// </summary>
    public enum GlobalRole
    {
        [Description("No global privileges.")]
        None = 0,

        [Description("Full system administrator privileges.")]
        Admin = 1
    }

    /// <summary>
    /// v1.0 - Defines roles within a specific workspace context.
    /// </summary>
    public enum WorkspaceRole
    {
        [Description("Regular member with limited permissions.")]
        Member = 0,

        [Description("Can manage members, assign tasks, and perform privileged actions.")]
        PrivilegedMember = 1,

        [Description("Full control over workspace, including archiving and deleting.")]
        Owner = 2
    }

    /// <summary>
    /// v1.0 - Indicates whether a workspace is active or archived.
    /// </summary>
    public enum WorkspaceStatus
    {
        [Description("Workspace is active and accessible.")]
        Active = 0,

        [Description("Workspace has been archived and is read-only.")]
        Archived = 1
    }

    #endregion

    #region v1.0 - PermissionScope
    /// <summary>
    /// v1.0 - Defines the scopes of permissions.
    /// </summary>
    public enum PermissionScope
    {
        Global,     // Admin-only
        Workspace,  // Everything that depends on a workspace
        User        // User-specific things like notifications
    }

    #region v1.0 - Permissions

    /// <summary>
    /// v1.0 - Defines all action-level permissions available in the system.
    /// </summary>
    public enum Permission
    {
        // Workspace
        [Description("View workspace details.")]
        ViewWorkspace,

        [Description("Manage workspace settings.")]
        ManageWorkspace,

        [Description("Archive the workspace.")]
        ArchiveWorkspace,

        [Description("Permanently delete the workspace.")]
        DeleteWorkspace,

        // Board
        [Description("View boards within a workspace.")]
        ViewBoard,

        [Description("Create new boards.")]
        CreateBoard,

        [Description("Update board details.")]
        UpdateBoard,

        [Description("Reorder boards within a workspace.")]
        ReorderBoard,

        [Description("Delete boards.")]
        DeleteBoard,

        // Column
        [Description("View columns within a board.")]
        ViewColumn,

        [Description("Create new columns.")]
        CreateColumn,

        [Description("Update column details.")]
        UpdateColumn,

        [Description("Reorder columns within a board.")]
        ReorderColumn,

        [Description("Delete columns.")]
        DeleteColumn,

        // Attachment
        [Description("View file attachments.")]
        ViewAttachment,

        [Description("Upload file attachments.")]
        CreateAttachment,

        // Comment
        [Description("View comments on tasks.")]
        ViewComment,

        [Description("Add new comments to tasks.")]
        CreateComment,

        // Task
        [Description("View task details.")]
        ViewTask,

        [Description("Create new tasks.")]
        CreateTask,

        [Description("Assign tasks to workspace members.")]
        AssignTask,

        [Description("Move tasks between columns or boards.")]
        MoveTask,

        [Description("Update task metadata or details.")]
        UpdateTask,

        [Description("Post comments on tasks.")]
        CommentOnTask,

        [Description("Change the status of a task.")]
        UpdateTaskStatus,

        [Description("Mark task as completed.")]
        MarkTaskAsComplete,

        [Description("Mark task as reopened.")]
        ReopenTask,

        [Description("Disapprove a task.")]
        DisapproveTask,

        [Description("Delete tasks.")]
        DeleteTask,

        // Workspace Member
        [Description("View workspace members.")]
        ViewMember,

        [Description("Add new members to the workspace.")]
        AddMember,

        [Description("Remove members from the workspace.")]
        RemoveMember,

        // Role
        [Description("View workspace roles and permissions.")]
        ViewRole,

        [Description("Update or modify member roles within a workspace.")]
        UpdateRole,

        // Notification
        [Description("View user notifications.")]
        ViewNotification,

        [Description("Mark notifications as read.")]
        MarkNotificationAsRead,

        // Admin-only permissions
        [Description("Manage user accounts and global settings.")]
        ManageUsers,

        [Description("View system audit logs.")]
        ViewAuditLog
    }

    #endregion

    #region v1.0 - Task Lifecycle

    /// <summary>
    /// v1.0 - Represents the lifecycle states of an active task.
    /// </summary>
    public enum TaskProgressStatus
    {
        [Description("Task has been assigned but not yet acknowledged.")]
        Assigned = 0,

        [Description("Assignee has acknowledged the task.")]
        Received = 1,

        [Description("Task is currently being worked on.")]
        InProgress = 2,

        [Description("Task is ready for review or approval.")]
        ReadyForReview = 3
    }

    /// <summary>
    /// v1.0 - Represents the completion status of a task.
    /// </summary>
    public enum TaskCompletionStatus
    {
        [Description("Task is still active.")]
        Active = 0,

        [Description("Task has been completed successfully.")]
        Completed = 1,

        [Description("Task was reopened after completion.")]
        Reopened = 2
    }

    #endregion

    #region AuthProvider
    /// <summary>
    /// v1.0 - Distinction between local auth provider and Google auth provider
    /// </summary>
    public enum AuthProvider
    {
        Local = 0,
        Google = 1,
    }

    #endregion

    #endregion
}
