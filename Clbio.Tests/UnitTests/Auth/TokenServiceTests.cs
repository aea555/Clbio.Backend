using Clbio.Domain.Entities.V1;
using Clbio.Infrastructure.Auth;
using Clbio.Tests.Configs;
using Shouldly;

namespace Clbio.Tests.UnitTests.Auth;

public class TokenServiceTests
{
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        var config = AuthTestConfig.Build();
        _service = new TokenService(config);
    }

    [Fact]
    public void CreateAccessToken_Returns_ValidJwt()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var result = _service.CreateAccessToken(user);

        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNullOrWhiteSpace();
        result.Value.ShouldContain(".");
    }

    [Fact]
    public void CreateRefreshToken_Returns_Token_And_Hash()
    {
        var result = _service.CreateRefreshToken();

        result.Success.ShouldBeTrue();
        result.Value.token.ShouldNotBeNull();
        result.Value.tokenHash.ShouldNotBeNull();
    }

    [Fact]
    public void HashRefreshToken_Returns_ConsistentHash()
    {
        var hash1 = _service.HashRefreshToken("ABC123");
        var hash2 = _service.HashRefreshToken("ABC123");

        hash1.Success.ShouldBeTrue();
        hash2.Success.ShouldBeTrue();
        hash1.Value.ShouldBe(hash2.Value);
    }
}
