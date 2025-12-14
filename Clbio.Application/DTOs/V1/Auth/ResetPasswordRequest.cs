using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class ResetPasswordRequestDto : RequestDtoBase
    {
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = null!;
    }
}
