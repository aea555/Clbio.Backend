using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Application.Interfaces;
using Clbio.Application.Settings;
using Clbio.Shared.Results;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Clbio.Application.Services.Auth.External
{
    public sealed class GoogleAuthService(IOptions<GoogleAuthSettings> options) : IGoogleAuthService
    {
        private readonly GoogleAuthSettings _settings = options.Value;

        public async Task<Result<ExternalUserInfoDto>> ValidateIdTokenAsync(
            string idToken,
            CancellationToken ct = default)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = [_settings.ClientId]
                    });

                if (payload == null)
                    return Result<ExternalUserInfoDto>.Fail("Invalid Google token.");

                if (string.IsNullOrWhiteSpace(payload.Email))
                    return Result<ExternalUserInfoDto>.Fail("Google account has no email.");

                if (!payload.EmailVerified)
                    return Result<ExternalUserInfoDto>.Fail("Google account is not verified");

                var info = new ExternalUserInfoDto
                {
                    Provider = "Google",
                    ProviderUserId = payload.Subject,
                    Email = payload.Email,
                    EmailVerified = payload.EmailVerified,
                    Name = payload.Name,
                    PictureUrl = payload.Picture
                };

                return Result<ExternalUserInfoDto>.Ok(info);
            }
            catch (InvalidJwtException)
            {
                return Result<ExternalUserInfoDto>.Fail("Invalid Google token.");
            }
            catch (Exception)
            {
                return Result<ExternalUserInfoDto>.Fail("Failed to validate Google token.");
            }
        }
    }
}
