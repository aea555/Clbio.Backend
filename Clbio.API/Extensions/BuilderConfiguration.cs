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

            if (builder.Environment.IsEnvironment("Testing"))
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Jwt:Key"] = "1234567890123456789012345678901234567890",
                    ["Auth:Jwt:Issuer"] = "http://test.local",
                    ["Auth:Jwt:Audience"] = "http://test.local",
                    ["Auth:Jwt:AccessTokenMinutes"] = "15",
                    ["Auth:Jwt:RefreshTokenDays"] = "14",

                    ["Auth:Login:MaxFailedAttempts"] = "5",
                    ["Auth:Login:WindowMinutes"] = "15",

                    ["Auth:PasswordReset:TokenMinutes"] = "30",
                    ["Auth:PasswordReset:WindowMinutes"] = "15",
                    ["Auth:PasswordReset:MaxAttempts"] = "5",
                    ["Auth:PasswordReset:MaxIpAttempts"] = "10",

                    ["Auth:EmailVerification:TokenMinutes"] = "120",

                    ["App:BaseUrl"] = "http://test.local",

                    ["Email:FromEmail"] = "test@clbio.org",
                    ["Email:FromName"] = "Test Sender",
                });
            }
            else
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
                .AddJwt(builder.Configuration)
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
