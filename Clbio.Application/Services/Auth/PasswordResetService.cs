using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth
{
    public sealed class PasswordResetService(
        ITokenService tokenService,
        IAuthThrottlingService throttling,
        ICacheService cache,
        IRepository<User> userRepo,
        IRepository<PasswordResetToken> passwordResetRepo,
        IRepository<RefreshToken> refreshTokenRepo,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IConfiguration config,
        ILogger<PasswordResetService>? logger = null) : IPasswordResetService
    {
        private readonly ITokenService _tokenService = tokenService;
        private readonly IAuthThrottlingService _throttling = throttling;
        private readonly ICacheService _cache = cache;
        private readonly IRepository<User> _userRepo = userRepo;
        private readonly IRepository<PasswordResetToken> _passwordResetRepo = passwordResetRepo;
        private readonly IRepository<RefreshToken> _refreshTokenRepo = refreshTokenRepo;
        private readonly IUnitOfWork _uow = uow;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly IConfiguration _config = config;
        private readonly ILogger<PasswordResetService>? _logger = logger;

        // -------------------------------------------------------------
        // FORGOT PASSWORD 
        // -------------------------------------------------------------
        public async Task<Result> ForgotPasswordAsync(
            ForgotPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                if (await _throttling.IsIpThrottled(ipAddress, ct))
                {
                    return Result.Ok();
                }

                var user = await _userRepo.Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

                if (user is null)
                {
                    await _throttling.LogResetAttempt(null, false, ipAddress, ct);
                    return Result.Ok();
                }

                if (user.AuthProvider == AuthProvider.Google && string.IsNullOrWhiteSpace(user.PasswordHash))
                    return Result.Ok();

                if (!user.EmailVerified)
                {
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                    return Result.Ok();
                }

                // Throttling
                if (await _throttling.IsResetPasswordThrottled(user.Email, ct))
                {
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                    return Result.Ok();
                }

                var cooldownKey = $"otp:reset_pass_cooldown:{user.Id}";
                var inCooldown = await _cache.GetAsync<string>(cooldownKey);

                if (!string.IsNullOrEmpty(inCooldown))
                    return Result.Ok();

                var otp = Random.Shared.Next(100000, 999999).ToString();
                var key = $"otp:reset_pass:{user.Id}";

                await _cache.SetAsync(key, otp, TimeSpan.FromMinutes(10));

                var subject = "Reset Your Password";
                var body = $@"
                    <h3>Hello {user.DisplayName},</h3>
                    <p>You requested a password reset. Use the code below to reset your password:</p>
                    <h1 style='color: #4A90E2; letter-spacing: 5px;'>{otp}</h1>
                    <p>This code expires in 10 minutes.</p>
                    <p>If you didn't request this, ignore this email.</p>";

                try
                {
                    await _emailSender.SendEmailAsync(user.Email, subject, body, ct);
                    await _throttling.LogResetAttempt(user.Email, true, ipAddress, ct);
                }
                catch (Exception emailEx)
                {
                    _logger?.LogError(emailEx, "Failed to send password reset email to {Email}", user.Email);
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                }

                await _cache.SetAsync(cooldownKey, "1", TimeSpan.FromMinutes(2));
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ForgotPasswordAsync failed for email {Email}", dto.Email);
                return Result.Ok();
            }
        }

        // -------------------------------------------------------------
        // RESET PASSWORD (deprecated)
        // -------------------------------------------------------------
        public async Task<Result> ResetPasswordAsync(
            ResetPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                var user = await _userRepo.Query()
                    .FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

                if (user is null)
                    return Result.Fail("Invalid request.");

                var key = $"otp:reset_pass:{user.Id}";
                var cachedOtp = await _cache.GetAsync<string>(key);

                if (cachedOtp == null)
                    return Result.Fail("The reset code has expired. Please request a new one.");

                if (cachedOtp != dto.Code)
                    return Result.Fail("Invalid reset code.");

                // 4) Throttling
                if (await _throttling.IsResetPasswordThrottled(user.Email, ct) ||
                    await _throttling.IsIpThrottled(ipAddress, ct))
                {
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                    return Result.Fail("Too many reset attempts. Please try again later.");
                }

                // 5) Hash new password
                var hashPwd = PasswordManager.HashPassword(dto.NewPassword);
                if (!hashPwd.Success || string.IsNullOrWhiteSpace(hashPwd.Value))
                {
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                    return Result.Fail(hashPwd.Error ?? "Password hashing failed.");
                }

                user.PasswordHash = hashPwd.Value!;

                await _refreshTokenRepo.Query()
                .Where(rt => rt.UserId == user.Id &&
                            rt.RevokedUtc == null &&
                            rt.ExpiresUtc > DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.RevokedUtc, DateTime.UtcNow), ct);

                await _uow.SaveChangesAsync(ct);

                await _throttling.LogResetAttempt(user.Email, true, ipAddress, ct);
                await _cache.RemoveAsync(key);
                await _cache.RemoveAsync($"otp:reset_pass_cooldown:{user.Id}");

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Password reset failed.");
                return Result.Fail("Unable to reset password.");
            }
        }

        public async Task<Result<string>> ValidateOtpOnlyAsync(string email, string code, CancellationToken ct = default)
        {
            var normalizedEmail = email.ToLowerInvariant().Trim();
            var user = await _userRepo.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
            if (user == null) return Result<string>.Fail("Invalid request.");

            var key = $"otp:reset_pass:{user.Id}";
            var storedOtp = await _cache.GetAsync<string>(key);

            if (storedOtp is null)
                return Result<string>.Fail("Verification code expired.");

            if (!string.Equals(storedOtp, code))
                return Result<string>.Fail("Invalid verification code.");

            var resetToken = Guid.NewGuid().ToString("N");
            var resetKey = $"password:reset:token:{resetToken}";

            await _cache.SetAsync(resetKey, user.Id.ToString(), TimeSpan.FromMinutes(5));

            await _cache.RemoveAsync(key);

            return Result<string>.Ok(resetToken);
        }

        public async Task<Result> ResetPasswordWithTokenAsync(string token, string newPassword, string? ipAddress, CancellationToken ct = default)
        {
            var resetKey = $"password:reset:token:{token}";
            var userIdStr = await _cache.GetAsync<string>(resetKey);

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Result.Fail("Reset session expired or invalid.");

            var user = await _userRepo.Query().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null) return Result.Fail("User not found.");

            var hashPwd = PasswordManager.HashPassword(newPassword);
            if (!hashPwd.Success)
            {
                await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                return Result.Fail(hashPwd.Error ?? "Password hashing failed.");
            }

            user.PasswordHash = hashPwd.Value!;

            await _refreshTokenRepo.Query()
                .Where(rt => rt.UserId == user.Id && rt.RevokedUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedUtc, DateTime.UtcNow), ct);

            await _uow.SaveChangesAsync(ct);

            await _cache.RemoveAsync(resetKey);
            await _cache.RemoveAsync($"otp:reset_pass_cooldown:{user.Id}");

            await _throttling.LogResetAttempt(user.Email, true, ipAddress, ct);

            return Result.Ok();
        }
    }
}
