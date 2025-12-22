using System.ComponentModel.DataAnnotations;
using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class ValidateOtpOnlyDto : RequestDtoBase
    {
        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string Email { get; set;} = default!;
        
        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = default!;
    }
}