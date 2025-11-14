using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class RegisterRequestDto : RequestDtoBase
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = default!;

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string DisplayName { get; set; } = default!;

        [Url]
        public string? AvatarUrl { get; set; }
    }
}
