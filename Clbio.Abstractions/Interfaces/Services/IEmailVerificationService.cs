using Clbio.Shared.Results;

namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IEmailVerificationService
    {
        Task<Result> SendVerificationEmailAsync(Guid userId, CancellationToken ct = default);
        Task<Result> VerifyEmailAsync(string rawToken, CancellationToken ct = default);
    }
}
