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
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
            ErrorMessage = "New password must be at least 6 characters long and contain at least one uppercase letter, one lowercase letter, and one number.")]
        public string NewPassword { get; set; } = null!;
    }
}
