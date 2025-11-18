namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IAuthThrottlingService
    {
        Task<bool> IsLoginThrottled(string email, CancellationToken ct = default);
        Task<bool> IsResetPasswordThrottled(string email, CancellationToken ct = default);
        Task<bool> IsIpThrottled(string? ip, CancellationToken ct = default);

        Task LogResetAttempt(string? email, bool success, string? ip, CancellationToken ct = default);
        Task LogLoginAttempt(string email, bool success, string? ip, CancellationToken ct = default);
    }

}
