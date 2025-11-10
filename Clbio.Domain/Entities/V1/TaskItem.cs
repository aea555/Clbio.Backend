using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class TaskItem : EntityBase
    {
        public string Title { get; set; } = null!;
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
