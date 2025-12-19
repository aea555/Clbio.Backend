using Clbio.Domain.Entities.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Domain.Entities.V1
{
    public class WorkspaceInvitation : EntityBase
    {
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public Guid InviterId { get; set; }
        
        public string Email { get; set; } = default!;
        public WorkspaceRole Role { get; set; } 
        
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7); 
    }
}