using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Reflection;

namespace Clbio.Tests
{
    public class TestWebAppFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // dev environment for testing
            builder.UseEnvironment("Development");
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.ApplicationKey,
                typeof(Program).Assembly.FullName ?? Assembly.GetExecutingAssembly().FullName!);
        }
    }
}
