using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Auth.External
{
    public class GoogleLoginRequestDto : RequestDtoBase
    {
        public string IdToken { get; set; } = null!;
    }
}
