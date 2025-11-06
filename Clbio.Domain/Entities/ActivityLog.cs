using Clbio.Domain.Entities.Base;

namespace Clbio.Domain.Entities
{
    public class ActivityLog : EntityBase
    {
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public Guid ActorId { get; set; }
        public User Actor { get; set; } = null!;

        public string ActionType { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string Metadata { get; set; } = null!;
    }
}
