using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Auth.External
{
    public class ExternalUserInfoDto : ResponseDtoBase
    {
        public string Provider { get; init; } = null!;
        public string ProviderUserId { get; init; } = null!;
        public string Email { get; init; } = null!;
        public bool EmailVerified { get; init; }
        public string? Name { get; init; }
        public string? PictureUrl { get; init; }
    }
}
