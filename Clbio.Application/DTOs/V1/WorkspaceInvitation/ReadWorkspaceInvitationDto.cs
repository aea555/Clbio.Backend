using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Application.DTOs.V1.WorkspaceInvitation
{
    public class ReadWorkspaceInvitationDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = default!;
        public string InviterName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public WorkspaceRole Role { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}