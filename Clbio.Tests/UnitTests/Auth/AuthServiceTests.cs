using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Application.Interfaces;
using Clbio.Application.Services.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Clbio.Tests.Utils;
using Moq;
using Shouldly;
using System.Linq.Expressions;

namespace Clbio.Tests.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRepository<User>> _userRepo = new();
    private readonly Mock<IRepository<LoginAttempt>> _loginAttempts = new();
    private readonly Mock<IAuthThrottlingService> _throttling = new();
    private readonly Mock<IEmailVerificationService> _emailVerification = new();
    private readonly Mock<IPasswordResetService> _passwordReset = new();
    private readonly Mock<ITokenFactoryService> _tokenFactory = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IGoogleAuthService> _googleAuth = new();

    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _uow.Setup(u => u.Repository<User>()).Returns(_userRepo.Object);
        _uow.Setup(u => u.Repository<LoginAttempt>()).Returns(_loginAttempts.Object);

        _service = new AuthService(
            _uow.Object,
            TestMapperFactory.Create(),
            _throttling.Object,
            _emailVerification.Object,
            _passwordReset.Object,
            _tokenFactory.Object,
            _tokenService.Object,
            _googleAuth.Object,
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

    [Fact]
    public async Task GoogleLogin_Fails_WhenTokenInvalid()
    {
        // Arrange
        _googleAuth
            .Setup(g => g.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Fail("Invalid token"));

        // Act
        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "badtoken" },
            null, null
        );

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("Invalid token");
    }

    [Fact]
    public async Task GoogleLogin_Creates_New_User_IfNotExists()
    {
        // Arrange: Google says this is a verified user
        var ext = new ExternalUserInfoDto
        {
            Provider = "Google",
            ProviderUserId = "123",
            Email = "google@test.com",
            EmailVerified = true,
            Name = "Tester",
            PictureUrl = "http://avatar"
        };

        _googleAuth
            .Setup(g => g.ValidateIdTokenAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        // No user currently exists
        _userRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<User>());


        // Capture created user
        User? createdUser = null;

        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        _tokenFactory
            .Setup(t => t.IssueTokensAsync(It.IsAny<User>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Ok(new TokenResponseDto()));

        // Act
        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null, null
        );

        // Assert
        result.Success.ShouldBeTrue();
        createdUser.ShouldNotBeNull();
        createdUser.Email.ShouldBe("google@test.com");
        createdUser.AuthProvider.ShouldBe(AuthProvider.Google);
        createdUser.ExternalId.ShouldBe("123");
        createdUser.EmailVerified.ShouldBe(true);
    }

    [Fact]
    public async Task GoogleLogin_Links_Local_User()
    {
        // Arrange
        var ext = new ExternalUserInfoDto
        {
            ProviderUserId = "g123",
            Email = "local@test.com",
            EmailVerified = true
        };

        _googleAuth
            .Setup(g => g.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        var existing = new User
        {
            Email = "local@test.com",
            PasswordHash = "hash",
            AuthProvider = AuthProvider.Local
        };

        _userRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        _tokenFactory
            .Setup(t => t.IssueTokensAsync(It.IsAny<User>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Ok(new TokenResponseDto()));

        // Act
        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null, null
        );

        // Assert
        result.Success.ShouldBeTrue();
        existing.AuthProvider.ShouldBe(AuthProvider.Google);
        existing.ExternalId.ShouldBe("g123");
    }

    [Fact]
    public async Task PasswordLogin_Fails_For_GoogleOnly_Account()
    {
        // Arrange
        var user = new User
        {
            Email = "googleonly@test.com",
            AuthProvider = AuthProvider.Google,
        };

        _userRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([user]);

        // Act
        var result = await _service.LoginAsync(
            new LoginRequestDto { Email = user.Email, Password = "whatever" },
            null, null
        );

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("Google");
    }

    [Fact]
    public async Task GoogleLogin_Fails_WhenEmailMissing()
    {
        // Arrange: Google token returns payload without email
        var ext = new ExternalUserInfoDto
        {
            ProviderUserId = "123",
            Email = "",
            EmailVerified = true,
        };

        _googleAuth
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        // Act
        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null,
            null
        );

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("email");
    }

    [Fact]
    public async Task GoogleLogin_Fails_When_Google_Email_Not_Verified()
    {
        var ext = new ExternalUserInfoDto
        {
            Email = "x@test.com",
            EmailVerified = false,
            ProviderUserId = "123"
        };

        _googleAuth
            .Setup(x => x.ValidateIdTokenAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null, null
        );

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("Google email is not verified.");
    }

    [Fact]
    public async Task GoogleLogin_Fails_When_Throttled()
    {
        _userRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), false, It.IsAny<CancellationToken>()))
         .ReturnsAsync(new List<User>());

        var ext = new ExternalUserInfoDto
        {
            Email = "x@test.com",
            EmailVerified = true,
            ProviderUserId = "g123"
        };

        _googleAuth.Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        // Throttled
        _throttling.Setup(x => x.IsLoginThrottled(ext.Email, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null, null
        );

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("Too many");
    }

    [Fact]
    public async Task GoogleLogin_Fails_When_ExternalId_Does_Not_Match()
    {
        var ext = new ExternalUserInfoDto
        {
            Email = "test@test.com",
            EmailVerified = true,
            ProviderUserId = "new_google_id"
        };

        _googleAuth
            .Setup(x => x.ValidateIdTokenAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        var existing = new User
        {
            Email = "test@test.com",
            AuthProvider = AuthProvider.Google,
            ExternalId = "different_google_id"
        };

        _userRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null, null
        );

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GoogleLogin_Fails_When_Account_Inconsistent()
    {
        var ext = new ExternalUserInfoDto
        {
            Email = "x@test.com",
            EmailVerified = true,
            ProviderUserId = "puid"
        };

        _googleAuth
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        // User with inconsistent state
        var inconsistent = new User
        {
            Email = "x@test.com",
            PasswordHash = null,
            ExternalId = null,
            AuthProvider = AuthProvider.Local
        };

        _userRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([inconsistent]);

        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null, null
        );

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("configuration");
    }

    [Fact]
    public async Task GoogleLogin_Succeeds_For_Hybrid_Account()
    {
        var ext = new ExternalUserInfoDto
        {
            Email = "hybrid@test.com",
            EmailVerified = true,
            ProviderUserId = "google123"
        };

        _googleAuth
            .Setup(x => x.ValidateIdTokenAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(ext));

        var existing = new User
        {
            Email = "hybrid@test.com",
            PasswordHash = "hash",            // local login possible
            AuthProvider = AuthProvider.Google,
            ExternalId = "google123"          // matches provider ID
        };

        _userRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        _tokenFactory
            .Setup(f => f.IssueTokensAsync(existing, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenResponseDto>.Ok(new TokenResponseDto()));

        var result = await _service.LoginWithGoogleAsync(
            new GoogleLoginRequestDto { IdToken = "token" },
            null, null
        );

        result.Success.ShouldBeTrue();
    }

}
