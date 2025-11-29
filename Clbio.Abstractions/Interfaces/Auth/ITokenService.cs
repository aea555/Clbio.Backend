using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;

namespace Clbio.Abstractions.Interfaces.Auth
{
    public interface ITokenService
    {
        Result<string> CreateAccessToken(User user);
        Result<(string token, DateTime expiresUtc, string tokenHash)> CreateRefreshToken();
        Result<string> HashRefreshToken(string token);
    }
}
