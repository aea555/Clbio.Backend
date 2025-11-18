using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.Extensions;
using Clbio.Domain.Entities.V1;
using Clbio.Tests.IntegrationTests.Base;
using Clbio.Tests.Utils;
using Shouldly;

namespace Clbio.Tests.IntegrationTests.Auth;

public class RefreshTokenTests : IntegrationTestBase
{
    [Fact]
    public async Task RefreshToken_RotatesSuccessfully()
    {
        var auth = TestServiceFactory.CreateAuthService(Context);

        var user = new User
        {
            Email = "rotate@test.com",
            PasswordHash = PasswordManager.HashPassword("pass123").Value!,
            DisplayName = "Tester",
            EmailVerified = true
        };
        Context.Users.Add(user);
        Context.SaveChanges();

        var initial = await auth.LoginAsync(
            new LoginRequestDto { Email = user.Email, Password = "pass123" },
            "agent",
            "127.0.0.1"
        );

        initial.Success.ShouldBeTrue(initial.Error);
        var refreshToken = initial.Value.RefreshToken;

        var rotated = await auth.RefreshAsync(refreshToken, "agent2", "127.0.0.1");

        rotated.Success.ShouldBeTrue();
        rotated.Value.RefreshToken.ShouldNotBe(refreshToken);
    }
}
