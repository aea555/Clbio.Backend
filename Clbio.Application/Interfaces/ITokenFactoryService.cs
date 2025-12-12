using Clbio.Application.DTOs.V1.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces
{
    public interface ITokenFactoryService
    {
        Task<Result<TokenResponseDto>> IssueTokensAsync(
            User user,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default);

        Task<Result<TokenResponseDto>> RotateRefreshTokenAsync(
            string refreshToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default);

        Task<Result> RevokeAllTokensAsync(
            Guid userId,
            CancellationToken ct = default);
    }
}
