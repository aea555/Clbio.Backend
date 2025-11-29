using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Domain.Entities.V1.Auth;
using Microsoft.Extensions.Configuration;

namespace Clbio.Application.Services.Auth
{
    public sealed class AuthThrottlingService(
        IRepository<LoginAttempt> loginRepo,
        IRepository<PasswordResetAttempt> resetRepo,
        IUnitOfWork uow,
        IConfiguration config) : IAuthThrottlingService
    {
        private readonly IRepository<LoginAttempt> _loginRepo = loginRepo;
        private readonly IRepository<PasswordResetAttempt> _resetRepo = resetRepo;
        private readonly IUnitOfWork _uow = uow;
        private readonly IConfiguration _config = config;

        // -------------------------
        // LOGIN THROTTLING
        // -------------------------
        public async Task<bool> IsLoginThrottled(string email, CancellationToken ct = default)
        {
            var max = int.Parse(_config["Auth:Login:MaxFailedAttempts"]!);
            var minutes = int.Parse(_config["Auth:Login:WindowMinutes"]!);
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

            var attempts = await _loginRepo.FindAsync(
                x => x.Email == email && !x.Succeeded && x.CreatedAt >= cutoff,
                false, ct);

            return attempts.Count() >= max;
        }

        // -------------------------
        // RESET PASSWORD THROTTLING
        // -------------------------
        public async Task<bool> IsResetPasswordThrottled(string email, CancellationToken ct = default)
        {
            var windowMinutes = int.Parse(_config["Auth:PasswordReset:WindowMinutes"]!);
            var maxAttempts = int.Parse(_config["Auth:PasswordReset:MaxAttempts"]!);

            var since = DateTime.UtcNow.AddMinutes(-windowMinutes);

            var attempts = await _resetRepo.FindAsync(
                a => a.Email == email && a.AttemptedAtUtc >= since,
                false, ct);

            return attempts.Count() >= maxAttempts;
        }

        // -------------------------
        // IP THROTTLING
        // -------------------------
        public async Task<bool> IsIpThrottled(string? ip, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            var windowMinutes = int.Parse(_config["Auth:PasswordReset:WindowMinutes"]!);
            var maxAttempts = int.Parse(_config["Auth:PasswordReset:MaxIpAttempts"]!);

            var since = DateTime.UtcNow.AddMinutes(-windowMinutes);

            var attempts = await _resetRepo.FindAsync(
                a => a.IpAddress == ip && a.AttemptedAtUtc >= since,
                false, ct);

            return attempts.Count() >= maxAttempts;
        }

        // -------------------------
        // ATTEMPT LOGGING
        // -------------------------
        public async Task LogResetAttempt(string? email, bool success, string? ip, CancellationToken ct = default)
        {
            await _resetRepo.AddAsync(new PasswordResetAttempt
            {
                Email = email,
                Succeeded = success,
                IpAddress = ip
            }, ct);

            await _uow.SaveChangesAsync(ct);
        }

        public async Task LogLoginAttempt(string? email, bool success, string? ip, CancellationToken ct = default)
        {
            await _loginRepo.AddAsync(new LoginAttempt
            {
                Email = email,
                Succeeded = success,
                IpAddress = ip
            }, ct);

            await _uow.SaveChangesAsync(ct);
        }
    }

}
