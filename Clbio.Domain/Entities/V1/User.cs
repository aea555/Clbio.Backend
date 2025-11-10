using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class User : EntityBase
    {
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = [];
        public ICollection<TaskItem> AssignedTasks { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
    }
}
