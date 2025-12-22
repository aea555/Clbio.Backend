using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Application.DTOs.V1.User;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<User> _userRepo;
        private readonly IAuthThrottlingService _throttling;
        private readonly IEmailVerificationService _emailVerification;
        private readonly IPasswordResetService _passwordReset;
        private readonly ITokenFactoryService _tokenFactory;
        private readonly ITokenService _tokenService;
        private readonly IGoogleAuthService _googleAuth;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AuthService>? _logger;

        public AuthService(
            IUnitOfWork uow,
            IMapper mapper,
            IAuthThrottlingService throttling,
            IEmailVerificationService emailVerification,
            IPasswordResetService passwordReset,
            ITokenFactoryService tokenFactory,
            ITokenService tokenService,
            IGoogleAuthService googleAuth,
            ILogger<AuthService>? logger = null)
        {
            _uow = uow;
            _mapper = mapper;
            _throttling = throttling;
            _emailVerification = emailVerification;
            _passwordReset = passwordReset;
            _tokenFactory = tokenFactory;
            _tokenService = tokenService;
            _googleAuth = googleAuth;
            _logger = logger;
            _userRepo = _uow.Repository<User>();
        }

        // --------------------------------------------------------------
        // REGISTER
        // --------------------------------------------------------------
        public async Task<Result<ReadUserDto>> RegisterAsync(
            RegisterRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                var existingUsers = await _userRepo.FindAsync(u => u.Email == dto.Email, false, ct);
                var existing = existingUsers.FirstOrDefault();

                // User already exists
                if (existing is not null)
                {
                    // e belongs to a Google-only user
                    if (existing.AuthProvider == AuthProvider.Google && string.IsNullOrWhiteSpace(existing.PasswordHash))
                    {
                        return Result<ReadUserDto>.Fail(
                            "This email is associated with a Google account. Use 'Continue with Google' to sign in.");
                    }

                    // if user exists and email is verified, fail
                    if (existing.EmailVerified)
                        return Result<ReadUserDto>.Fail("Email is already in use.");

                    return Result<ReadUserDto>.Fail("Please verify your account first.");
                }

                // Hash password
                var hash = PasswordManager.HashPassword(dto.Password);
                if (!hash.Success)
                    return Result<ReadUserDto>.Fail(hash.Error ?? "Password hashing failed.");

                // Create local user
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = hash.Value!,
                    DisplayName = dto.DisplayName,
                    AvatarUrl = null,
                    AuthProvider = AuthProvider.Local,
                    ExternalId = null,
                    EmailVerified = false
                };

                await _userRepo.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);

                var userDto = _mapper.Map<User, ReadUserDto>(user);

                // Send verification email
                await _emailVerification.SendVerificationOtpEmailAsync(user.Email, user.DisplayName, ct);
                return Result<ReadUserDto>.Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RegisterAsync failed");
                return Result<ReadUserDto>.Fail("Registration failed.");
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
                var users = await _userRepo.FindAsync(u => u.Email == dto.Email, false, ct);
                var user = users.FirstOrDefault();

                if (user is null)
                {
                    await _throttling.LogLoginAttempt(dto.Email, false, ipAddress, ct);
                    return Result<TokenResponseDto>.Fail("Invalid credentials.");
                }

                // Check if throttled
                if (await _throttling.IsLoginThrottled(dto.Email, ct))
                    return Result<TokenResponseDto>.Fail("Too many failed login attempts. Try again later.");

                // check if user is google-only
                if (user.AuthProvider == AuthProvider.Google && string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    await _throttling.LogLoginAttempt(dto.Email, false, ipAddress, ct);
                    return Result<TokenResponseDto>.Fail(
                        "This account uses Google sign-in. Use 'Continue with Google' to log in.");
                }

                var verify = PasswordManager.VerifyPassword(dto.Password, user.PasswordHash);
                var ok = verify.Success && verify.Value;

                await _throttling.LogLoginAttempt(dto.Email, ok, ipAddress, ct);

                if (!ok)
                    return Result<TokenResponseDto>.Fail("Invalid credentials.");

                if (!user.EmailVerified)
                    return Result<TokenResponseDto>.Fail("Email is not verified.");

                // Issue tokens
                return await _tokenFactory.IssueTokensAsync(user, userAgent, ipAddress, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LoginAsync failed");
                return Result<TokenResponseDto>.Fail("Login failed.");
            }
        }

        // --------------------------------------------------------------
        // LOGIN WITH GOOGLE
        // --------------------------------------------------------------
        public async Task<Result<TokenResponseDto>> LoginWithGoogleAsync(
            GoogleLoginRequestDto dto,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                // 1) validate the Google ID token
                var tokenResult = await _googleAuth.ValidateIdTokenAsync(dto.IdToken, ct);
                if (!tokenResult.Success || tokenResult.Value is null)
                {
                    await _throttling.LogLoginAttempt(null, false, ipAddress, ct);
                    _logger?.LogWarning("Google login failed token validation: {Error}", tokenResult.Error);
                    return Result<TokenResponseDto>.Fail(tokenResult.Error ?? "Invalid Google login.");
                }

                var ext = tokenResult.Value;

                if (string.IsNullOrWhiteSpace(ext.Email))
                {
                    return Result<TokenResponseDto>.Fail("Google account has no email.");
                }

                if (!ext.EmailVerified)
                {
                    await _throttling.LogLoginAttempt(ext.Email, false, ipAddress, ct);
                    return Result<TokenResponseDto>.Fail("Google email is not verified.");
                }

                if (await _throttling.IsLoginThrottled(ext.Email, ct))
                {
                    await _throttling.LogLoginAttempt(ext.Email, false, ipAddress, ct);
                    return Result<TokenResponseDto>.Fail("Too many failed login attempts. Try again later.");
                }

                // 2) find existing user
                var users = await _userRepo.FindAsync(u => u.Email == ext.Email, true, ct);
                var user = users.FirstOrDefault();

                // 3) If user doesn't exist, create one
                if (user is null)
                {
                    user = new User
                    {
                        Email = ext.Email,
                        DisplayName = ext.Name ?? ext.Email,
                        AvatarUrl = ext.PictureUrl,
                        EmailVerified = true,
                        AuthProvider = AuthProvider.Google,
                        ExternalId = ext.ProviderUserId,
                    };

                    await _userRepo.AddAsync(user, ct);
                    await _uow.SaveChangesAsync(ct);
                    await _throttling.LogLoginAttempt(ext.Email, true, ipAddress, ct);

                    return await _tokenFactory.IssueTokensAsync(user, userAgent, ipAddress, ct);
                }
                if (user.AuthProvider == AuthProvider.Google)
                {
                    // if external ID exists, it must match
                    if (!string.IsNullOrEmpty(user.ExternalId))
                    {
                        if (user.ExternalId != ext.ProviderUserId)
                        {
                            return Result<TokenResponseDto>.Fail("External ID mismatch.");
                        }
                    }
                    else
                    {
                        // if external ID is missing, set it
                        user.ExternalId = ext.ProviderUserId;
                    }

                    // Update avatar if missing
                    if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrWhiteSpace(ext.PictureUrl))
                        user.AvatarUrl = ext.PictureUrl;

                    user.EmailVerified = true;

                    await _uow.SaveChangesAsync(ct);
                    await _throttling.LogLoginAttempt(ext.Email, true, ipAddress, ct);

                    return await _tokenFactory.IssueTokensAsync(user, userAgent, ipAddress, ct);
                }
                if (user.AuthProvider == AuthProvider.Local && user.PasswordHash != null)
                {
                    // auto link google
                    user.AuthProvider = AuthProvider.Google;
                    user.ExternalId = ext.ProviderUserId;
                    user.EmailVerified = ext.EmailVerified;

                    // Keep avatar if user set one manually, otherwise use Google’s
                    if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(ext.PictureUrl))
                        user.AvatarUrl = ext.PictureUrl;

                    await _uow.SaveChangesAsync(ct);
                    await _throttling.LogLoginAttempt(ext.Email, true, ipAddress, ct);

                    return await _tokenFactory.IssueTokensAsync(user, userAgent, ipAddress, ct);
                }

                // Inconsistent account state → cannot safely determine provider
                _logger?.LogError(
                    "Inconsistent auth state for user {Email}: PasswordHash=null AND ExternalId=null",
                    user.Email
                );

                await _throttling.LogLoginAttempt(ext.Email, false, ipAddress, ct);

                return Result<TokenResponseDto>.Fail(
                    "Account configuration error."
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LoginWithGoogleAsync failed");
                return Result<TokenResponseDto>.Fail($"Google login failed. Details: {ex.Message}");
                //return Result<TokenResponseDto>.Fail("Google login failed.");
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
        // EMAIL VERIFICATION (OTP)
        // --------------------------------------------------------------
        public async Task<Result> VerifyEmailOtpAsync(
            string email,
            string code,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            return await _emailVerification.VerifyEmailOtpAsync(email, code, userAgent, ipAddress, ct);
        }

        // --------------------------------------------------------------
        // EMAIL VERIFICATION (token, deprecated)
        // --------------------------------------------------------------
        public async Task<Result> VerifyEmailAsync(string rawToken, string? userAgent,
            string? ipAddress, CancellationToken ct = default)
        {
            return await _emailVerification.VerifyEmailAsync(rawToken, userAgent, ipAddress, ct);
        }

        // --------------------------------------------------------------
        // FORGOT PASSWORD (deprecated)
        // --------------------------------------------------------------
        public async Task<Result> ForgotPasswordAsync(
            ForgotPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default)
        {
            return await _passwordReset.ForgotPasswordAsync(dto, ipAddress, ct);
        }

        // --------------------------------------------------------------
        // RESET PASSWORD (deprecated)
        // --------------------------------------------------------------
        public async Task<Result> ResetPasswordAsync(
            ResetPasswordRequestDto dto,
            string? ipAddress,
            CancellationToken ct = default)
        {
            return await _passwordReset.ResetPasswordAsync(dto, ipAddress, ct);
        }

        // --------------------------------------------------------------
        // VALIDATE OTP ONLY 
        // --------------------------------------------------------------
        public async Task<Result<string>> ValidateOtpOnlyAsync(
            string email,
            string code,
            CancellationToken ct = default)
        {
            return await _passwordReset.ValidateOtpOnlyAsync(email, code, ct);
        }

        // --------------------------------------------------------------
        // RESET PASSWORD WITH TOKEN 
        // --------------------------------------------------------------
        public async Task<Result> ResetPasswordWithTokenAsync(
            string token,
            string newPassword,
            string? ipAddress,
            CancellationToken ct = default)
        {
            return await _passwordReset.ResetPasswordWithTokenAsync(token, newPassword, ipAddress, ct);
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
                    true, ct);

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
