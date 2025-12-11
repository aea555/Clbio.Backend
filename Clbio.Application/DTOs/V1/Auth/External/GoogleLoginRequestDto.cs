using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Auth.External
{
    public class GoogleLoginRequestDto : RequestDtoBase
    {
        [MaxLength(4000)]
        public string IdToken { get; set; } = null!;
    }
}
