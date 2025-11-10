using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.WorkspaceMember
{
    public class UpdateWorkspaceMemberDto : RequestDtoBase
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public WorkspaceRole Role { get; set; }
    }
}
