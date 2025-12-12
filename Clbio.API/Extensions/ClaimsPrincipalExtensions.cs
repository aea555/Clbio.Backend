using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Clbio.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var idStr =
                user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                user.FindFirst("userId")?.Value;

            if (idStr is null || !Guid.TryParse(idStr, out var id))
                throw new InvalidOperationException("User ID claim is missing or invalid.");

            return id;
        }
    }
}
