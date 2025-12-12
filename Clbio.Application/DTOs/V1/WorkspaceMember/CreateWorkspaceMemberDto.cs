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
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [EnumDataType(typeof(WorkspaceRole), ErrorMessage = "Invalid Role value.")]
        public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    }
}
