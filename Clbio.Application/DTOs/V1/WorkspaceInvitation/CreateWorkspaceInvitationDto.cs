using System.ComponentModel.DataAnnotations;
using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Application.DTOs.V1.WorkspaceInvitation
{
    public class CreateWorkspaceInvitationDto : RequestDtoBase
    {
        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(WorkspaceRole), ErrorMessage = "Invalid Role value.")]
        public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    }
}
