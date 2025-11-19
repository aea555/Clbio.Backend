using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Shared.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth;

public sealed class EmailVerificationService(
    IRepository<User> userRepo,
    IRepository<EmailVerificationToken> verificationRepo,
    ITokenService tokenService,
    IEmailSender emailSender,
    IUnitOfWork uow,
    IConfiguration config,
    ILogger<EmailVerificationService>? logger = null) : IEmailVerificationService
{
    private readonly IRepository<User> _userRepo = userRepo;
    private readonly IRepository<EmailVerificationToken> _verificationRepo = verificationRepo;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly ILogger<EmailVerificationService>? _logger = logger;

    // --------------------------------------------------------------------
    // Send verification email
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
            var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:8080";
            var verifyUrl = $"{baseUrl}/verify-email?token={rawToken}";

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
    // Verify email token
    // --------------------------------------------------------------------
    public async Task<Result> VerifyEmailAsync(string rawToken, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return Result.Fail("Verification token is required.");

            // hash raw token for lookup
            var hash = _tokenService.HashRefreshToken(rawToken);
            if (!hash.Success || string.IsNullOrWhiteSpace(hash.Value))
                return Result.Fail("Invalid verification token.");

            var tokenHash = hash.Value!;
            var now = DateTime.UtcNow;

            // Look for valid token
            var stored = (await _verificationRepo.FindAsync(
                x => x.TokenHash == tokenHash &&
                     !x.Used &&
                     x.ExpiresUtc > now,
                false, ct)).FirstOrDefault();

            if (stored is null)
                return Result.Fail("Invalid or expired verification token.");

            var user = await _userRepo.GetByIdAsync(stored.UserId, true, ct);
            if (user is null)
                return Result.Fail("User not found.");

            // Mark verified
            user.EmailVerified = true;

            // Mark token used
            stored.Used = true;

            await _uow.SaveChangesAsync(ct);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "VerifyEmailAsync failed.");
            return Result.Fail("Email verification failed.");
        }
    }
}
