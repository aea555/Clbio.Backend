using Clbio.Abstractions.Interfaces.Services;
using Clbio.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Clbio.Infrastructure.DependencyInjection
{
    public static class RedisDependencyInjection
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDistributedMemoryCache();
                return services;
            }

            var redisConn = configuration.GetConnectionString("RedisConnection");

            if (string.IsNullOrEmpty(redisConn))
            {
                throw new InvalidOperationException("Redis ConnectionString 'RedisConnection' is missing in environment variables!");
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConn));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
            });

            services.AddScoped<IPresenceService, RedisPresenceService>();

            return services;
        }
    }
}
