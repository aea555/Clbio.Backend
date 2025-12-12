using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Auth
{
    public class TokenResponseDto : ResponseDtoBase
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public DateTime AccessExpiresUtc { get; set; } = default!;
        public DateTime RefreshExpiresUtc { get; set; } = default!;
    }
}
