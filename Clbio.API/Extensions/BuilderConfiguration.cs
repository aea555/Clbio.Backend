using Clbio.API.DependencyInjection;
using Clbio.Application.Settings;

namespace Clbio.API.Extensions
{
    public static class BuilderConfiguration
    {
        public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder)
        {
            // Max payload size
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
            });

            // Json logging (temporary)
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.FormatterName = "json";
            });

            // Env variable loading
            if (!builder.Environment.IsEnvironment("Testing"))
            {
                builder.Configuration
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
               .AddEnvironmentVariables();
            }
            return builder;
        }

        public static WebApplicationBuilder ConfigureBuilderServices(this WebApplicationBuilder builder)
        {
            builder.Services
                .AddControllers()
                .AddJsonOptions(opts =>
                {
                    // ensure safe encoding 
                    opts.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default;
                });

            // validate connection string
            builder.Services.AddOptions<DatabaseSettings>()
                    .Bind(builder.Configuration.GetSection("ConnectionStrings"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            // google client config
            builder.Services.Configure<GoogleAuthSettings>(
            builder.Configuration.GetSection("Auth:Google:ClientId"));

            // add services
            builder.Services
                .AddOpenApi()
                .AddClbio(builder.Configuration)
                .AddCorsPolicy()
                .AddGlobalRateLimiter()
                .AddControllers();

            return builder;
        }
    }
}
