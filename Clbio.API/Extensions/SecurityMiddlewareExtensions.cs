using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace Clbio.API.Extensions
{
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiSecurity(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // --- Reverse proxy hardening ---
            var forwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                KnownNetworks = { }, // clearing the defaults to avoid trusting all local networks
                KnownProxies = { IPAddress.Parse("172.17.0.1") } // Docker bridge ip for reverse proxy
            };

            app.UseForwardedHeaders(forwardedHeaderOptions);

            // --- HTTPS & HSTS ---
            if (!env.IsDevelopment())
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
                .AddReferrerPolicyNoReferrer();
            });

            return app;
        }
    }
}
