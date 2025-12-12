using Clbio.API.Extensions;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Application.Interfaces;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Infrastructure.Data;
using Clbio.Shared.Results;
using Clbio.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace Clbio.Tests.IntegrationTests.Auth
{
    public class GoogleAuthIntegrationTests : IClassFixture<TestWebAppFactory>
    {
        private readonly TestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _testOutputHelper;

        public GoogleAuthIntegrationTests(TestWebAppFactory factory, ITestOutputHelper testOutputHelper)
        {
            _factory = factory;
            _testOutputHelper = testOutputHelper;

            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        s => s.ServiceType == typeof(IGoogleAuthService));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    var mockGoogle = new Mock<IGoogleAuthService>();

                    mockGoogle.Setup(g =>
                        g.ValidateIdTokenAsync("valid_token", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(
                        new ExternalUserInfoDto
                        {
                            Provider = "Google",
                            ProviderUserId = "google123",
                            Email = "integration@test.com",
                            EmailVerified = true,
                            Name = "Integration Tester",
                            PictureUrl = "http://avatar"
                        }
                    ));

                    services.AddSingleton(mockGoogle.Object);
                });
            }).CreateClient();
        }

        [Fact]
        public async Task GoogleLogin_Creates_And_Logs_In_User()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/google", new
            {
                idToken = "valid_token"
            });

            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"Google login failed: {body}");

            var dto = await response.Content.ReadFromJsonAsync<ApiResponse<TokenResponseDto>>();
            Assert.NotNull(dto?.Data?.AccessToken);
            Assert.NotNull(dto?.Data?.RefreshToken);

            // Verify DB state
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = db.Users.FirstOrDefault(u => u.Email == "integration@test.com");
            Assert.NotNull(user);
            Assert.Equal(AuthProvider.Google, user.AuthProvider);
            Assert.Equal("google123", user.ExternalId);
            Assert.True(user.EmailVerified);
        }

        [Fact]
        public async Task GoogleLogin_Links_Existing_Local_Account()
        {
            // create mock
            var mockGoogle = new Mock<IGoogleAuthService>();
            mockGoogle.Setup(g =>
                g.ValidateIdTokenAsync("valid_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(
                new ExternalUserInfoDto
                {
                    Provider = "Google",
                    ProviderUserId = "google999",
                    Email = "local@test.com",
                    EmailVerified = true
                }
            ));

            // override factory
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IGoogleAuthService>();
                    services.AddSingleton<IGoogleAuthService>(mockGoogle.Object);

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                });
            });

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Users.Add(new User
                {
                    Email = "local@test.com",
                    DisplayName = "Local User",
                    PasswordHash = "hashed_pass",
                    AuthProvider = AuthProvider.Local,
                    EmailVerified = true
                });

                db.SaveChanges();
            }

            var client = factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/google", new
            {
                IdToken = "valid_token"
            });
            _testOutputHelper.WriteLine($"Response to POST /api/auth/google: {await response.Content.ReadAsStringAsync()}");
            response.EnsureSuccessStatusCode();

            // Verify DB result
            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db2.Users.First(u => u.Email == "local@test.com");
            _testOutputHelper.WriteLine(
                "DB RESULT (user): " + JsonSerializer.Serialize(user, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                })
            );

            Assert.Equal(AuthProvider.Google, user.AuthProvider);
            Assert.Equal("google999", user.ExternalId);
            Assert.True(user.EmailVerified);
        }

        [Fact]
        public async Task GoogleLogin_Fails_On_Email_Mismatch()
        {
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var mockGoogle = new Mock<IGoogleAuthService>();
                    mockGoogle.Setup(g =>
                        g.ValidateIdTokenAsync("bad", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<ExternalUserInfoDto>.Fail("Invalid Google token"));

                    services.AddSingleton(mockGoogle.Object);
                });
            }).CreateClient();

            var res = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "bad" });
            Assert.False(res.IsSuccessStatusCode);
        }

        [Fact]
        public async Task GoogleLogin_Fails_For_Inconsistent_User()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Users.Add(new User
                {
                    Email = "broken@test.com",
                    ExternalId = null,
                    DisplayName = "Broken",
                    AuthProvider = AuthProvider.Local // inconsistent
                });

                db.SaveChanges();
            }

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var mockGoogle = new Mock<IGoogleAuthService>();
                    mockGoogle.Setup(x => x.ValidateIdTokenAsync("token", It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<ExternalUserInfoDto>.Ok(
                            new ExternalUserInfoDto
                            {
                                Email = "broken@test.com",
                                EmailVerified = true,
                                ProviderUserId = "pid"
                            }));

                    services.AddSingleton(mockGoogle.Object);
                });
            }).CreateClient();

            var res = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "token" });

            Assert.False(res.IsSuccessStatusCode);

            var body = await res.Content.ReadAsStringAsync();
            Assert.Contains("configuration", body);
        }
    }

}
