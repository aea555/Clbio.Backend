using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class Comment : EntityBase
    {
        public string Body { get; set; } = null!;
        public Guid TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
        public Guid AuthorId { get; set; }
        public User Author { get; set; } = null!;
    }
}
