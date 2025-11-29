using Clbio.Tests.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Clbio.Tests.IntegrationTests.Security
{
    public class HttpsRedirectTests : IClassFixture<TestWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly TestWebAppFactory _factory;

        public HttpsRedirectTests(TestWebAppFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            _factory = factory;
        }

        [Fact]
        public async Task ShouldRedirectHttpToHttps()
        {
            var response = await _client.GetAsync("http://localhost/dev/health");

            var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
            {
                response.StatusCode.Should().Be(HttpStatusCode.PermanentRedirect);
                response.Headers.Location.Should().NotBeNull();
                response.Headers.Location!.Scheme.Should().Be("https");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }
}
