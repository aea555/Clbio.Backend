using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<Result<ExternalUserInfoDto>> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
    }
}
