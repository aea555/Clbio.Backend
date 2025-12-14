using Clbio.Application.DTOs.V1.Auth;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces
{
    public interface IEmailVerificationService
    {
        Task<Result> SendVerificationOtpEmailAsync(Guid userId, string displayName, string email, CancellationToken ct = default);
        Task<Result> SendVerificationEmailAsync(Guid userId, CancellationToken ct = default);
        Task<Result> VerifyEmailOtpAsync(Guid userId, string otp, string? userAgent,
        string? ipAddress, CancellationToken ct = default);
        Task<Result<TokenResponseDto>> VerifyEmailAsync(string rawToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default);
        Task<Result> ResendVerificationOtpEmailAsync(Guid userId, CancellationToken ct = default);
    }
}
