using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.Services.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Shared.Results;
using Clbio.Tests.Configs;
using Clbio.Tests.Helpers;
using Moq;
using Shouldly;

namespace Clbio.Tests.UnitTests.Auth;

public class PasswordResetServiceTests
{
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IAuthThrottlingService> _throttling = new();
    private readonly Mock<IRepository<User>> _userRepo = new();
    private readonly Mock<IRepository<PasswordResetToken>> _resetRepo = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly FakeEmailSender _emailSender = new();

    private readonly PasswordResetService _service;

    public PasswordResetServiceTests()
    {
        _service = new PasswordResetService(
            _tokenService.Object,
            _throttling.Object,
            _userRepo.Object,
            _resetRepo.Object,
            _refreshRepo.Object,
            _uow.Object,
            _emailSender,
            AuthTestConfig.Build(),
            null
        );
    }

    [Fact]
    public async Task ResetPassword_Fails_WhenTokenInvalid()
    {
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>()))
            .Returns(Result<string>.Fail("bad token"));

        var result = await _service.ResetPasswordAsync(
            new ResetPasswordRequestDto { Token = "bad", NewPassword = "abc123" },
            "127.0.0.1"
        );

        result.Success.ShouldBeFalse();
    }
}
