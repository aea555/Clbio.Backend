using Clbio.Application.DTOs.V1.Auth;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces
{
    public interface IPasswordResetService
    {
        Task<Result> ResetPasswordAsync(ResetPasswordRequestDto dto, string? ipAddress, CancellationToken ct = default);
        Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default);
        Task<Result<string>> ValidateOtpOnlyAsync(string email, string code, CancellationToken ct = default);
        Task<Result> ResetPasswordWithTokenAsync(string token, string newPassword, string? ipAddress, CancellationToken ct = default);
    }
}
