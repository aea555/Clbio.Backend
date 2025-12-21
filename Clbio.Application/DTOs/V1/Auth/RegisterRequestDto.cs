using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class RegisterRequestDto : RequestDtoBase
    {
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [StringLength(100, MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", 
            ErrorMessage = "Password must be at least 6 characters long and contain at least one uppercase letter, one lowercase letter, and one number.")]
        public string Password { get; set; } = default!;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string DisplayName { get; set; } = default!;

        [Url]
        [MaxLength(2048)]
        public string? AvatarUrl { get; set; }
    }
}
