using Clbio.API.Extensions;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Infrastructure.Data;
using Clbio.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Clbio.Tests.IntegrationTests.Auth
{
    public class AuthApiTests(TestWebAppFactory factory, ITestOutputHelper testOutputHelper) : IClassFixture<TestWebAppFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public async Task Register_Login_Refresh_Flow_Works()
        {
            var envResponse = await _client.GetStringAsync("/dev/env");
            _testOutputHelper.WriteLine("ENV FROM API: " + envResponse);

            // 1) Register
            var regPayload = new
            {
                Email = "api@test.com",
                Password = "abcdef",
                DisplayName = "Tester"
            };

            var regResponse = await _client.PostAsJsonAsync("/api/auth/register", regPayload);
            var responseBody = await regResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine("Response to POST /api/auth/register: " + responseBody);
            Assert.True(regResponse.IsSuccessStatusCode,
            $"Register failed with {regResponse.StatusCode}: {responseBody}");

            regResponse.EnsureSuccessStatusCode();

            var regDto = await regResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponseDto>>();
            Assert.NotNull(regDto?.Data?.AccessToken);
            Assert.NotNull(regDto?.Data?.RefreshToken);

            // verify user manually
            var db = factory.Services.GetRequiredService<AppDbContext>();
            var user = db.Users.First(u => u.Email == "api@test.com");
            user.EmailVerified = true;
            db.SaveChanges();

            // 2) Login
            var loginPayload = new
            {
                Email = "api@test.com",
                Password = "abcdef"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);
            var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine("Response to POST /api/auth/login: " + loginResponseBody);
            loginResponse.EnsureSuccessStatusCode();

            // 3) Refresh
            var refreshPayload = new
            {
                refreshToken = regDto.Data.RefreshToken
            };

            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshPayload);
            refreshResponse.EnsureSuccessStatusCode();
        }
    }

}
