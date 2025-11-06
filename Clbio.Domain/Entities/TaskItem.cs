using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class TaskItem : EntityBase
    {
        [Required, MaxLength(100)]
        public string Title { get; set; } = null!;
        [MaxLength(500)]
        public string? Description { get; set; }
        public int Position { get; set; }

        public Guid ColumnId { get; set; }
        public Column Column { get; set; } = null!;

        public Guid? AssigneeId { get; set; }
        public User? Assignee { get; set; }

        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Attachment> Attachments { get; set; } = [];
    }
}
