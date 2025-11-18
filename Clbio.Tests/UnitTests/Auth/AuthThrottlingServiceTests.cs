using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.Services.Auth;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Tests.Configs;
using Moq;
using Shouldly;
using System.Linq.Expressions;

namespace Clbio.Tests.UnitTests.Auth;

public class AuthThrottlingServiceTests
{
    private readonly AuthThrottlingService _service;
    private readonly Mock<IRepository<LoginAttempt>> _loginRepo = new();
    private readonly Mock<IRepository<PasswordResetAttempt>> _resetRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public AuthThrottlingServiceTests()
    {
        _service = new AuthThrottlingService(
            _loginRepo.Object,
            _resetRepo.Object,
            _uow.Object,
            AuthTestConfig.Build()
        );
    }

    [Fact]
    public async Task IsLoginThrottled_ReturnsTrue_WhenTooManyFailures()
    {
        _loginRepo
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<LoginAttempt, bool>>>(),
                It.IsAny<CancellationToken>()
            ))

            .ReturnsAsync(
            [
                new() { Succeeded = false },
                new() { Succeeded = false },
                new() { Succeeded = false },
                new() { Succeeded = false },
                new() { Succeeded = false }
            ]);

        var result = await _service.IsLoginThrottled("user@example.com");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLoginThrottled_ReturnsFalse_WhenUnderLimit()
    {
        _loginRepo
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<LoginAttempt, bool>>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(
            [
                new() { Succeeded = false }
            ]);

        var result = await _service.IsLoginThrottled("user@example.com");

        result.ShouldBeFalse();
    }
}
