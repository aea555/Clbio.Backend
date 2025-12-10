using Microsoft.AspNetCore.HttpOverrides;

namespace Clbio.API.Extensions
{
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiSecurity(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            var forwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            };

            forwardedHeaderOptions.KnownNetworks.Clear();
            forwardedHeaderOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardedHeaderOptions);

            // --- HTTPS & HSTS ---
            if (!env.IsEnvironment("Testing") && !env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            // --- Security Headers ---
            app.UseSecurityHeaders(policyCollection =>
            {
                policyCollection
                .AddFrameOptionsDeny()
                .AddXssProtectionBlock()
                .AddContentTypeOptionsNoSniff()
                .AddStrictTransportSecurityMaxAge(TimeSpan.FromDays(365).Days)
                .AddReferrerPolicyNoReferrerWhenDowngrade();

                policyCollection.AddContentSecurityPolicy(builder =>
                {
                    builder.AddDefaultSrc().None(); // trust nothing
                    builder.AddScriptSrc().Self();  // scripts only work from owned domain
                    builder.AddStyleSrc().Self().UnsafeInline(); // inline style for swagger
                    builder.AddImgSrc().Self().Data();
                    builder.AddConnectSrc().Self(); // for signalR and api calls. app can send ajax requests and connect socket to only its own backend 
                    builder.AddFrameAncestors().None(); // block iframe
                });

                policyCollection.RemoveServerHeader();
            });

            return app;
        }
    }
}
