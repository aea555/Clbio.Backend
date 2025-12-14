
using System.ComponentModel.DataAnnotations;
using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class VerifyEmailOtpRequestDto : RequestDtoBase
    {
        [Required]
        public Guid UserId { get; set; }
        [Required]
        [MaxLength(6)]
        public string Otp { get; set; } = null!;
    }
}
