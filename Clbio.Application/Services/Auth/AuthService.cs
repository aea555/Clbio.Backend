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
    public sealed class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<RefreshToken> _refreshTokenRepo;
        private readonly IRepository<LoginAttempt> _loginAttemptRepo;
        private readonly IRepository<EmailVerificationToken> _emailVerificationRepo;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthService>? _logger;

        public AuthService(
            IUnitOfWork uow,
            ITokenService tokenService,
            IConfiguration config,
            IEmailSender emailSender,
            ILogger<AuthService>? logger = null)
        {
            _uow = uow;
            _tokenService = tokenService;
            _config = config;
            _emailSender = emailSender;
            _logger = logger;

            _userRepo = _uow.Repository<User>();
            _refreshTokenRepo = _uow.Repository<RefreshToken>();
            _loginAttemptRepo = _uow.Repository<LoginAttempt>();
            _emailVerificationRepo = _uow.Repository<EmailVerificationToken>();
        }

        public async Task<Result<TokenResponseDto>> RegisterAsync(
            RegisterRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                // 1) Check if email is already in use
                var existing = await _userRepo.FindAsync(u => u.Email == dto.Email, ct);
                if (existing.Any())
                    return Result<TokenResponseDto>.Fail("Email is already in use.");

                // 2) Hash password
                var hashResult = PasswordManager.HashPassword(dto.Password);
                if (!hashResult.Success || string.IsNullOrWhiteSpace(hashResult.Value))
                    return Result<TokenResponseDto>.Fail(hashResult.Error ?? "Password hashing failed.");

                // 3) Create user entity
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = hashResult.Value,
                    DisplayName = dto.DisplayName,
                    AvatarUrl = null
                };

                await _userRepo.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);

                await SendEmailVerificationAsync(user, ct);

                // 4) Issue tokens
                return await IssueTokensAsync(user, userAgent, ipAddress, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RegisterAsync failed for email {Email}", dto.Email);
                return Result<TokenResponseDto>.Fail("Registration failed.");
            }
        }

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
                    // Log failed attempt (no user found)
                    await _loginAttemptRepo.AddAsync(new LoginAttempt
                    {
                        Email = dto.Email,
                        Succeeded = false,
                        IpAddress = ipAddress
                    }, ct);
                    await _uow.SaveChangesAsync(ct);

                    return Result<TokenResponseDto>.Fail("Invalid credentials.");
                }

                if (!user.EmailVerified)
                    return Result<TokenResponseDto>.Fail("Email is not verified.");

                if (await IsLoginThrottled(dto.Email, ct))
                    return Result<TokenResponseDto>.Fail("Too many failed login attempts. Please try again later.");

                var verifyResult = PasswordManager.VerifyPassword(dto.Password, user.PasswordHash);
                var passwordOk = verifyResult.Success && verifyResult.Value;

                // Log attempt regardless of outcome
                await _loginAttemptRepo.AddAsync(new LoginAttempt
                {
                    Email = dto.Email,
                    Succeeded = passwordOk,
                    IpAddress = ipAddress
                }, ct);
                await _uow.SaveChangesAsync(ct);

                if (!passwordOk)
                    return Result<TokenResponseDto>.Fail("Invalid credentials.");

                // Here we know credentials are valid
                return await IssueTokensAsync(user, userAgent, ipAddress, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LoginAsync failed for email {Email}", dto.Email);
                return Result<TokenResponseDto>.Fail("Login failed.");
            }
        }

        public async Task<Result<TokenResponseDto>> RefreshAsync(
            string refreshToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result<TokenResponseDto>.Fail("Refresh token is required.");

            try
            {
                var hashResult = _tokenService.HashRefreshToken(refreshToken);
                if (!hashResult.Success || string.IsNullOrWhiteSpace(hashResult.Value))
                    return Result<TokenResponseDto>.Fail(hashResult.Error ?? "Invalid refresh token.");

                var now = DateTime.UtcNow;
                var tokens = await _refreshTokenRepo.FindAsync(
                    rt => rt.TokenHash == hashResult.Value &&
                          rt.RevokedUtc == null &&
                          rt.ExpiresUtc > now,
                    ct);

                var storedToken = tokens.FirstOrDefault();
                if (storedToken is null)
                    return Result<TokenResponseDto>.Fail("Invalid or expired refresh token.");

                var user = await _userRepo.GetByIdAsync(storedToken.UserId, ct);
                if (user is null)
                    return Result<TokenResponseDto>.Fail("User not found for this token.");

                // Rotate refresh token
                storedToken.RevokedUtc = now;

                var newRefreshResult = _tokenService.CreateRefreshToken();
                if (!newRefreshResult.Success)
                    return Result<TokenResponseDto>.Fail(newRefreshResult.Error ?? "Failed to create new refresh token.");

                var (newToken, newExpiresUtc, newTokenHash) = newRefreshResult.Value;

                storedToken.ReplacedByTokenHash = newTokenHash;

                var newRefreshEntity = new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = newTokenHash,
                    ExpiresUtc = newExpiresUtc,
                    CreatedUtc = now,
                    UserAgent = userAgent,
                    IpAddress = ipAddress
                };

                await _refreshTokenRepo.AddAsync(newRefreshEntity, ct);

                // Create new access token
                var accessResult = _tokenService.CreateAccessToken(user);
                if (!accessResult.Success || string.IsNullOrWhiteSpace(accessResult.Value))
                    return Result<TokenResponseDto>.Fail(accessResult.Error ?? "Failed to create access token.");

                var accessExpiresUtc = GetAccessTokenExpiryUtc(now, out var expiryError);
                if (accessExpiresUtc == null)
                    return Result<TokenResponseDto>.Fail(expiryError ?? "Invalid JWT configuration.");

                await _uow.SaveChangesAsync(ct);

                var dto = new TokenResponseDto
                {
                    AccessToken = accessResult.Value,
                    RefreshToken = newToken,
                    AccessExpiresUtc = accessExpiresUtc.Value,
                    RefreshExpiresUtc = newExpiresUtc
                };

                return Result<TokenResponseDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RefreshAsync failed.");
                return Result<TokenResponseDto>.Fail("Token refresh failed.");
            }
        }

        public async Task<Result> VerifyEmailAsync(string rawToken, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawToken))
                    return Result.Fail("Token is required.");

                var hashResult = _tokenService.HashRefreshToken(rawToken);
                if (!hashResult.Success)
                    return Result.Fail("Invalid verification token.");

                var stored = (await _emailVerificationRepo.FindAsync(
                    x => x.TokenHash == hashResult.Value &&
                         !x.Used &&
                         x.ExpiresUtc > DateTime.UtcNow,
                    ct
                )).FirstOrDefault();

                if (stored is null)
                    return Result.Fail("Invalid or expired verification token.");

                var user = await _userRepo.GetByIdAsync(stored.UserId, ct);
                if (user is null)
                    return Result.Fail("User not found for this token.");

                user.EmailVerified = true;
                user.EmailVerifiedAtUtc = DateTime.UtcNow;

                stored.Used = true;

                await _uow.SaveChangesAsync(ct);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "VerifyEmailAsync failed.");
                return Result.Fail("Unable to verify email.");
            }
        }

        public async Task<Result> LogoutAsync(
            Guid userId,
            string refreshToken,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result.Fail("Refresh token is required.");

            try
            {
                var hashResult = _tokenService.HashRefreshToken(refreshToken);
                if (!hashResult.Success || string.IsNullOrWhiteSpace(hashResult.Value))
                    return Result.Fail(hashResult.Error ?? "Invalid refresh token.");

                var tokens = await _refreshTokenRepo.FindAsync(
                    rt => rt.UserId == userId &&
                          rt.TokenHash == hashResult.Value &&
                          rt.RevokedUtc == null,
                    ct);

                var storedToken = tokens.FirstOrDefault();
                if (storedToken is null)
                    return Result.Fail("Refresh token not found.");

                storedToken.RevokedUtc = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LogoutAsync failed for user {UserId}", userId);
                return Result.Fail("Logout failed.");
            }
        }

        public async Task<Result> LogoutAllAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                var tokens = await _refreshTokenRepo.FindAsync(
                    rt => rt.UserId == userId &&
                          rt.RevokedUtc == null &&
                          rt.ExpiresUtc > now,
                    ct);

                foreach (var t in tokens)
                {
                    t.RevokedUtc = now;
                }

                await _uow.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LogoutAllAsync failed for user {UserId}", userId);
                return Result.Fail("Logout all sessions failed.");
            }
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        private async Task<Result<TokenResponseDto>> IssueTokensAsync(
            User user,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // 1) Access token
            var accessResult = _tokenService.CreateAccessToken(user);
            if (!accessResult.Success || string.IsNullOrWhiteSpace(accessResult.Value))
                return Result<TokenResponseDto>.Fail(accessResult.Error ?? "Failed to create access token.");

            var accessExpiresUtc = GetAccessTokenExpiryUtc(now, out var expiryError);
            if (accessExpiresUtc == null)
                return Result<TokenResponseDto>.Fail(expiryError ?? "Invalid JWT configuration.");

            // 2) Refresh token
            var refreshResult = _tokenService.CreateRefreshToken();
            if (!refreshResult.Success)
                return Result<TokenResponseDto>.Fail(refreshResult.Error ?? "Failed to create refresh token.");

            var (refreshToken, refreshExpiresUtc, refreshTokenHash) = refreshResult.Value;

            var refreshEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresUtc = refreshExpiresUtc,
                CreatedUtc = now,
                UserAgent = userAgent,
                IpAddress = ipAddress
            };

            await _refreshTokenRepo.AddAsync(refreshEntity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new TokenResponseDto
            {
                AccessToken = accessResult.Value,
                RefreshToken = refreshToken,
                AccessExpiresUtc = accessExpiresUtc.Value,
                RefreshExpiresUtc = refreshExpiresUtc
            };

            return Result<TokenResponseDto>.Ok(dto);
        }

        private DateTime? GetAccessTokenExpiryUtc(DateTime now, out string? error)
        {
            error = null;

            var minutesStr = _config["Jwt:AccessTokenMinutes"];
            if (!int.TryParse(minutesStr, out var minutes))
            {
                error = "Jwt:AccessTokenMinutes is not a valid integer.";
                return null;
            }

            return now.AddMinutes(minutes);
        }

        private async Task<bool> IsLoginThrottled(string email, CancellationToken ct)
        {
            var max = int.Parse(_config["Auth:Login:MaxFailedAttempts"]!);
            var minutes = int.Parse(_config["Auth:Login:WindowMinutes"]!);

            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

            var attempts = await _loginAttemptRepo.FindAsync(
                x => x.Email == email && x.CreatedAt >= cutoff && !x.Succeeded,
                ct
            );

            return attempts.Count() >= max;
        }
        private async Task SendEmailVerificationAsync(User user, CancellationToken ct)
        {
            // generate token using refresh token generator
            var tokenResult = _tokenService.CreateRefreshToken();
            if (!tokenResult.Success)
                throw new Exception("Unable to generate email verification token.");

            var (rawToken, expiresUtc, hash) = tokenResult.Value;

            var entity = new EmailVerificationToken
            {
                UserId = user.Id,
                TokenHash = hash,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(15)
            };

            await _emailVerificationRepo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var verifyUrl = $"{_config["App:BaseUrl"]}/verify-email?token={rawToken}";
            var subject = "Verify your email";
            var body = $@"
                <p>Hello {user.DisplayName},</p>
                <p>Click the link below to verify your email:</p>
                <p><a href=""{verifyUrl}"">Verify Email</a></p>";

            await _emailSender.SendEmailAsync(user.Email, subject, body, ct);
        }

    }
}
