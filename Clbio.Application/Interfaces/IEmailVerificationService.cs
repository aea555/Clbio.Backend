using Clbio.Application.DTOs.V1.Auth;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces
{
    public interface IEmailVerificationService
    {
        Task<Result> SendVerificationEmailAsync(Guid userId, CancellationToken ct = default);
        Task<Result<TokenResponseDto>> VerifyEmailAsync(string rawToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default);
    }
}
