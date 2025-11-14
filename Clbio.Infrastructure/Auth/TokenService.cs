using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Clbio.Application.Services.Auth
{
    public sealed class TokenService(IConfiguration config) : ITokenService
    {
        public Result<string> CreateAccessToken(User user)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
                    new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                if (user is { GlobalRole: GlobalRole.Admin })
                    claims.Add(new(ClaimTypes.Role, nameof(GlobalRole.Admin)));

                var now = DateTime.UtcNow;
                var minutesParsed = int.TryParse(config["Jwt:AccessTokenMinutes"], out int minutes);

                if (!minutesParsed)
                    return Result<string>.Fail("Failed to parse access token minutes count to integer.");

                var expires = now.AddMinutes(minutes);

                var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                    issuer: config["Jwt:Issuer"],
                    audience: config["Jwt:Audience"],
                    claims: claims,
                    notBefore: now,
                    expires: expires,
                    signingCredentials: creds);

                return Result<string>.Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Create access token failed. Reason: {ex.Message}");
            }
        }

        public Result<(string token, DateTime expiresUtc, string tokenHash)> CreateRefreshToken()
        {
            try
            {
                // 64 random bytes → Base64Url
                var bytes = RandomNumberGenerator.GetBytes(64);
                var token = WebEncoders.Base64UrlEncode(bytes);
                var expires = DateTime.UtcNow.AddDays(int.Parse(config["Jwt:RefreshTokenDays"]!));

                var hashed = HashRefreshToken(token)
                    .Map(hash => (token, expires, hash));

                if (!hashed.Success)
                    return Result<(string, DateTime, string)>
                        .Fail(hashed.Error ?? "Can't get refresh token");

                return hashed;
            }
            catch (Exception ex)
            {
                return Result<(string token, DateTime expiresUtc, string tokenHash)>.Fail($"Failed to create refresh token. Reason: {ex.Message}");
            }
        }

        public Result<string> HashRefreshToken(string token)
        {
            try
            {
                // Store only a hash in DB, never the raw refresh token
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
                return Result<string>.Ok(WebEncoders.Base64UrlEncode(hash));
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Failed to hash refresh token. Reason: {ex.Message}");
            }

        }
    }
}
