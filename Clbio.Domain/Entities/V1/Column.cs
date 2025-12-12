using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class Column : EntityBase
    {
        public string Name { get; set; } = null!;
        public int Position { get; set; }
        public Guid BoardId { get; set; }
        public Board Board { get; set; } = null!;
        public ICollection<TaskItem> Tasks { get; set; } = [];
    }
}
