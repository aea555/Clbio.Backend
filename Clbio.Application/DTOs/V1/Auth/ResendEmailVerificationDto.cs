using System.ComponentModel.DataAnnotations;
using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class ResendEmailVerificationDto : RequestDtoBase
    {
        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string Email { get; set; } = null!;
    }
}