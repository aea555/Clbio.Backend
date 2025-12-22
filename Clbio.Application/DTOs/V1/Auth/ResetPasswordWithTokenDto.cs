using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class ResetPasswordWithTokenDto : RequestDtoBase
    {
        [Required]
        [MaxLength(32)]
        public string Token { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
            ErrorMessage = "New password must be at least 6 characters long and contain at least one uppercase letter, one lowercase letter, and one number.")]
        public string NewPassword { get; set; } = null!;
    }
}
