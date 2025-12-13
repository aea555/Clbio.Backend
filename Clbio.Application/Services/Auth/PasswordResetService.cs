using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Shared.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth
{
    public sealed class PasswordResetService(
        ITokenService tokenService,
        IAuthThrottlingService throttling,
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
                var users = await _userRepo.FindAsync(u => u.Email == dto.Email, false, ct);
                var user = users.FirstOrDefault();

                if (user is null)
                {
                    await _throttling.LogResetAttempt(null, false, ipAddress, ct);
                    return Result.Ok();
                }

                if (!user.EmailVerified)
                {
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                    return Result.Ok();
                }

                // Throttling
                if (await _throttling.IsResetPasswordThrottled(user.Email, ct) ||
                    await _throttling.IsIpThrottled(ipAddress, ct))
                {
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                    return Result.Ok();
                }

                // Generate random token 
                var tokenResult = _tokenService.CreateRefreshToken();
                if (!tokenResult.Success)
                {
                    _logger?.LogError("Failed to generate password reset token for {Email}. Error: {Error}",
                        user.Email, tokenResult.Error);
                    await _throttling.LogResetAttempt(user.Email, false, ipAddress, ct);
                    return Result.Ok();
                }

                var (rawToken, defaultExpiresUtc, tokenHash) = tokenResult.Value;

                // Override expiry from config 
                var expiresUtc = defaultExpiresUtc;
                if (int.TryParse(_config["Auth:PasswordReset:TokenMinutes"], out var minutes))
                {
                    expiresUtc = DateTime.UtcNow.AddMinutes(minutes);
                }

                var entity = new PasswordResetToken
                {
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    ExpiresUtc = expiresUtc
                };

                await _passwordResetRepo.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:8080";
                var resetUrl = $"{baseUrl}/api/Auth/reset-password?token={rawToken}";

                var subject = "Reset your password";
                var body = $@"
                    <p>Hello {user.DisplayName},</p>
                    <p>You requested a password reset. Click the link below to set a new password:</p>
                    <p><a href=""{resetUrl}"">Reset Password</a></p>
                    <p>If you didn't request this, you can safely ignore this email.</p>";

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

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ForgotPasswordAsync failed for email {Email}", dto.Email);
                return Result.Ok();
            }
        }

        // -------------------------------------------------------------
        // RESET PASSWORD
        // -------------------------------------------------------------
        public async Task<Result> ResetPasswordAsync(
            ResetPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                // 1) Validate and hash token
                if (string.IsNullOrWhiteSpace(dto.Token))
                {
                    await _throttling.LogResetAttempt(null, false, ipAddress, ct);
                    return Result.Fail("Reset token is required.");
                }

                var hashRes = _tokenService.HashRefreshToken(dto.Token);
                if (!hashRes.Success || string.IsNullOrWhiteSpace(hashRes.Value))
                {
                    await _throttling.LogResetAttempt(null, false, ipAddress, ct);
                    return Result.Fail("Invalid reset token.");
                }

                var tokenHash = hashRes.Value!;
                var now = DateTime.UtcNow;

                // 2) Find valid token
                var token = (await _passwordResetRepo.FindAsync(
                    x => x.TokenHash == tokenHash &&
                         !x.Used &&
                         x.ExpiresUtc > now,
                    true, ct)).FirstOrDefault();

                if (token is null)
                {
                    await _throttling.LogResetAttempt(null, false, ipAddress, ct);
                    return Result.Fail("Invalid or expired reset token.");
                }

                // 3) Load user
                var user = await _userRepo.GetByIdAsync(token.UserId, true, ct);
                if (user is null)
                {
                    await _throttling.LogResetAttempt(null, false, ipAddress, ct);
                    return Result.Fail("User not found.");
                }

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

                user.PasswordHash = hashPwd.Value;

                // 6) Mark token as used
                token.Used = true;

                // 7) Revoke all active refresh tokens
                var refreshTokens = await _refreshTokenRepo.FindAsync(
                    rt => rt.UserId == user.Id &&
                          rt.RevokedUtc == null &&
                          rt.ExpiresUtc > now,
                    true, ct);

                foreach (var rt in refreshTokens)
                    rt.RevokedUtc = now;

                await _uow.SaveChangesAsync(ct);

                await _throttling.LogResetAttempt(user.Email, true, ipAddress, ct);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Password reset failed.");
                return Result.Fail("Unable to reset password.");
            }
        }
    }
}
