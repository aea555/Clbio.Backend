using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.WorkspaceInvitation
{
    public class ReadWorkspaceInvitationPagedReturnDto
    {
        public IEnumerable<ReadWorkspaceInvitationDto> Items { get; set; } = null!;

        public PagedMetaDto Meta { get; set; } = null!;
    }
}