using Clbio.Abstractions.Interfaces.Services;
using Clbio.Infrastructure.Data;
using Clbio.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Clbio.Tests.Utils
{
    public class TestWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
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
                });
            });

            builder.ConfigureServices(services =>
            {
                var emailSenderDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailSenderDescriptor != null)
                    services.Remove(emailSenderDescriptor);

                services.AddSingleton<IEmailSender, FakeEmailSender>();

                services.Remove(
                    services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
                );

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("ApiTestDb");
                });
                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }
}
