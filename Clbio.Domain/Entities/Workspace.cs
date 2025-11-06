using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class Workspace : EntityBase
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Description { get; set; }

        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        public ICollection<WorkspaceMember> Members { get; set; } = [];
        public ICollection<Board> Boards { get; set; } = [];
    }
}
