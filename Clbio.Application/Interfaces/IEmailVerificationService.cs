using Clbio.Application.DTOs.V1.Auth;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces
{
    public interface IEmailVerificationService
    {
        Task<Result> SendVerificationOtpEmailAsync(string email, string displayName, CancellationToken ct = default);
        Task<Result> SendVerificationEmailAsync(Guid userId, CancellationToken ct = default);
        Task<Result> VerifyEmailOtpAsync(string email, string code, string? userAgent,
        string? ipAddress, CancellationToken ct = default);
        Task<Result<TokenResponseDto>> VerifyEmailAsync(string rawToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default);
        Task<Result> ResendVerificationOtpEmailAsync(string email, CancellationToken ct = default);
    }
}
