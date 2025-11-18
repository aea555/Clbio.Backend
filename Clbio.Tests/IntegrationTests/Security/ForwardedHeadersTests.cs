using Clbio.Tests.Utils;
using FluentAssertions;
using System.Net;

namespace Clbio.Tests.IntegrationTests.Security
{
    public class ForwardedHeadersTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task ShouldAcceptXForwardedFor_WhenConfiguredProxy()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/dev/health");
            request.Headers.Add("X-Forwarded-For", "203.0.113.5");

            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
