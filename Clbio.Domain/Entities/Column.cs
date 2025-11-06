using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class Column : EntityBase
    {
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        public int Position { get; set; }
        public Guid BoardId { get; set; }
        public Board Board { get; set; } = null!;
        public ICollection<TaskItem> Tasks { get; set; } = [];
    }
}
