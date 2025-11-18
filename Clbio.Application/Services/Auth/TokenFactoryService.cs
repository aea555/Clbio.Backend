using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.Interfaces;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Shared.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth;

public sealed class TokenFactoryService(
    ITokenService tokenService,
    IRepository<RefreshToken> refreshRepo,
    IRepository<User> userRepo,
    IUnitOfWork uow,
    IConfiguration config,
    ILogger<TokenFactoryService>? logger = null) : ITokenFactoryService
{
    private readonly ITokenService _tokenService = tokenService;
    private readonly IRepository<RefreshToken> _refreshRepo = refreshRepo;
    private readonly IRepository<User> _userRepo = userRepo;
    private readonly IUnitOfWork _uow = uow;
    private readonly IConfiguration _config = config;
    private readonly ILogger<TokenFactoryService>? _logger = logger;

    // ------------------------------------------------------------------
    // ISSUE NEW ACCESS + REFRESH TOKENS
    // ------------------------------------------------------------------
    public async Task<Result<TokenResponseDto>> IssueTokensAsync(
        User user,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            // 1) Create access token
            var access = _tokenService.CreateAccessToken(user);
            if (!access.Success)
                return Result<TokenResponseDto>.Fail(access.Error!);

            var accessToken = access.Value!;
            var accessExpires = GetAccessTokenExpiryUtc(now, out var expErr);

            if (accessExpires is null)
                return Result<TokenResponseDto>.Fail(expErr!);

            // 2) Create refresh token
            var refresh = _tokenService.CreateRefreshToken();
            if (!refresh.Success)
                return Result<TokenResponseDto>.Fail(refresh.Error!);

            var (rawRefresh, refreshExpires, refreshHash) = refresh.Value;

            // 3) Save refresh token
            var entity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresUtc = refreshExpires,
                CreatedUtc = now,
                UserAgent = userAgent,
                IpAddress = ipAddress
            };

            await _refreshRepo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // 4) Build response
            return Result<TokenResponseDto>.Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = rawRefresh,
                AccessExpiresUtc = accessExpires.Value,
                RefreshExpiresUtc = refreshExpires
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Token issuing failed for user {UserId}", user.Id);
            return Result<TokenResponseDto>.Fail("Token issuing failed.");
        }
    }

    // ------------------------------------------------------------------
    // ROTATE REFRESH TOKEN
    // ------------------------------------------------------------------
    public async Task<Result<TokenResponseDto>> RotateRefreshTokenAsync(
        string rawToken,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct = default)
    {
        try
        {
            // 1) Hash received token
            var hashed = _tokenService.HashRefreshToken(rawToken);
            if (!hashed.Success)
                return Result<TokenResponseDto>.Fail("Invalid refresh token.");

            var tokenHash = hashed.Value!;

            // 2) Find stored record
            var now = DateTime.UtcNow;
            var rt = (await _refreshRepo.FindAsync(
                r => r.TokenHash == tokenHash &&
                     r.RevokedUtc == null &&
                     r.ExpiresUtc > now,
                ct)).FirstOrDefault();

            if (rt is null)
                return Result<TokenResponseDto>.Fail("Refresh token invalid or expired.");

            // 3) Load user
            var user = await _userRepo.GetByIdAsync(rt.UserId, ct);
            if (user is null)
                return Result<TokenResponseDto>.Fail("User not found.");

            if (user is null)
                return Result<TokenResponseDto>.Fail("User not found.");

            // 4) Revoke old token
            rt.RevokedUtc = now;

            // 5) Issue new tokens
            return await IssueTokensAsync(user, userAgent, ipAddress, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Refresh token rotation failed.");
            return Result<TokenResponseDto>.Fail("Unable to rotate refresh token.");
        }
    }

    // ------------------------------------------------------------------
    // REVOKE ALL TOKENS FOR A USER
    // ------------------------------------------------------------------
    public async Task<Result> RevokeAllTokensAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var tokens = await _refreshRepo.FindAsync(
                r => r.UserId == userId && r.RevokedUtc == null && r.ExpiresUtc > now,
                ct);

            foreach (var t in tokens)
                t.RevokedUtc = now;

            await _uow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to revoke tokens for {UserId}", userId);
            return Result.Fail("Failed to revoke tokens.");
        }
    }

    // ------------------------------------------------------------------
    // CONFIG PARSING
    // ------------------------------------------------------------------
    private DateTime? GetAccessTokenExpiryUtc(DateTime now, out string? error)
    {
        error = null;

        var minutesStr = _config["Auth:Jwt:AccessTokenMinutes"];
        if (!int.TryParse(minutesStr, out var minutes))
        {
            error = "Jwt:AccessTokenMinutes is not a valid integer.";
            return null;
        }

        return now.AddMinutes(minutes);
    }
}
