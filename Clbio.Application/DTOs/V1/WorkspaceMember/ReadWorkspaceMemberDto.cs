using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Application.DTOs.V1.WorkspaceMember
{
    public class ReadWorkspaceMemberDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = null!;
        public string? UserAvatarUrl { get; set; }
        public WorkspaceRole Role { get; set; }
    }
}
