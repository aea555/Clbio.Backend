using Clbio.Tests.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Clbio.Tests.IntegrationTests.Security
{
    public class SecurityHeadersTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TestWebAppFactory _factory = factory;

        [Fact]
        public async Task Responses_ShouldContain_AllSecurityHeaders()
        {
            var response = await _client.GetAsync("/dev/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var headers = response.Headers;

            headers.Contains("X-Frame-Options").Should().BeTrue();
            headers.Contains("X-Content-Type-Options").Should().BeTrue();
            headers.Contains("Referrer-Policy").Should().BeTrue();

            var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();

            if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
            {
                headers.Contains("Strict-Transport-Security").Should().BeTrue();
            }
        }
    }
}
