using Amazon.Runtime;
using Amazon.S3;
using Clbio.Abstractions.Interfaces.Infrastructure;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.API.DependencyInjection;
using Clbio.API.Services;
using Clbio.Application.Settings;
using Clbio.Infrastructure.Options;
using Clbio.Infrastructure.Services;
using StackExchange.Redis;

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
            builder.Configuration.GetSection("Auth:Google"));

            // aws
            builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AWS"));

            var awsSettings = new AwsSettings();
            builder.Configuration.GetSection("AWS").Bind(awsSettings);

            builder.Services.AddSingleton<IAmazonS3>(sp =>
            {
                var credentials = new BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey);
                var config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsSettings.Region)
                };

                // MinIO check
                if (!string.IsNullOrEmpty(awsSettings.ServiceUrl))
                {
                    config.ServiceURL = awsSettings.ServiceUrl;
                    config.ForcePathStyle = true; 
                }

                return new AmazonS3Client(credentials, config);
            });

            builder.Services.AddSingleton<IFileStorageService, S3FileStorageService>();

            // add services
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen()
                .AddClbio(builder.Configuration)
                .AddCorsPolicy(builder.Configuration)
                .AddGlobalRateLimiter()
                .AddControllers();

            // SignalR

            // conn string
            var redisConn = builder.Configuration.GetConnectionString("RedisConnection")
                    ?? "localhost:6379";

            // Add SignalR and Redis backplane
            builder.Services.AddSignalR()
                .AddStackExchangeRedis(redisConn, options =>
                {
                    // Prefix to not mix keys
                    options.Configuration.ChannelPrefix = RedisChannel.Literal("ClbioSocket");
                });

            // add it
            builder.Services.AddScoped<ISocketService, SignalRSocketService>();

            return builder;
        }
    }
}
