using Clbio.Application.DTOs.V1.Auth;
using Clbio.Domain.Entities.V1;
using Clbio.Tests.IntegrationTests.Base;
using Clbio.Tests.Utils;
using Shouldly;

namespace Clbio.Tests.IntegrationTests.Auth;

public class PasswordResetTests : IntegrationTestBase
{
    [Fact]
    public async Task ForgotPassword_CreatesToken()
    {
        var auth = TestServiceFactory.CreateAuthService(Context);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed",
            DisplayName = "Tester",
            EmailVerified = true
        };

        Context.Users.Add(user);
        Context.SaveChanges();

        var result = await auth.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = user.Email },
            "127.0.0.1"
        );

        result.Success.ShouldBeTrue();

        Context.PasswordResetTokens.Count().ShouldBe(1);
    }
}
