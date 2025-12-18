using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.Interfaces;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth;

public sealed class EmailVerificationService(
    IRepository<User> userRepo,
    IRepository<EmailVerificationToken> verificationRepo,
    ICacheService cache,
    ITokenService tokenService,
    ITokenFactoryService tokenFactory,
    IEmailSender emailSender,
    IUnitOfWork uow,
    IConfiguration config,
    ILogger<EmailVerificationService>? logger = null) : IEmailVerificationService
{
    private readonly IRepository<User> _userRepo = userRepo;
    private readonly IRepository<EmailVerificationToken> _verificationRepo = verificationRepo;
    private readonly ICacheService _cache = cache;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ITokenFactoryService _tokenFactory = tokenFactory;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly ILogger<EmailVerificationService>? _logger = logger;


    // --------------------------------------------------------------------
    // Send verification email (OTP)
    // --------------------------------------------------------------------
    public async Task<Result> SendVerificationOtpEmailAsync(string email, string displayName, CancellationToken ct = default)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant().Trim();

            var user = await _userRepo.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

            if (user == null) return Result.Fail("User not found.");
            if (user.EmailVerified) return Result.Fail("Email is already verified.");

            var cooldownKey = $"otp:cooldown:{normalizedEmail}";
            var inCooldown = await _cache.GetAsync<string>(cooldownKey);

            if (!string.IsNullOrEmpty(inCooldown))
            {
                return Result.Fail("Please wait a few minutes before requesting a new code.");
            }
            
            // Generate OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            var key = $"otp:verify:{normalizedEmail}";
            await _cache.SetAsync(key, otp, TimeSpan.FromMinutes(3));

            // Email
            var subject = "Clbio - Email Verification Code";
            var body = 
                $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px;'>
                        <h2>Hello {displayName},</h2>
                        <p>Your verification code is:</p>
                        <h1 style='color: #4A90E2; letter-spacing: 5px;'>{otp}</h1>
                        <p>This code will expire in 3 minutes.</p>
                        <p>If you didn't request this, please ignore this email.</p>
                    </div>
                ";
            
            await _emailSender.SendEmailAsync(email, subject, body, ct);

            await _cache.SetAsync(cooldownKey, "1", TimeSpan.FromMinutes(3));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SendVerificationOtpEmailAsync failed for user {Email}", email);
            return Result.Fail("Unable to send verification email.");
        }
    }

    // --------------------------------------------------------------------
    // Send verification email (token) (deprecated)
    // --------------------------------------------------------------------
    public async Task<Result> SendVerificationEmailAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(userId, false, ct);
            if (user is null)
                return Result.Fail("User not found.");

            if (user.EmailVerified)
                return Result.Ok(); // Already verified

            // Generate token
            var tokenResult = _tokenService.CreateRefreshToken();
            if (!tokenResult.Success)
                return Result.Fail(tokenResult.Error ?? "Failed to generate verification token.");

            var (rawToken, expiresUtc, hash) = tokenResult.Value;

            // Override expiry if configured
            if (int.TryParse(_config["Auth:EmailVerification:TokenMinutes"], out var minutes))
                expiresUtc = DateTime.UtcNow.AddMinutes(minutes);

            var entity = new EmailVerificationToken
            {
                UserId = user.Id,
                TokenHash = hash,
                ExpiresUtc = expiresUtc
            };

            await _verificationRepo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // Build verification url
            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:8080";
            var verifyUrl = $"{baseUrl}/api/Auth/verify-email?token={rawToken}";

            // Email
            var subject = "Verify your email";
            var body = $@"
                <p>Hello {user.DisplayName},</p>
                <p>Please click the link below to verify your email:</p>
                <p><a href=""{verifyUrl}"">Verify Email</a></p>
                <p>This link expires in {minutes} minutes.</p>
            ";

            await _emailSender.SendEmailAsync(user.Email, subject, body, ct);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SendVerificationEmailAsync failed for user {UserId}", userId);
            return Result.Fail("Unable to send verification email.");
        }
    }

    // --------------------------------------------------------------------
    // Verify OTP and mark email as verified
    // --------------------------------------------------------------------
    public async Task<Result> VerifyEmailOtpAsync(string email, string code, string? userAgent,
        string? ipAddress, CancellationToken ct = default)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant().Trim();
            var user = await _userRepo.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct) 
            ?? throw new Exception("User not found.");

            if (user.EmailVerified)
                return Result.Fail("Email is already verified."); 

            var key = $"otp:verify:{normalizedEmail}";
            var storedOtp = await _cache.GetAsync<string>(key);

            if (storedOtp is null)
                return Result.Fail("Verification code expired or not found.");

            if (string.Equals(storedOtp, code) is false)
                return Result.Fail("Invalid verification code.");

            user.EmailVerified = true;

            await _uow.SaveChangesAsync(ct);
            await _cache.RemoveAsync(key);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "VerifyEmailOtpAsync failed for user {Email}", email);
            return Result.Fail("Email verification failed.");
        }
    }

    // --------------------------------------------------------------------
    // Verify email token and issue tokens to user
    // --------------------------------------------------------------------
    public async Task<Result<TokenResponseDto>> VerifyEmailAsync(string rawToken,
        string? userAgent,
        string? ipAddress, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return Result<TokenResponseDto>.Fail("Verification token is required.");

            // hash raw token for lookup
            var hash = _tokenService.HashRefreshToken(rawToken);
            if (!hash.Success || string.IsNullOrWhiteSpace(hash.Value))
                return Result<TokenResponseDto>.Fail("Invalid verification token.");

            var tokenHash = hash.Value!;
            var now = DateTime.UtcNow;

            // Look for valid token
            var stored = (await _verificationRepo.FindAsync(
                x => x.TokenHash == tokenHash &&
                     !x.Used &&
                     x.ExpiresUtc > now,
                false, ct)).FirstOrDefault();

            if (stored is null)
                return Result<TokenResponseDto>.Fail("Invalid or expired verification token.");

            var user = await _userRepo.GetByIdAsync(stored.UserId, true, ct);
            if (user is null)
                return Result<TokenResponseDto>.Fail("User not found.");

            // Mark verified
            user.EmailVerified = true;

            // Mark token used
            stored.Used = true;

            await _uow.SaveChangesAsync(ct);

            // issue tokens
            var tokens = await _tokenFactory.IssueTokensAsync(user, userAgent, ipAddress, ct);

            if (!tokens.Success || tokens.Value is null)
                return Result<TokenResponseDto>.Fail("Failed to issue tokens!");

            return Result<TokenResponseDto>.Ok(tokens.Value);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "VerifyEmailAsync failed.");
            return Result<TokenResponseDto>.Fail("Email verification failed.");
        }
    }

    // --------------------------------------------------------------------
    // Resend verification OTP email
    // --------------------------------------------------------------------
    public async Task<Result> ResendVerificationOtpEmailAsync(string email, CancellationToken ct = default)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant().Trim();
            var user = await _userRepo.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct)
            ?? throw new Exception("User not found.");

            if (user.EmailVerified)
                return Result.Ok(); // Already verified

            var cooldownKey = $"otp:cooldown:{normalizedEmail}";
            var inCooldown = await _cache.GetAsync<string>(cooldownKey);

            if (!string.IsNullOrEmpty(inCooldown))
            {
                return Result.Fail("Please wait a few minutes before requesting a new code.");
            }

            var sendResult = await SendVerificationOtpEmailAsync(user.Email, user.DisplayName, ct);

            if (sendResult.Success)
            {
                await _cache.SetAsync(cooldownKey, "1", TimeSpan.FromMinutes(3));
            }

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResendVerificationOtpEmailAsync failed for user {Email}", email);
            return Result.Fail("Unable to resend verification email.");
        }
    }
}
