using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.Interfaces;
using Clbio.Application.Services.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Moq;
using Shouldly;

namespace Clbio.Tests.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<LoginAttempt>> _loginAttempts = new();
    private readonly Mock<IAuthThrottlingService> _throttling = new();
    private readonly Mock<IEmailVerificationService> _emailVerification = new();
    private readonly Mock<IPasswordResetService> _passwordReset = new();
    private readonly Mock<ITokenFactoryService> _tokenFactory = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var userRepo = new Mock<IRepository<User>>();
        _uow.Setup(u => u.Repository<User>()).Returns(userRepo.Object);
        _uow.Setup(u => u.Repository<LoginAttempt>()).Returns(_loginAttempts.Object);

        _service = new AuthService(
            _uow.Object,
            _throttling.Object,
            _emailVerification.Object,
            _passwordReset.Object,
            _tokenFactory.Object,
            _tokenService.Object,
            null
        );
    }

    [Fact]
    public async Task Login_Fails_WhenUserNotFound()
    {
        var result = await _service.LoginAsync(
            new LoginRequestDto { Email = "a", Password = "b" },
            null,
            null
        );

        result.Success.ShouldBeFalse();
    }
}
