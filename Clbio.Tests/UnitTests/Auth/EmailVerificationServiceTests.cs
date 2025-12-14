using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.Interfaces;
using Clbio.Application.Services.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Shared.Results;
using Clbio.Tests.Configs;
using Clbio.Tests.Helpers;
using Clbio.Tests.Utils.Fakes;
using Moq;
using Shouldly;

namespace Clbio.Tests.UnitTests.Auth;

public class EmailVerificationServiceTests
{
    private readonly Mock<IRepository<EmailVerificationToken>> _tokenRepo = new();
    private readonly Mock<IRepository<User>> _userRepo = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ITokenFactoryService> _tokenFactory = new();
    private readonly EmailVerificationService _service;

    public EmailVerificationServiceTests()
    {
        _service = new EmailVerificationService(
            _userRepo.Object,
            _tokenRepo.Object,
            new FakeCaching(),
            _tokenService.Object,
            _tokenFactory.Object,
            _emailSender,
            _uow.Object,
            AuthTestConfig.Build(),
            null
        );
    }
}
