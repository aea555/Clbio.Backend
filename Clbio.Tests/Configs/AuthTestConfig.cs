using Microsoft.Extensions.Configuration;

namespace Clbio.Tests.Configs
{
    public static class AuthTestConfig
    {
        public static IConfiguration Build()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Jwt:Key"] = "THIS_IS_A_DETERMINISTIC_TEST_KEY_12345678901234567890",
                    ["Auth:Jwt:Issuer"] = "http://test.local",
                    ["Auth:Jwt:Audience"] = "http://test.local",
                    ["Auth:Jwt:AccessTokenMinutes"] = "15",
                    ["Auth:Jwt:RefreshTokenDays"] = "14",

                    ["Auth:Login:MaxFailedAttempts"] = "5",
                    ["Auth:Login:WindowMinutes"] = "15",

                    ["Auth:PasswordReset:TokenMinutes"] = "30",
                    ["Auth:PasswordReset:WindowMinutes"] = "15",
                    ["Auth:PasswordReset:MaxAttempts"] = "5",
                    ["Auth:PasswordReset:MaxIpAttempts"] = "10",

                    ["Auth:EmailVerification:TokenMinutes"] = "120",

                    ["App:BaseUrl"] = "http://test.local",

                    ["Email:FromEmail"] = "test@clbio.org",
                    ["Email:FromName"] = "Test",
                })
            .Build();
        }

    }
}
