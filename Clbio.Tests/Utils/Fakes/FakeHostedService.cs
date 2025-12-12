using Microsoft.Extensions.Hosting;

namespace Clbio.Tests.Utils.Fakes
{
    public class FakeHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
