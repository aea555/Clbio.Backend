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
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IAuthThrottlingService _throttling;
        private readonly IEmailVerificationService _emailVerification;
        private readonly IPasswordResetService _passwordReset;
        private readonly ITokenFactoryService _tokenFactory;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AuthService>? _logger;

        public AuthService(
            IUnitOfWork uow,
            IAuthThrottlingService throttling,
            IEmailVerificationService emailVerification,
            IPasswordResetService passwordReset,
            ITokenFactoryService tokenFactory,
            ITokenService tokenService,
            ILogger<AuthService>? logger = null)
        {
            _uow = uow;
            _throttling = throttling;
            _emailVerification = emailVerification;
            _passwordReset = passwordReset;
            _tokenFactory = tokenFactory;
            _tokenService = tokenService;
            _logger = logger;

            _userRepo = _uow.Repository<User>();
        }

        // --------------------------------------------------------------
        // REGISTER
        // --------------------------------------------------------------
        public async Task<Result<TokenResponseDto>> RegisterAsync(
            RegisterRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                var existing = await _userRepo.FindAsync(u => u.Email == dto.Email, ct);
                if (existing.Any())
                    return Result<TokenResponseDto>.Fail("Email is already in use.");

                var hash = PasswordManager.HashPassword(dto.Password);
                if (!hash.Success)
                    return Result<TokenResponseDto>.Fail(hash.Error ?? "Password hashing failed.");

                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = hash.Value!,
                    DisplayName = dto.DisplayName,
                    AvatarUrl = null
                };

                await _userRepo.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);

                // Send verification email
                await _emailVerification.SendVerificationEmailAsync(user.Id, ct);

                // Issue JWT + refresh
                return await _tokenFactory.IssueTokensAsync(user, userAgent, ipAddress, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RegisterAsync failed");
                return Result<TokenResponseDto>.Fail(
                $"Registration failed: {ex.GetType().Name}: {ex.Message}",
                "DEBUG"
                     );
                // return Result<TokenResponseDto>.Fail("Registration failed.");
            }
        }

        // --------------------------------------------------------------
        // LOGIN
        // --------------------------------------------------------------
        public async Task<Result<TokenResponseDto>> LoginAsync(
            LoginRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                var users = await _userRepo.FindAsync(u => u.Email == dto.Email, ct);
                var user = users.FirstOrDefault();

                if (user is null)
                {
                    await _throttling.LogLoginAttempt(dto.Email, false, ipAddress, ct);
                    return Result<TokenResponseDto>.Fail("Invalid credentials.");
                }

                // Check if throttled
                if (await _throttling.IsLoginThrottled(dto.Email, ct))
                    return Result<TokenResponseDto>.Fail("Too many failed login attempts. Try again later.");

                var verify = PasswordManager.VerifyPassword(dto.Password, user.PasswordHash);
                var ok = verify.Success && verify.Value;

                await _throttling.LogLoginAttempt(dto.Email, ok, ipAddress, ct);

                if (!ok)
                    return Result<TokenResponseDto>.Fail("Invalid credentials.");

                if (!user.EmailVerified)
                    return Result<TokenResponseDto>.Fail("Email is not verified.");

                // Issue tokens through factory
                return await _tokenFactory.IssueTokensAsync(user, userAgent, ipAddress, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LoginAsync failed");
                return Result<TokenResponseDto>.Fail(
                $"Login failed: {ex.GetType().Name}: {ex.Message}",
                "DEBUG"
                    );

                // return Result<TokenResponseDto>.Fail("Login failed.");
            }
        }

        // --------------------------------------------------------------
        // REFRESH TOKEN
        // --------------------------------------------------------------
        public async Task<Result<TokenResponseDto>> RefreshAsync(
            string refreshToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            return await _tokenFactory.RotateRefreshTokenAsync(refreshToken, userAgent, ipAddress, ct);
        }

        // --------------------------------------------------------------
        // EMAIL VERIFICATION
        // --------------------------------------------------------------
        public async Task<Result> VerifyEmailAsync(string rawToken, CancellationToken ct = default)
        {
            return await _emailVerification.VerifyEmailAsync(rawToken, ct);
        }

        // --------------------------------------------------------------
        // FORGOT PASSWORD 
        // --------------------------------------------------------------
        public async Task<Result> ForgotPasswordAsync(
            ForgotPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default)
        {
            return await _passwordReset.ForgotPasswordAsync(dto, ipAddress, ct);
        }

        // --------------------------------------------------------------
        // RESET PASSWORD 
        // --------------------------------------------------------------
        public async Task<Result> ResetPasswordAsync(
            ResetPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default)
        {
            return await _passwordReset.ResetPasswordAsync(dto, ipAddress, ct);
        }

        // --------------------------------------------------------------
        // LOGOUT
        // --------------------------------------------------------------
        public async Task<Result> LogoutAsync(
            Guid userId,
            string refreshToken,
            CancellationToken ct = default)
        {
            try
            {
                var hash = _tokenService.HashRefreshToken(refreshToken);
                if (!hash.Success || string.IsNullOrWhiteSpace(hash.Value))
                    return Result.Fail("Invalid refresh token.");

                var now = DateTime.UtcNow;
                var refreshRepo = _uow.Repository<RefreshToken>();

                var tokens = await refreshRepo.FindAsync(
                    rt => rt.UserId == userId &&
                          rt.TokenHash == hash.Value &&
                          rt.RevokedUtc == null &&
                          rt.ExpiresUtc > now,
                    ct);

                var stored = tokens.FirstOrDefault();
                if (stored is null)
                    return Result.Fail("Refresh token not found.");

                stored.RevokedUtc = now;

                await _uow.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Logout failed");
                return Result.Fail("Logout failed.");
            }
        }

        // --------------------------------------------------------------
        // LOGOUT ALL
        // --------------------------------------------------------------
        public async Task<Result> LogoutAllAsync(Guid userId, CancellationToken ct = default)
        {
            return await _tokenFactory.RevokeAllTokensAsync(userId, ct);
        }
    }
}
