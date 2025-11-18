using Clbio.Tests.Utils;
using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace Clbio.Tests.IntegrationTests.Security
{
    public class ErrorHandlingTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task UnhandledException_ShouldReturn_JsonError()
        {
            var response = await _client.GetAsync("/dev/errortest");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(body);

            json.TryGetProperty("message", out _).Should().BeTrue();
            json.TryGetProperty("traceId", out _).Should().BeTrue();
        }
    }
}
