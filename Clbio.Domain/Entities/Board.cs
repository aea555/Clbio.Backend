using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class Board : EntityBase
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Description { get; set; }
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;
        public ICollection<Column> Columns { get; set; } = [];
    }
}
