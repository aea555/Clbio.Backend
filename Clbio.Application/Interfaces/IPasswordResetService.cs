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
    }
}
