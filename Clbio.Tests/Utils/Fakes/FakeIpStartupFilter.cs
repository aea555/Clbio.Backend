using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Clbio.Tests.Utils.Fakes
{
    public class FakeIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // ip address set to 127.0.0.1 on every req
                app.Use(async (context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
                    await nextMiddleware();
                });

                next(app);
            };
        }
    }
}
