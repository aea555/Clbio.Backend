using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Auth
{
    // DEPRECATED
    public class VerifyEmailRequestDto : RequestDtoBase
    {
        [Required]
        public string Token { get; set; } = null!;
    }
}
