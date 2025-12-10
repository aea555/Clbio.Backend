using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Interfaces;
using Clbio.Infrastructure.Data;
using Clbio.Tests.Helpers;
using Clbio.Tests.Utils.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Clbio.Tests.Utils
{
    public class TestWebAppFactory : WebApplicationFactory<Program>
    {
        public TestWebAppFactory()
        {
            this.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.Sources.Clear();
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Auth:Jwt:Key"] = "1234567890123456789012345678901234567890",
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
                        ["Email:FromName"] = "Test Sender",
                    });
                });
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.RemoveAll<IGoogleAuthService>();
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                services.RemoveAll<IConnectionMultiplexer>();
                services.RemoveAll<IDistributedCache>();

                services.AddDistributedMemoryCache();

                services.RemoveAll<IHostedService>();
                services.AddSingleton<IHostedService, FakeHostedService>();

                services.AddSingleton<IEmailSender, FakeEmailSender>();

                services.AddDbContext<AppDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase("ApiTestDb");
                    options.UseApplicationServiceProvider(sp);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // fake IP middleware
                services.AddSingleton<IStartupFilter, FakeIpStartupFilter>();
            });
        }
    }
}
