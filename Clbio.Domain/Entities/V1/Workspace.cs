using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class Workspace : EntityBase
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public ICollection<WorkspaceMember> Members { get; set; } = [];
        public ICollection<Board> Boards { get; set; } = [];
    }
}
