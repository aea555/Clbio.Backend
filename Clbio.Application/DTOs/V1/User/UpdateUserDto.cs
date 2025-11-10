using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.User
{
    public class UpdateUserDto : RequestDtoBase
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string DisplayName { get; set; } = null!;

        [Url]
        public string? AvatarUrl { get; set; }
    }
}
