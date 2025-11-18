using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class ForgotPasswordRequestDto : RequestDtoBase
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
