using Clbio.Application.Services.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Infrastructure;
using Clbio.Infrastructure.Auth;
using Clbio.Infrastructure.Data;
using Clbio.Tests.Configs;
using Clbio.Tests.Helpers;

namespace Clbio.Tests.Utils;

public static class TestServiceFactory
{
    public static AuthService CreateAuthService(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var config = AuthTestConfig.Build();

        var emailSender = new FakeEmailSender();

        // Infrastructure repositories
        var userRepo = uow.Repository<User>();
        var loginAttemptRepo = uow.Repository<LoginAttempt>();
        var refreshRepo = uow.Repository<RefreshToken>();
        var passwordResetRepo = uow.Repository<PasswordResetToken>();
        var passwordResetAttemptRepo = uow.Repository<PasswordResetAttempt>();
        var emailVerificationRepo = uow.Repository<EmailVerificationToken>();

        // Core services
        var tokenService = new TokenService(config);

        var throttling = new AuthThrottlingService(
            loginAttemptRepo,
            passwordResetAttemptRepo,
            uow,
            config
        );

        var emailVerification = new EmailVerificationService(
            userRepo,
            emailVerificationRepo,
            tokenService,
            emailSender,
            uow,
            config,
            null
        );

        var tokenFactory = new TokenFactoryService(
            tokenService,
            refreshRepo,
            userRepo,
            uow,
            config,
            null
        );

        var passwordReset = new PasswordResetService(
            tokenService,
            throttling,
            userRepo,
            passwordResetRepo,
            refreshRepo,
            uow,
            emailSender,
            config,
            null
        );

        return new AuthService(
            uow,
            throttling,
            emailVerification,
            passwordReset,
            tokenFactory,
            tokenService,
            null
        );
    }
}
