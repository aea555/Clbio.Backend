using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result> RegisterAsync(
            RegisterRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default
        );

        Task<Result<TokenResponseDto>> LoginAsync(
            LoginRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default
        );

        Task<Result<TokenResponseDto>> LoginWithGoogleAsync(
            GoogleLoginRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default
        );

        Task<Result<TokenResponseDto>> RefreshAsync(
            string refreshToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default
        );

        Task<Result> VerifyEmailAsync(
            string rawToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default
        );

        Task<Result> LogoutAsync(
            Guid userId,
            string refreshToken,
            CancellationToken ct = default
        );

        Task<Result> LogoutAllAsync(
            Guid userId,
            CancellationToken ct = default
        );

        Task<Result> ForgotPasswordAsync(
            ForgotPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default
        );

        Task<Result> ResetPasswordAsync(
            ResetPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default
        );
    }
}
