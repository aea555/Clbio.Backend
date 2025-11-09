using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Application.DTOs.V1.WorkspaceMember
{
    public class ReadWorkspaceMemberDto : ResponseDtoBase
    {
        public Guid WorkspaceId { get; set; }
        public Guid UserId { get; set; }
        public WorkspaceRole Role { get; set; } = default!;
        public string? UserDisplayName { get; set; }
        public string? UserEmail { get; set; }
    }
}
