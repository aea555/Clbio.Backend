using Clbio.Domain.Entities.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Domain.Entities.V1
{
    public class WorkspaceMember : EntityBase
    {
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    }
}
