using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Clbio.Tests.IntegrationTests
{
    public class RequestLimitTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TestWebAppFactory _factory = factory;

        [Fact]
        public async Task LargePayload_ShouldReturn_413()
        {
            /*
            Note: because in-memory testing doesn't use kestrel, a manual payload size testing middleware like below can be used instead in program.cs
            to pass this test without environment constraints:

            app.Use(async (context, next) =>
            {
                context.Request.Headers.TryGetValue("Content-Length", out var lenStr);
                if (long.TryParse(lenStr, out var len) && len > 10 * 1024 * 1024)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.CompleteAsync();
                    return;
                }

                await next();
            });

            */

            var bigPayload = new string('x', 11 * 1024 * 1024);
            using var content = new StringContent(bigPayload, System.Text.Encoding.UTF8, "text/plain");
            var response = await _client.PostAsync("/dev/payloadtest", content);

            if (!_factory.Services.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
            }
            else
                response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
