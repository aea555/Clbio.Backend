using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class Board : EntityBase
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;
        public int Order { get; set; }
        public ICollection<Column> Columns { get; set; } = [];
    }
}
