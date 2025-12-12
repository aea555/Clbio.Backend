using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
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
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
