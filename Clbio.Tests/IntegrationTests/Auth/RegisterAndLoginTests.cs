using Clbio.Application.DTOs.V1.Auth;
using Clbio.Tests.IntegrationTests.Base;
using Clbio.Tests.Utils;
using Shouldly;

namespace Clbio.Tests.IntegrationTests.Auth;

public class RegisterAndLoginTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_Then_Login_Works()
    {
        var auth = TestServiceFactory.CreateAuthService(Context);

        // 1) Register
        var register = await auth.RegisterAsync(
            new RegisterRequestDto
            {
                Email = "test@example.com",
                Password = "mypassword123",
                DisplayName = "Tester"
            },
            userAgent: "test-agent",
            ipAddress: "127.0.0.1"
        );

        register.Success.ShouldBeTrue(register.Error);
        register.Value.AccessToken.ShouldNotBeNull();
        register.Value.RefreshToken.ShouldNotBeNull();

        // Mark user as verified. email verification is tested separately
        var user = Context.Users.First();
        user.EmailVerified = true;
        Context.SaveChanges();

        // 2) Login
        var login = await auth.LoginAsync(
            new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "mypassword123"
            },
            "test-agent",
            "127.0.0.1"
        );

        login.Success.ShouldBeTrue(login.Error);
        login.Value.AccessToken.ShouldNotBeNull();
    }
}
