using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.Services.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Shared.Results;
using Clbio.Tests.Configs;
using Moq;
using Shouldly;

namespace Clbio.Tests.UnitTests.Auth;

public class TokenFactoryServiceTests
{
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshRepo = new();
    private readonly Mock<IRepository<User>> _userRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private readonly TokenFactoryService _factory;

    public TokenFactoryServiceTests()
    {
        _factory = new TokenFactoryService(
            _tokenService.Object,
            _refreshRepo.Object,
            _userRepo.Object,
            _uow.Object,
            AuthTestConfig.Build(),
            null
        );
    }

    [Fact]
    public async Task IssueTokensAsync_ReturnsFailure_WhenAccessTokenFails()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };

        _tokenService.Setup(t => t.CreateAccessToken(user))
            .Returns(Result<string>.Fail("fail"));

        var result = await _factory.IssueTokensAsync(user, "agent", "127.0.0.1");

        result.Success.ShouldBeFalse();
    }
}
