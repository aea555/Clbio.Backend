using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class User : EntityBase
    {
        [Required, MaxLength(100)]
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = [];
        public ICollection<TaskItem> AssignedTasks { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
    }
}
