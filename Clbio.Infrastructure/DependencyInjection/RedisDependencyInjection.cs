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

            // In tests don't wire real Redis
            if (string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                // Use simple in-memory cache
                services.AddDistributedMemoryCache();
                return services;
            }

            var redisConn = configuration.GetConnectionString("RedisConnection")
                         ?? "localhost:6379";

            // In non-test envs, real Redis
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConn));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
            });

            return services;
        }
    }
}
