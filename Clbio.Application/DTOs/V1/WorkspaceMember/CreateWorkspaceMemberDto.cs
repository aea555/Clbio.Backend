using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.WorkspaceMember
{
    public class CreateWorkspaceMemberDto : RequestDtoBase
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    }
}
